using BestHTTP.WebSocket;
using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEditor;
using UnityEngine;
using PokerError = Com.poker.Core.Error;

public sealed class GameServerClient : MonoBehaviour
{
    static GameServerClient _instance;
    public static GameServerClient Instance => _instance != null ? _instance : CreateSingleton();

    [Header("Server")]
    public string serverUrl = "ws://51.79.160.227:26001/ws";
    public uint protocolVersion = 1;
    public int maxPacketBytes = 32768;
    public float pingIntervalSec = 15f;

    [Header("Auth")]
    public string clientVersionOverride = "";
    public string deviceIdOverride = "";

    [Header("Reconnect")]
    public bool autoReconnect = true;
    public float reconnectInitialDelaySec = 1.0f;
    public float reconnectMaxDelaySec = 10.0f;
    public int reconnectMaxAttempts = 0; // 0 = infinite

    [Header("State (read-only)")]
    [SerializeField] string playerId;
    [SerializeField] long credits;
    [SerializeField] string sessionId;
    [SerializeField] string tableId;
    [SerializeField] bool isConnected;
    [SerializeField] ulong lastReceivedSeq;
    [SerializeField] uint resumeWindowSec;
    [SerializeField] long lastInboundUnixMs;
    [SerializeField] long lastResumeUnixMs;

    [Header("Main Thread Queue")]
    [SerializeField] int _maxMainThreadActionsPerFrame = 120;
    [SerializeField] int _catchUpQueueThreshold = 240;
    public bool IsCatchingUp { get; private set; }

    public string PlayerId => playerId;
    public string SessionId => sessionId;
    public string TableId => tableId;
    public bool IsConnected => isConnected;
    private int matchSizeId_;
    public int MatchSizeId
    {
        get { return matchSizeId_; }
        set
        {
            matchSizeId_ = value;
        }
    }

    WebSocket _socket;
    string _pendingLaunchToken;
    string _pendingOperatorPublicId;
    bool _isConnecting;
    ulong _seq;
    float _nextPingTime;
    float _nextReconnectTime;
    float _currentReconnectDelay;
    int _reconnectAttempts;
    bool _reconnectPending;
    bool _shouldSendResume;
    ulong _resumeSeq;
    bool _suppressNextReconnect;
    const int ResumeStallMs = 20000;
    const int ResumeCooldownMs = 3000;

    readonly ConcurrentQueue<byte[]> _recvQueue = new ConcurrentQueue<byte[]>();
    readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    AutoResetEvent _recvSignal;
    Thread _recvThread;
    volatile bool _running;
    long _lastRecvTicks;
    long _lastHandledTicks;

    public event Action<MsgType, IMessage> MessageReceived;
    public event Action<ConnectResponse> ConnectResponseReceived;
    public event Action<ResumeResponse> ResumeResponseReceived;
    public event Action<TableSnapshot> TableSnapshotReceived;
    public event Action<PokerTableList> TableListReceived;
    public event Action<DealHoleCards> DealHoleCardsReceived;
    public event Action<CommunityCards> CommunityCardsReceived;
    public event Action<ActionRequest> ActionRequestReceived;
    public event Action<ActionResult> ActionResultReceived;
    public event Action<ActionBroadcast> ActionBroadcastReceived;
    public event Action<TurnUpdate> TurnUpdateReceived;
    public event Action<RoundEndNotice> RoundEndNoticeReceived;
    public event Action<PotUpdate> PotUpdateReceived;
    public event Action<HandResult> HandResultReceived;
    public event Action<StackUpdate> StackUpdateReceived;
    public event Action<TipResponse> TipResponseReceived;
    public event Action<ChatBroadcast> ChatBroadcastReceived;
    public event Action<JoinTableResponse> JoinTableResponseReceived;
    public event Action<LeaveTableResponse> LeaveTableResponseReceived;
    public event Action<BuyInResponse> BuyInResponseReceived;
    public event Action<SpectateResponse> SpectateResponseReceived;
    public event Action<SpectatorHoleCards> SpectatorHoleCardsReceived;
    public event Action<InactiveNotice> InactiveNoticeReceived;
    public event Action<WaitVoteRequest> WaitVoteRequestReceived;
    public event Action<WaitVoteResult> WaitVoteResultReceived;
    public event Action<RejoinResponse> RejoinResponseReceived;
    public event Action<Kick> KickReceived;
    public event Action<PokerError> ErrorReceived;

    [Header("Debug Logging")]
    public bool logIncomingMessages = false;
    public bool logIncomingPayloadSize = false;
    public bool logQueueHealth = false;
    public float stallWarnSeconds = 5f;

    public static event Action<MsgType, IMessage> MessageReceivedStatic
    {
        add => Instance.MessageReceived += value;
        remove { if (_instance != null) _instance.MessageReceived -= value; }
    }

    public static event Action<ConnectResponse> ConnectResponseReceivedStatic
    {
        add => Instance.ConnectResponseReceived += value;
        remove { if (_instance != null) _instance.ConnectResponseReceived -= value; }
    }

    public static event Action<PokerTableList> TableListReceivedStatic
    {
        add => Instance.TableListReceived += value;
        remove { if (_instance != null) _instance.TableListReceived -= value; }
    }

    public static event Action<JoinTableResponse> JoinTableResponseReceivedStatic
    {
        add => Instance.JoinTableResponseReceived += value;
        remove { if (_instance != null) _instance.JoinTableResponseReceived -= value; }
    }

    public static event Action<BuyInResponse> BuyInResponseReceivedStatic
    {
        add => Instance.BuyInResponseReceived += value;
        remove { if (_instance != null) _instance.BuyInResponseReceived -= value; }
    }

    public static event Action<SpectateResponse> SpectateResponseReceivedStatic
    {
        add => Instance.SpectateResponseReceived += value;
        remove { if (_instance != null) _instance.SpectateResponseReceived -= value; }
    }

    public static event Action<SpectatorHoleCards> SpectatorHoleCardsReceivedStatic
    {
        add => Instance.SpectatorHoleCardsReceived += value;
        remove { if (_instance != null) _instance.SpectatorHoleCardsReceived -= value; }
    }

    public static event Action<InactiveNotice> InactiveNoticeReceivedStatic
    {
        add => Instance.InactiveNoticeReceived += value;
        remove { if (_instance != null) _instance.InactiveNoticeReceived -= value; }
    }

    public static event Action<WaitVoteRequest> WaitVoteRequestReceivedStatic
    {
        add => Instance.WaitVoteRequestReceived += value;
        remove { if (_instance != null) _instance.WaitVoteRequestReceived -= value; }
    }

    public static event Action<WaitVoteResult> WaitVoteResultReceivedStatic
    {
        add => Instance.WaitVoteResultReceived += value;
        remove { if (_instance != null) _instance.WaitVoteResultReceived -= value; }
    }

    public static event Action<RoundEndNotice> RoundEndNoticeReceivedStatic
    {
        add => Instance.RoundEndNoticeReceived += value;
        remove { if (_instance != null) _instance.RoundEndNoticeReceived -= value; }
    }

    public static event Action<TipResponse> TipResponseReceivedStatic
    {
        add => Instance.TipResponseReceived += value;
        remove { if (_instance != null) _instance.TipResponseReceived -= value; }
    }

    public static event Action<ChatBroadcast> ChatBroadcastReceivedStatic
    {
        add => Instance.ChatBroadcastReceived += value;
        remove { if (_instance != null) _instance.ChatBroadcastReceived -= value; }
    }

    public static event Action<RejoinResponse> RejoinResponseReceivedStatic
    {
        add => Instance.RejoinResponseReceived += value;
        remove { if (_instance != null) _instance.RejoinResponseReceived -= value; }
    }

    static GameServerClient CreateSingleton()
    {
        var go = new GameObject("GameServerClient");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<GameServerClient>();
        return _instance;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Application.runInBackground = true;
        _instance = this;
        DontDestroyOnLoad(gameObject);
#if !UNITY_WEBGL || UNITY_EDITOR
        _recvSignal = new AutoResetEvent(false);
        StartReceiver();
#else
        _recvSignal = null;
#endif
    }

    void Update()
    {
        DrainMainThreadQueue();

#if UNITY_WEBGL && !UNITY_EDITOR
        ProcessRecvQueue();
#endif

        if (!isConnected || _socket == null || !_socket.IsOpen)
        {
            TryReconnect();
            return;
        }

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (lastInboundUnixMs == 0)
            lastInboundUnixMs = nowMs;

        if (Time.unscaledTime >= _nextPingTime)
        {
            _nextPingTime = Time.unscaledTime + pingIntervalSec;
            SendPing();
        }

        if (resumeWindowSec > 0 && nowMs - lastInboundUnixMs > ResumeStallMs && nowMs - lastResumeUnixMs > ResumeCooldownMs)
        {
            lastResumeUnixMs = nowMs;
            Debug.LogWarning($"[WS] Inbound stalled. Sending ResumeRequest lastSeq={lastReceivedSeq}");
            SendResume(lastReceivedSeq);
        }

        if (logQueueHealth && stallWarnSeconds > 0f)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            if (_lastRecvTicks > 0)
            {
                var idleRecv = (nowTicks - _lastRecvTicks) / (double)TimeSpan.TicksPerSecond;
                if (idleRecv > stallWarnSeconds)
                    Debug.LogWarning($"GameServerClient: no packets received for {idleRecv:0.0}s (connected={isConnected}).");
            }
            if (_lastHandledTicks > 0)
            {
                var idleHandle = (nowTicks - _lastHandledTicks) / (double)TimeSpan.TicksPerSecond;
                if (idleHandle > stallWarnSeconds)
                    Debug.LogWarning($"GameServerClient: main thread not handling packets for {idleHandle:0.0}s (queue={_mainThreadQueue.Count}).");
            }
        }
    }

    void OnDisable()
    {
        StopReceiver();
        Close();
    }

    void OnDestroy()
    {
        StopReceiver();
        Close();
        _recvSignal?.Dispose();
        if (_instance == this)
            _instance = null;
    }

    public void SetServerUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
            serverUrl = url;
    }

    public void Connect(string launchToken, string operatorPublicId)
    {
        if (string.IsNullOrEmpty(launchToken) || string.IsNullOrEmpty(operatorPublicId))
        {
            Debug.LogWarning("GameServerClient: launchToken/operatorPublicId required.");
            return;
        }

        if (_isConnecting || isConnected || (_socket != null && _socket.IsOpen))
        {
            Debug.LogWarning("GameServerClient: connect skipped (already connecting/connected).");
            return;
        }

        // reset local session state before a fresh connect
        playerId = string.Empty;
        sessionId = string.Empty;
        tableId = string.Empty;
        lastReceivedSeq = 0;
        _seq = 0;
        resumeWindowSec = 0;
        lastInboundUnixMs = 0;
        lastResumeUnixMs = 0;

        _pendingLaunchToken = launchToken;
        _pendingOperatorPublicId = operatorPublicId;
        ResetReconnectState();
        _isConnecting = true;
        Open();
    }

    public static void Configure(string url, uint protocolVersionOverride = 0)
    {
        var client = Instance;
        if (!string.IsNullOrEmpty(url))
            client.SetServerUrl(url);
        if (protocolVersionOverride != 0)
            client.protocolVersion = protocolVersionOverride;
    }

    public static void ConnectWithLaunchToken(string launchToken, string operatorPublicId)
    {
        Instance.Connect(launchToken, operatorPublicId);
    }

    public static void ForceConnectWithLaunchToken(string launchToken, string operatorPublicId)
    {
        Instance.ForceConnect(launchToken, operatorPublicId);
    }

    public static void SendJoinTableStatic(string tableIdValue)
    {
        Instance.SendJoinTable(tableIdValue);
    }

    public static void SendJoinTableStatic(string tableIdValue, int matchSizeId)
    {
        Debug.Log("sending to server");
        Instance.SendJoinTable(tableIdValue, matchSizeId);
    }

    public void SendJoinTable(string tableIdValue, int matchSizeId)
    {
        var req = new JoinTableRequest
        {
            TableId = tableIdValue ?? "",
            MatchSizeId = matchSizeId
        };
        SendPacket(MsgType.JoinTableRequest, req);
    }

    public static void SendBuyInStatic(string tableIdValue, long amount)
    {
        Instance.SendBuyIn(tableIdValue, amount);
    }

    public static void SendSpectateStatic(string tableIdValue)
    {
        Instance.SendSpectate(tableIdValue);
    }

    public static void SendTipStatic(string tableIdValue, long amount)
    {
        Instance.SendTip(tableIdValue, amount);
    }

    public static void SendChatStatic(string message)
    {
        Instance.SendChat(null, message);
    }

    public static void SendChatStatic(string tableIdValue, string message)
    {
        Instance.SendChat(tableIdValue, message);
    }

    public static void SendWaitVoteResponseStatic(string tableIdValue, string targetPlayerId, bool wait)
    {
        Instance.SendWaitVoteResponse(tableIdValue, targetPlayerId, wait);
    }

    public static void SendRejoinStatic(string tableIdValue)
    {
        Instance.SendRejoin(tableIdValue);
    }

    public static void SendLeaveTableStatic(string reason)
    {
        Instance.SendLeaveTable(reason);
    }

    public static void SendActionStatic(PokerActionType action, long amount)
    {
        Instance.SendAction(action, amount);
    }

    // Convenience static wrappers (use these in UI button handlers)
    public static void SendFoldStatic() => Instance.SendFold();
    public static void SendCheckStatic() => Instance.SendCheck();
    public static void SendCallStatic(long callAmount) => Instance.SendCall(callAmount);
    public static void SendBetStatic(long amount) => Instance.SendBet(amount);
    public static void SendRaiseStatic(long raiseAmount) => Instance.SendRaise(raiseAmount);
    public static void SendAllInStatic(long amount) => Instance.SendAllIn(amount);

    public static void SendResumeStatic(ulong lastSeq)
    {
        Instance.SendResume(lastSeq);
    }

    public void Close()
    {
        if (_socket == null)
            return;

        _socket.Close();
        _socket = null;
        isConnected = false;
        _isConnecting = false;
    }

    public void CloseAndSuppressReconnect()
    {
        _suppressNextReconnect = true;
        Close();
    }

    public void SendJoinTable(string tableIdValue)
    {
        var req = new JoinTableRequest { TableId = tableIdValue ?? "" };
        SendPacket(MsgType.JoinTableRequest, req);
    }

    public void ForceConnect(string launchToken, string operatorPublicId)
    {
        Close();
        Connect(launchToken, operatorPublicId);
    }

    public void SendBuyIn(string tableIdValue, long amount)
    {
        var req = new BuyInRequest
        {
            TableId = tableIdValue ?? "",
            Amount = amount
        };
        SendPacket(MsgType.BuyinRequest, req);
    }

    public void SendTip(string tableIdValue, long amount)
    {
        var req = new TipRequest
        {
            TableId = tableIdValue ?? "",
            Amount = amount
        };
        SendPacket(MsgType.TipRequest, req);
    }

    public void SendChat(string tableIdValue, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        if (string.IsNullOrWhiteSpace(tableIdValue))
            tableIdValue = tableId;
        var req = new ChatRequest
        {
            TableId = tableIdValue ?? "",
            Message = message
        };
        SendPacket(MsgType.ChatRequest, req);
    }

    public void SendSpectate(string tableIdValue)
    {
        var req = new SpectateRequest { TableId = tableIdValue ?? "" };
        SendPacket(MsgType.SpectateRequest, req);
    }

    public void SendWaitVoteResponse(string tableIdValue, string targetPlayerId, bool wait)
    {
        var req = new WaitVoteResponse
        {
            TableId = tableIdValue ?? "",
            TargetPlayerId = targetPlayerId ?? "",
            Wait = wait
        };
        SendPacket(MsgType.WaitVoteResponse, req);
    }

    public void SendRejoin(string tableIdValue)
    {
        var req = new RejoinRequest { TableId = tableIdValue ?? "" };
        SendPacket(MsgType.RejoinRequest, req);
    }

    public void SendLeaveTable(string reason)
    {
        var req = new LeaveTableRequest { Reason = reason ?? "" };
        SendPacket(MsgType.LeaveTableRequest, req);
    }

    public void SendAction(PokerActionType action, long amount)
    {
        var req = new ActionRequest
        {
            TableId = tableId ?? "",
            Action = action,
            Amount = amount
        };
        SendPacket(MsgType.ActionRequest, req);
    }

    // Convenience instance methods
    public void SendFold() => SendAction(PokerActionType.Fold, 0);
    public void SendCheck() => SendAction(PokerActionType.Check, 0);
    public void SendCall(long callAmount) => SendAction(PokerActionType.Call, callAmount);
    public void SendBet(long amount) => SendAction(PokerActionType.Bet, amount);
    public void SendRaise(long raiseAmount) => SendAction(PokerActionType.Raise, raiseAmount);
    public void SendAllIn(long amount) => SendAction(PokerActionType.AllIn, amount);

    public void SendResume(ulong lastSeq)
    {
        var req = new ResumeRequest { LastSeq = lastSeq };
        SendPacket(MsgType.ResumeRequest, req);
    }

    void Open()
    {
        Close();

        var uri = new Uri(serverUrl);
        _socket = new WebSocket(uri);
        _socket.OnOpen += OnOpen;
        _socket.OnBinary += OnBinary;
        _socket.OnClosed += OnClosed;
        _socket.OnError += OnError;
        _socket.Open();
    }

    void OnOpen(WebSocket ws)
    {
        isConnected = true;
        _nextPingTime = Time.unscaledTime + pingIntervalSec;
        Debug.Log($"GameServerClient: connected to {serverUrl}.");
        SendConnectRequest();
    }

    void OnBinary(WebSocket ws, byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if (data.Length > maxPacketBytes)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("GameServerClient: packet too large.");
                Close();
            });
            return;
        }

        var copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);
        _recvQueue.Enqueue(copy);
        _recvSignal?.Set();
        _lastRecvTicks = DateTime.UtcNow.Ticks;
    }

    void OnClosed(WebSocket ws, ushort code, string message)
    {
        isConnected = false;
        _isConnecting = false;
        Debug.LogWarning($"GameServerClient: disconnected code={code} message={message}");
        ScheduleReconnect($"closed ({code}) {message}");
    }

    void OnError(WebSocket ws, string reason)
    {
        isConnected = false;
        _isConnecting = false;
        Debug.LogWarning("GameServerClient: socket error " + reason);
        ScheduleReconnect($"error {reason}");
    }

    void StartReceiver()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if (_running)
            return;

        _running = true;
        _recvThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "GameServerClientRecv"
        };
        _recvThread.Start();
#endif
    }

    void StopReceiver()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if (!_running)
            return;

        _running = false;
        _recvSignal?.Set();
        _recvThread?.Join(200);
        _recvThread = null;
#endif
    }

    void ReceiveLoop()
    {
        while (_running)
        {
            _recvSignal?.WaitOne(200);
            ProcessRecvQueue();
        }
    }

    void ProcessRecvQueue()
    {
        while (_recvQueue.TryDequeue(out var data))
        {
            if (!TryParsePacket(data, out var packet))
                continue;

            if (!ValidatePacket(packet))
                continue;

            HandlePacket(packet);
        }
    }

    bool TryParsePacket(byte[] data, out Packet packet)
    {
        packet = null;
        try
        {
            packet = Packet.Parser.ParseFrom(data);
            return true;
        }
        catch
        {
            EnqueueMainThread(() => Debug.LogWarning("GameServerClient: invalid packet."));
            return false;
        }
    }

    bool ValidatePacket(Packet packet)
    {
        if (packet == null)
            return false;

        if (packet.ProtocolVersion != protocolVersion)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("GameServerClient: protocol mismatch.");
                Close();
            });
            return false;
        }

        if (!Enum.IsDefined(typeof(MsgType), (int)packet.MsgType))
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("GameServerClient: invalid msg_type.");
                Close();
            });
            return false;
        }

        if (!string.IsNullOrEmpty(sessionId) && (MsgType)packet.MsgType != MsgType.ConnectResponse)
        {
            if (!string.Equals(packet.SessionId, sessionId, StringComparison.Ordinal))
            {
                EnqueueMainThread(() =>
                {
                    Debug.LogWarning("GameServerClient: session mismatch.");
                    Close();
                });
                return false;
            }
        }

        if (packet.Seq != 0 && packet.Seq <= lastReceivedSeq)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("GameServerClient: invalid seq.");
                Close();
            });
            return false;
        }

        if (packet.Seq != 0)
            lastReceivedSeq = packet.Seq;

        lastInboundUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return true;
    }

    void HandlePacket(Packet packet)
    {
        var msgType = (MsgType)packet.MsgType;
        var payload = packet.Payload ?? ByteString.Empty;

        if (logIncomingMessages)
        {
            var size = payload.Length;
            Debug.Log(logIncomingPayloadSize
                ? $"GameServerClient: recv {msgType} payload={size}B seq={packet.Seq} ts={packet.Timestamp}"
                : $"GameServerClient: recv {msgType} seq={packet.Seq}");
        }

        switch (msgType)
        {
            case MsgType.Ping:
                EnqueueMainThread(SendPong);
                break;
            case MsgType.Pong:
                break;
            case MsgType.ConnectResponse:
                if (TryParsePayload(payload, ConnectResponse.Parser, out var connectResponse))
                {
                    EnqueueMainThread(() =>
                    {
                        playerId = connectResponse.PlayerId;
                        sessionId = connectResponse.SessionId;
                        credits = connectResponse.Credits;
                        resumeWindowSec = connectResponse.ResumeWindowSec;
                        _isConnecting = false;
                        if (_shouldSendResume && _resumeSeq > 0)
                        {
                            Debug.Log($"GameServerClient: sending resume lastSeq={_resumeSeq}.");
                            SendResume(_resumeSeq);
                            _shouldSendResume = false;
                        }
                        ConnectResponseReceived?.Invoke(connectResponse);
                        MessageReceived?.Invoke(msgType, connectResponse);
                        Debug.Log(playerId + " Credit: " + credits);
                        Debug.Log($"{playerId} Credit: {credits}");
                        Debug.Log("myPlayerID:" + playerId);
                        // RENDER: update player HUD (player id, session, credits, protocol).
                        // Example:
                        // - show connect success toast
                        // - store playerId/sessionId for UI
                        // - update credits display: connectResponse.Credits
                    });
                }
                break;
            case MsgType.ResumeResponse:
                if (TryParsePayload(payload, ResumeResponse.Parser, out var resumeResponse))
                    EnqueueMainThread(() =>
                    {
                        ResumeResponseReceived?.Invoke(resumeResponse);
                        MessageReceived?.Invoke(msgType, resumeResponse);

                        // RENDER: show resume result and restore table UI from snapshot.
                        // Example:
                        // - if resumeResponse.Success, render resumeResponse.Snapshot
                        // - else show resumeResponse.Message
                    });
                break;
            case MsgType.TableList:
                if (TryParsePayload(payload, PokerTableList.Parser, out var tableList))
                    EnqueueMainThread(() =>
                    {
                        TableListReceived?.Invoke(tableList);
                        MessageReceived?.Invoke(msgType, tableList);
                        Debug.Log("Recieved table list with " + tableList.Tables.Count + " tables.");
                        // RENDER: build table list UI.
                        // Example:
                        foreach (var table in tableList.Tables)
                        {
                            var code = table.TableCode;
                            var name = table.TableName;
                            var smallBlind = table.SmallBlind;
                            var bigBlind = table.BigBlind;
                            var maxPlayers = table.MaxPlayers;
                            var minBuyIn = table.MinBuyIn;
                            var maxBuyIn = table.MaxBuyIn;
                            // render a table card/button using these fields
                        }
                    });
                break;
            case MsgType.TableSnapshot:
                if (TryParsePayload(payload, TableSnapshot.Parser, out var snapshot))
                {

                    Debug.Log("Table Snapshot received");
                    EnqueueMainThread(() =>
                    {
                        tableId = snapshot.TableId;
                        TableSnapshotReceived?.Invoke(snapshot);
                        MessageReceived?.Invoke(msgType, snapshot);

                        // RENDER: update full table state (seats, stacks, pot, board).
                        // Example:
                        // - snapshot.State, snapshot.CommunityCards, snapshot.PotTotal
                        // - snapshot.Players list: seat, stack, status, bet
                        // - snapshot.CurrentTurnSeat highlight
                    });
                }
                break;
            case MsgType.DealHoleCards:
                if (TryParsePayload(payload, DealHoleCards.Parser, out var holeCards))
                    EnqueueMainThread(() =>
                    {
                        DealHoleCardsReceived?.Invoke(holeCards);
                        MessageReceived?.Invoke(msgType, holeCards);
                        Debug.Log("Cards are given to players");
                        // RENDER: show player's 2 private hole cards.
                        // Example:
                        foreach (var card in holeCards.Cards)
                        {
                            var rank = card.Rank;
                            var suit = card.Suit;
                            // render each hole card sprite/model
                        }
                    });
                break;
            case MsgType.CommunityCards:
                if (TryParsePayload(payload, CommunityCards.Parser, out var communityCards))
                    EnqueueMainThread(() =>
                    {
                        CommunityCardsReceived?.Invoke(communityCards);
                        MessageReceived?.Invoke(msgType, communityCards);

                        // RENDER: update board cards for the street (Flop/Turn/River).
                        // Example:
                        foreach (var card in communityCards.Cards)
                        {
                            var rank = card.Rank;
                            var suit = card.Suit;
                            // render community card
                        }
                    });
                break;
            case MsgType.ActionRequest:
                if (TryParsePayload(payload, ActionRequest.Parser, out var actionRequest))
                    EnqueueMainThread(() =>
                    {
                        ActionRequestReceived?.Invoke(actionRequest);
                        MessageReceived?.Invoke(msgType, actionRequest);

                        // RENDER: if server asks for action, show action buttons.
                        // Example:
                        // - actionRequest.TableId
                        // - actionRequest.Action
                        // - actionRequest.Amount
                    });
                break;
            case MsgType.ActionResult:
                if (TryParsePayload(payload, ActionResult.Parser, out var actionResult))
                    EnqueueMainThread(() =>
                    {
                        ActionResultReceived?.Invoke(actionResult);
                        MessageReceived?.Invoke(msgType, actionResult);

                        // RENDER: update local player UI with result of action.
                        // Example:
                        // - success/fail message
                        // - update stack/bet/current_bet/min_raise
                    });
                break;
            case MsgType.ActionBroadcast:
                if (TryParsePayload(payload, ActionBroadcast.Parser, out var actionBroadcast))
                    EnqueueMainThread(() =>
                    {
                        ActionBroadcastReceived?.Invoke(actionBroadcast);
                        MessageReceived?.Invoke(msgType, actionBroadcast);

                        // RENDER: show other players' actions (fold/call/raise/bet).
                        // Example:
                        // - actionBroadcast.PlayerId
                        // - actionBroadcast.Action, Amount
                        // - update pot/current_bet
                    });
                break;
            case MsgType.TurnUpdate:
                if (TryParsePayload(payload, TurnUpdate.Parser, out var turnUpdate))
                    EnqueueMainThread(() =>
                    {
                        TurnUpdateReceived?.Invoke(turnUpdate);
                        MessageReceived?.Invoke(msgType, turnUpdate);

                        var turn = turnUpdate;

                        // Cache for UI logic
                        bool canFold = turn.AllowedActions.Contains(PokerActionType.Fold);
                        bool canCheck = turn.AllowedActions.Contains(PokerActionType.Check);
                        bool canCall = turn.AllowedActions.Contains(PokerActionType.Call);
                        bool canBet = turn.AllowedActions.Contains(PokerActionType.Bet);
                        bool canRaise = turn.AllowedActions.Contains(PokerActionType.Raise);
                        bool canAllIn = turn.AllowedActions.Contains(PokerActionType.AllIn);

                        Debug.Log("TurnUpdate player=" + turn.PlayerId + " seat = " + turn.Seat);

                        // RENDER: highlight current turn + enable allowed buttons.
                        // Example:
                        // - enable buttons based on canFold/canCheck/canCall/canBet/canRaise/canAllIn
                        // - show call_amount/min_raise/max_raise/stack
                    });
                break;
            case MsgType.RoundEndNotice:
                if (TryParsePayload(payload, RoundEndNotice.Parser, out var roundEnd))
                {
                    EnqueueMainThread(() =>
                    {
                        RoundEndNoticeReceived?.Invoke(roundEnd);
                        MessageReceived?.Invoke(msgType, roundEnd);

                        Debug.Log("RoundEndNotice: completed_street=" + roundEnd.CompletedStreet);
                        // RENDER: show end-of-round animation before next street.
                        // Example:
                        // - roundEnd.CompletedStreet
                        // - roundEnd.DelaySeconds
                        // - roundEnd.NextStreet
                    });
                }
                break;
            case MsgType.PotUpdate:
                if (TryParsePayload(payload, PotUpdate.Parser, out var potUpdate))
                    EnqueueMainThread(() =>
                    {
                        PotUpdateReceived?.Invoke(potUpdate);
                        MessageReceived?.Invoke(msgType, potUpdate);

                        // RENDER: update pot and side pots.
                        // Example:
                        // - potUpdate.PotTotal
                        // - potUpdate.Pots list (amount + eligible_player_ids)
                    });
                break;
            case MsgType.HandResult:
                if (TryParsePayload(payload, HandResult.Parser, out var handResult))
                    EnqueueMainThread(() =>
                    {
                        HandResultReceived?.Invoke(handResult);
                        MessageReceived?.Invoke(msgType, handResult);
                        Debug.Log($"[HandResult] table={handResult.TableId} pot={handResult.PotTotal} rake={handResult.RakeAmount} percent={handResult.RakePercent} cap={handResult.RakeCap}");

                        // RENDER: show winners and showdown hands.
                        // Example:
                        foreach (var winner in handResult.Winners)
                        {
                            var winnerId = winner.PlayerId;
                            var winAmount = winner.Amount;
                            var rank = winner.Rank;
                            // render winner banner + best five cards
                        }
                        foreach (var revealed in handResult.RevealedHands)
                        {
                            var pid = revealed.PlayerId;
                            // render revealed hole cards for showdown
                        }
                    });
                break;
            case MsgType.StackUpdate:
                if (TryParsePayload(payload, StackUpdate.Parser, out var stackUpdate))
                    EnqueueMainThread(() =>
                    {
                        StackUpdateReceived?.Invoke(stackUpdate);
                        MessageReceived?.Invoke(msgType, stackUpdate);

                        // RENDER: update player stack UI.
                        // Example:
                        // - stackUpdate.PlayerId
                        // - stackUpdate.Stack
                    });
                break;
            case MsgType.TipResponse:
                if (TryParsePayload(payload, TipResponse.Parser, out var tipResp))
                {
                    EnqueueMainThread(() =>
                    {
                        TipResponseReceived?.Invoke(tipResp);
                        MessageReceived?.Invoke(msgType, tipResp);
                    });
                }
                break;
            case MsgType.ChatBroadcast:
                if (TryParsePayload(payload, ChatBroadcast.Parser, out var chat))
                {
                    EnqueueMainThread(() =>
                    {
                        ChatBroadcastReceived?.Invoke(chat);
                        MessageReceived?.Invoke(msgType, chat);
                    });
                }
                break;
            case MsgType.JoinTableResponse:
                if (TryParsePayload(payload, JoinTableResponse.Parser, out var joinTableResponse))
                {
                    EnqueueMainThread(() =>
                    {
                        Debug.Log("server response join" + msgType + joinTableResponse);
                        if (joinTableResponse.Success)
                            tableId = joinTableResponse.TableId;
                        JoinTableResponseReceived?.Invoke(joinTableResponse);
                        MessageReceived?.Invoke(msgType, joinTableResponse);
                        // RENDER: show join result + seat assignment.
                        // Example:
                        // - joinTableResponse.Seat
                        // - joinTableResponse.SmallBlind / BigBlind
                    });
                }
                break;
            case MsgType.BuyinResponse:
                if (TryParsePayload(payload, BuyInResponse.Parser, out var buyInResponse))
                {
                    EnqueueMainThread(() =>
                    {
                        BuyInResponseReceived?.Invoke(buyInResponse);
                        MessageReceived?.Invoke(msgType, buyInResponse);

                        // RENDER: show buy-in result and updated balance/credits.
                        // Example:
                        // - buyInResponse.Success, Message
                        // - buyInResponse.Amount, Stack
                    });
                }
                break;
            case MsgType.SpectateResponse:
                if (TryParsePayload(payload, SpectateResponse.Parser, out var spectateResponse))
                {
                    EnqueueMainThread(() =>
                    {
                        SpectateResponseReceived?.Invoke(spectateResponse);
                        MessageReceived?.Invoke(msgType, spectateResponse);

                        // RENDER: show spectator state (success/fail + message).
                    });
                }
                break;
            case MsgType.SpectatorHoleCards:
                if (TryParsePayload(payload, SpectatorHoleCards.Parser, out var spectatorCards))
                {
                    EnqueueMainThread(() =>
                    {
                        SpectatorHoleCardsReceived?.Invoke(spectatorCards);
                        MessageReceived?.Invoke(msgType, spectatorCards);

                        // RENDER: show player's hole cards (spectator mode only).
                        var pid = spectatorCards.PlayerId;
                        foreach (var card in spectatorCards.Cards)
                        {
                            var rank = card.Rank;
                            var suit = card.Suit;
                            // render card for pid in spectator UI
                        }
                    });
                }
                break;
            case MsgType.InactiveNotice:
                if (TryParsePayload(payload, InactiveNotice.Parser, out var inactiveNotice))
                {
                    EnqueueMainThread(() =>
                    {
                        InactiveNoticeReceived?.Invoke(inactiveNotice);
                        MessageReceived?.Invoke(msgType, inactiveNotice);
                    });
                }
                break;
            case MsgType.WaitVoteRequest:
                if (TryParsePayload(payload, WaitVoteRequest.Parser, out var voteReq))
                {
                    EnqueueMainThread(() =>
                    {
                        WaitVoteRequestReceived?.Invoke(voteReq);
                        MessageReceived?.Invoke(msgType, voteReq);
                    });
                }
                break;
            case MsgType.WaitVoteResult:
                if (TryParsePayload(payload, WaitVoteResult.Parser, out var voteResult))
                {
                    EnqueueMainThread(() =>
                    {
                        WaitVoteResultReceived?.Invoke(voteResult);
                        MessageReceived?.Invoke(msgType, voteResult);
                    });
                }
                break;
            case MsgType.RejoinResponse:
                if (TryParsePayload(payload, RejoinResponse.Parser, out var rejoinResp))
                {
                    EnqueueMainThread(() =>
                    {
                        RejoinResponseReceived?.Invoke(rejoinResp);
                        MessageReceived?.Invoke(msgType, rejoinResp);
                    });
                }
                break;
            case MsgType.LeaveTableResponse:
                if (TryParsePayload(payload, LeaveTableResponse.Parser, out var leaveTableResponse))
                {
                    EnqueueMainThread(() =>
                    {
                        if (leaveTableResponse.Success)
                            tableId = string.Empty;
                        LeaveTableResponseReceived?.Invoke(leaveTableResponse);
                        MessageReceived?.Invoke(msgType, leaveTableResponse);

                        // RENDER: show leave result and reset table UI.
                    });
                }
                break;
            case MsgType.Kick:
                if (TryParsePayload(payload, Kick.Parser, out var kick))
                    EnqueueMainThread(() =>
                    {
                        KickReceived?.Invoke(kick);
                        MessageReceived?.Invoke(msgType, kick);

                        // RENDER: show kick dialog and return to lobby.
                    });
                break;
            case MsgType.Error:
                if (TryParsePayload(payload, PokerError.Parser, out var error))
                    EnqueueMainThread(() =>
                    {
                        ErrorReceived?.Invoke(error);
                        MessageReceived?.Invoke(msgType, error);

                        // RENDER: show error banner/toast.
                    });
                break;
        }

        _lastHandledTicks = DateTime.UtcNow.Ticks;
    }

    bool TryParsePayload<T>(ByteString payload, MessageParser<T> parser, out T message) where T : class, IMessage<T>
    {
        message = null;
        if (payload == null || payload.Length == 0)
            return false;

        try
        {
            message = parser.ParseFrom(payload);
            return true;
        }
        catch
        {
            EnqueueMainThread(() => Debug.LogWarning("GameServerClient: invalid payload."));
            return false;
        }
    }

    void SendConnectRequest()
    {
        var request = new ConnectRequest
        {
            GameTicket = _pendingLaunchToken ?? string.Empty,
            AccessToken = string.Empty,
            ClientVersion = string.IsNullOrEmpty(clientVersionOverride) ? Application.version : clientVersionOverride,
            DeviceId = string.IsNullOrEmpty(deviceIdOverride) ? SystemInfo.deviceUniqueIdentifier : deviceIdOverride,
            OperatorPublicId = _pendingOperatorPublicId ?? string.Empty
        };

        SendPacket(MsgType.ConnectRequest, request);
    }

    void SendPing()
    {
        var ping = new Com.poker.Core.Ping { TimestampUnixMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        SendPacket(MsgType.Ping, ping);
    }

    void SendPong()
    {
        var pong = new Pong { TimestampUnixMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        SendPacket(MsgType.Pong, pong);
    }

    void SendPacket(MsgType msgType, IMessage message)
    {
        if (_socket == null || !_socket.IsOpen)
            return;

        if (msgType != MsgType.ConnectRequest && string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("GameServerClient: session not ready. Wait for ConnectResponse before sending.");
            return;
        }

        var packet = new Packet
        {
            ProtocolVersion = protocolVersion,
            MsgType = (uint)msgType,
            Seq = ++_seq,
            SessionId = sessionId ?? string.Empty,
            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Payload = message != null ? message.ToByteString() : ByteString.Empty
        };

        _socket.Send(packet.ToByteArray());
    }

    void ScheduleReconnect(string reason)
    {
        if (_suppressNextReconnect)
        {
            _suppressNextReconnect = false;
            return;
        }

        if (!autoReconnect)
            return;

        if (string.IsNullOrEmpty(_pendingLaunchToken) || string.IsNullOrEmpty(_pendingOperatorPublicId))
        {
            Debug.LogWarning("GameServerClient: reconnect skipped (missing launch token/operator id).");
            return;
        }

        if (!_reconnectPending)
        {
            _reconnectPending = true;
            _currentReconnectDelay = Mathf.Max(0.2f, reconnectInitialDelaySec);
            _reconnectAttempts = 0;
        }

        if (lastReceivedSeq > 0)
        {
            _resumeSeq = lastReceivedSeq;
            _shouldSendResume = true;
        }

        Debug.LogWarning($"GameServerClient: scheduling reconnect ({reason}).");
    }

    void TryReconnect()
    {
        if (!autoReconnect || !_reconnectPending)
            return;

        if (reconnectMaxAttempts > 0 && _reconnectAttempts >= reconnectMaxAttempts)
            return;

        if (Time.unscaledTime < _nextReconnectTime)
            return;

        _reconnectAttempts++;
        _nextReconnectTime = Time.unscaledTime + _currentReconnectDelay;
        _currentReconnectDelay = Mathf.Min(reconnectMaxDelaySec, _currentReconnectDelay * 1.6f);

        Debug.LogWarning($"GameServerClient: reconnect attempt {_reconnectAttempts} (delay={_currentReconnectDelay:0.00}s).");
        Open();
    }

    void ResetReconnectState()
    {
        _reconnectPending = false;
        _currentReconnectDelay = 0f;
        _reconnectAttempts = 0;
        _nextReconnectTime = 0f;
        _shouldSendResume = false;
        _resumeSeq = 0;
    }

    void EnqueueMainThread(Action action)
    {
        if (action != null)
            _mainThreadQueue.Enqueue(action);
    }

    void DrainMainThreadQueue()
    {
        var backlog = _mainThreadQueue.Count;
        if (backlog > _catchUpQueueThreshold)
            IsCatchingUp = true;

        var processed = 0;
        while (processed < _maxMainThreadActionsPerFrame && _mainThreadQueue.TryDequeue(out var action))
        {
            processed++;
            action.Invoke();
        }

        if (IsCatchingUp && _mainThreadQueue.Count <= _catchUpQueueThreshold / 2)
            IsCatchingUp = false;
    }

}

/*
USAGE EXAMPLE (UI BUTTONS)
-------------------------
// Connect:
GameServerClient.Configure("ws://51.79.160.227:26001/ws");
GameServerClient.ConnectWithLaunchToken(launchToken, operatorPublicId);

// Subscribe to server messages:
GameServerClient.MessageReceivedStatic += OnMessage;
GameServerClient.ConnectResponseReceivedStatic += OnConnect;
GameServerClient.JoinTableResponseReceivedStatic += OnJoinTable;

void OnConnect(ConnectResponse resp)
{
    // When connected, request table list or join a specific table
    // Example: buy-in then join
    GameServerClient.SendBuyInStatic(tableCode, buyInAmount);
}

void OnJoinTable(JoinTableResponse resp)
{
    // Ready to play
}

// Action buttons:
public void OnFoldClicked()  => GameServerClient.SendFoldStatic();
public void OnCheckClicked() => GameServerClient.SendCheckStatic();
public void OnCallClicked(long callAmount) => GameServerClient.SendCallStatic(callAmount);
public void OnBetClicked(long amount)  => GameServerClient.SendBetStatic(amount);
public void OnRaiseClicked(long amount)=> GameServerClient.SendRaiseStatic(amount);
public void OnAllInClicked(long amount)=> GameServerClient.SendAllInStatic(amount);

// Vote / Rejoin:
public void OnVoteWaitYes(string tableId, string targetPlayerId)
    => GameServerClient.SendWaitVoteResponseStatic(tableId, targetPlayerId, true);
public void OnVoteWaitNo(string tableId, string targetPlayerId)
    => GameServerClient.SendWaitVoteResponseStatic(tableId, targetPlayerId, false);
public void OnRejoin(string tableId)
    => GameServerClient.SendRejoinStatic(tableId);

IMPORTANT
---------
1) Only send actions when it's your turn.
2) Use TurnUpdate.AllowedActions and call_amount/min_raise/max_raise from TurnUpdate to validate
   UI input before sending (server will also validate).
3) You must be connected and in a table before sending actions.
*/
