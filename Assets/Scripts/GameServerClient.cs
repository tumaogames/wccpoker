using BestHTTP.WebSocket;
using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Threading;
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

    [Header("State (read-only)")]
    [SerializeField] string playerId;
    [SerializeField] long credit;
    [SerializeField] string sessionId;
    [SerializeField] string tableId;
    [SerializeField] bool isConnected;
    [SerializeField] ulong lastReceivedSeq;
    [SerializeField] uint resumeWindowSec;

    public string PlayerId => playerId;
    public string SessionId => sessionId;
    public string TableId => tableId;
    public bool IsConnected => isConnected;

    WebSocket _socket;
    string _pendingLaunchToken;
    string _pendingOperatorPublicId;
    ulong _seq;
    float _nextPingTime;

    readonly ConcurrentQueue<byte[]> _recvQueue = new ConcurrentQueue<byte[]>();
    readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    AutoResetEvent _recvSignal;
    Thread _recvThread;
    volatile bool _running;

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
    public event Action<PotUpdate> PotUpdateReceived;
    public event Action<HandResult> HandResultReceived;
    public event Action<StackUpdate> StackUpdateReceived;
    public event Action<JoinTableResponse> JoinTableResponseReceived;
    public event Action<LeaveTableResponse> LeaveTableResponseReceived;
    public event Action<Kick> KickReceived;
    public event Action<PokerError> ErrorReceived;

    public static event Action<MsgType, IMessage> MessageReceivedStatic
    {
        add => Instance.MessageReceived += value;
        remove => Instance.MessageReceived -= value;
    }

    public static event Action<ConnectResponse> ConnectResponseReceivedStatic
    {
        add => Instance.ConnectResponseReceived += value;
        remove => Instance.ConnectResponseReceived -= value;
    }

    public static event Action<PokerTableList> TableListReceivedStatic
    {
        add => Instance.TableListReceived += value;
        remove => Instance.TableListReceived -= value;
    }

    public static event Action<JoinTableResponse> JoinTableResponseReceivedStatic
    {
        add => Instance.JoinTableResponseReceived += value;
        remove => Instance.JoinTableResponseReceived -= value;
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

        _instance = this;
        DontDestroyOnLoad(gameObject);
        _recvSignal = new AutoResetEvent(false);
        StartReceiver();
    }

    void Update()
    {
        DrainMainThreadQueue();

        if (!isConnected || _socket == null || !_socket.IsOpen)
            return;

        if (Time.unscaledTime >= _nextPingTime)
        {
            _nextPingTime = Time.unscaledTime + pingIntervalSec;
            SendPing();
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

        _pendingLaunchToken = launchToken;
        _pendingOperatorPublicId = operatorPublicId;
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

    public static void SendJoinTableStatic(string tableIdValue)
    {
        Instance.SendJoinTable(tableIdValue);
    }

    public static void SendLeaveTableStatic(string reason)
    {
        Instance.SendLeaveTable(reason);
    }

    public static void SendActionStatic(PokerActionType action, long amount)
    {
        Instance.SendAction(action, amount);
    }

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
    }

    public void SendJoinTable(string tableIdValue)
    {
        var req = new JoinTableRequest { TableId = tableIdValue ?? "" };
        SendPacket(MsgType.JoinTableRequest, req);
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
        _recvSignal.Set();
    }

    void OnClosed(WebSocket ws, ushort code, string message)
    {
        isConnected = false;
    }

    void OnError(WebSocket ws, string reason)
    {
        isConnected = false;
        Debug.LogWarning("GameServerClient: socket error " + reason);
    }

    void StartReceiver()
    {
        if (_running)
            return;

        _running = true;
        _recvThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "GameServerClientRecv"
        };
        _recvThread.Start();
    }

    void StopReceiver()
    {
        if (!_running)
            return;

        _running = false;
        _recvSignal?.Set();
        _recvThread?.Join(200);
        _recvThread = null;
    }

    void ReceiveLoop()
    {
        while (_running)
        {
            _recvSignal?.WaitOne(200);
            while (_recvQueue.TryDequeue(out var data))
            {
                if (!TryParsePacket(data, out var packet))
                    continue;

                if (!ValidatePacket(packet))
                    continue;

                HandlePacket(packet);
            }
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

        return true;
    }

    void HandlePacket(Packet packet)
    {
        var msgType = (MsgType)packet.MsgType;
        var payload = packet.Payload ?? ByteString.Empty;

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
                        credit = connectResponse.Credits;
                        resumeWindowSec = connectResponse.ResumeWindowSec;
                        ConnectResponseReceived?.Invoke(connectResponse);
                        MessageReceived?.Invoke(msgType, connectResponse);
                        Debug.Log(playerId + " Credit: " + credit);
                    });
                }
                break;
            case MsgType.ResumeResponse:
                if (TryParsePayload(payload, ResumeResponse.Parser, out var resumeResponse))
                    EnqueueMainThread(() =>
                    {
                        ResumeResponseReceived?.Invoke(resumeResponse);
                        MessageReceived?.Invoke(msgType, resumeResponse);
                    });
                break;
            case MsgType.TableList:
                if (TryParsePayload(payload, PokerTableList.Parser, out var tableList))
                    EnqueueMainThread(() =>
                    {
                        TableListReceived?.Invoke(tableList);
                        MessageReceived?.Invoke(msgType, tableList);
                        Debug.Log("Recieved table list with " + tableList.Tables.Count + " tables.");
                        foreach (var item in tableList.Tables)
                        {
                            Debug.Log(item.TableName);
                        }
                    });
                break;
            case MsgType.TableSnapshot:
                if (TryParsePayload(payload, TableSnapshot.Parser, out var snapshot))
                {
                    EnqueueMainThread(() =>
                    {
                        tableId = snapshot.TableId;
                        TableSnapshotReceived?.Invoke(snapshot);
                        MessageReceived?.Invoke(msgType, snapshot);
                    });
                }
                break;
            case MsgType.DealHoleCards:
                if (TryParsePayload(payload, DealHoleCards.Parser, out var holeCards))
                    EnqueueMainThread(() =>
                    {
                        var card1 = holeCards.Cards[0];
                        var card2 = holeCards.Cards[1];

                        Debug.Log($"Hole cards: {card1.Rank}-{card1.Suit}, {card2.Rank}-{card2.Suit}");

                        DealHoleCardsReceived?.Invoke(holeCards);
                        MessageReceived?.Invoke(msgType, holeCards);
                    });
                break;
            case MsgType.CommunityCards:
                if (TryParsePayload(payload, CommunityCards.Parser, out var communityCards))
                    EnqueueMainThread(() =>
                    {
                        CommunityCardsReceived?.Invoke(communityCards);
                        MessageReceived?.Invoke(msgType, communityCards);
                    });
                break;
            case MsgType.ActionRequest:
                if (TryParsePayload(payload, ActionRequest.Parser, out var actionRequest))
                    EnqueueMainThread(() =>
                    {
                        ActionRequestReceived?.Invoke(actionRequest);
                        MessageReceived?.Invoke(msgType, actionRequest);
                    });
                break;
            case MsgType.ActionResult:
                if (TryParsePayload(payload, ActionResult.Parser, out var actionResult))
                    EnqueueMainThread(() =>
                    {
                        ActionResultReceived?.Invoke(actionResult);
                        MessageReceived?.Invoke(msgType, actionResult);
                    });
                break;
            case MsgType.ActionBroadcast:
                if (TryParsePayload(payload, ActionBroadcast.Parser, out var actionBroadcast))
                    EnqueueMainThread(() =>
                    {
                        ActionBroadcastReceived?.Invoke(actionBroadcast);
                        MessageReceived?.Invoke(msgType, actionBroadcast);
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

                        Debug.Log("TurnUpdate player=" + turn.PlayerId + " seat = " + turn .Seat );
                    });
                break;
            case MsgType.PotUpdate:
                if (TryParsePayload(payload, PotUpdate.Parser, out var potUpdate))
                    EnqueueMainThread(() =>
                    {
                        PotUpdateReceived?.Invoke(potUpdate);
                        MessageReceived?.Invoke(msgType, potUpdate);
                    });
                break;
            case MsgType.HandResult:
                if (TryParsePayload(payload, HandResult.Parser, out var handResult))
                    EnqueueMainThread(() =>
                    {
                        HandResultReceived?.Invoke(handResult);
                        MessageReceived?.Invoke(msgType, handResult);
                    });
                break;
            case MsgType.StackUpdate:
                if (TryParsePayload(payload, StackUpdate.Parser, out var stackUpdate))
                    EnqueueMainThread(() =>
                    {
                        StackUpdateReceived?.Invoke(stackUpdate);
                        MessageReceived?.Invoke(msgType, stackUpdate);
                    });
                break;
            case MsgType.JoinTableResponse:
                if (TryParsePayload(payload, JoinTableResponse.Parser, out var joinTableResponse))
                {
                    EnqueueMainThread(() =>
                    {
                        if (joinTableResponse.Success)
                            tableId = joinTableResponse.TableId;
                        JoinTableResponseReceived?.Invoke(joinTableResponse);
                        MessageReceived?.Invoke(msgType, joinTableResponse);
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
                    });
                }
                break;
            case MsgType.Kick:
                if (TryParsePayload(payload, Kick.Parser, out var kick))
                    EnqueueMainThread(() =>
                    {
                        KickReceived?.Invoke(kick);
                        MessageReceived?.Invoke(msgType, kick);
                    });
                break;
            case MsgType.Error:
                if (TryParsePayload(payload, PokerError.Parser, out var error))
                    EnqueueMainThread(() =>
                    {
                        ErrorReceived?.Invoke(error);
                        MessageReceived?.Invoke(msgType, error);
                    });
                break;
        }
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

    void EnqueueMainThread(Action action)
    {
        if (action != null)
            _mainThreadQueue.Enqueue(action);
    }

    void DrainMainThreadQueue()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
            action.Invoke();
    }
}
