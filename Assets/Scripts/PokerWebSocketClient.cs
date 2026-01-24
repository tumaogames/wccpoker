//#if POKER_LEGACY
using BestHTTP.WebSocket;
using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using PokerError = Com.poker.Core.Error;

public sealed class PokerWebSocketClient : MonoBehaviour
{
    [Header("UI")]
    //public UILabel queryParamLbl;

    [Header("Server")]
    public string serverUrl = "ws://127.0.0.1:26001/ws";
    public uint protocolVersion = 1;
    public int maxPacketBytes = 32768;
    public float pingIntervalSec = 15f;

    [Header("Auth")]
    public AccountAuthClient accountAuth;
    public string clientVersionOverride = "";
    public string deviceIdOverride = "";
    public string operatorPublicIdOverride = "";
    public bool autoConnectOnStart = false;

    [Header("State (read-only)")]
    [SerializeField] string playerId;
    [SerializeField] string sessionId;
    [SerializeField] string tableId;
    [SerializeField] bool isConnected;
    [SerializeField] ulong lastReceivedSeq;
    [SerializeField] uint resumeWindowSec;

    public string PlayerId => playerId;
    public string SessionId => sessionId;
    public string TableId => tableId;
    public bool IsConnected => isConnected;

    public event Action<ConnectResponse> ConnectResponseReceived;
    public event Action<ResumeResponse> ResumeResponseReceived;
    public event Action<TableSnapshot> TableSnapshotReceived;
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
    public event Action<PokerTableList> TableListReceived;
    public event Action<MsgType, IMessage> MessageReceived;
    public event Action<Kick> KickReceived;
    public event Action<PokerError> ErrorReceived;

    WebSocket _socket;
    string _pendingTicket;
    ulong _seq;
    float _nextPingTime;

    readonly ConcurrentQueue<byte[]> _recvQueue = new ConcurrentQueue<byte[]>();
    readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    AutoResetEvent _recvSignal;
    Thread _recvThread;
    volatile bool _running;

    void Awake()
    {
        _recvSignal = new AutoResetEvent(false);
    }

    void OnEnable()
    {
        StartReceiver();
    }

    void Start()
    {
        //var launchToken = LaunchTokenReader.GetQueryParam("launchToken");
        //queryParamLbl.text = launchToken;
        if (autoConnectOnStart && accountAuth != null && !string.IsNullOrEmpty(accountAuth.GameTicket))
            ConnectWithTicket(accountAuth.GameTicket);
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
        if (_recvSignal != null)
            _recvSignal.Dispose();
    }

    public void ConnectWithTicket(string ticket)
    {
        if (string.IsNullOrEmpty(ticket))
        {
            Debug.LogWarning("PokerWebSocketClient: missing game ticket.");
            return;
        }

        _pendingTicket = ticket;
        Open();
    }

    public void ConnectFromAccountAuth()
    {
        if (accountAuth == null || string.IsNullOrEmpty(accountAuth.GameTicket))
        {
            Debug.LogWarning("PokerWebSocketClient: account auth not ready.");
            return;
        }

        ConnectWithTicket(accountAuth.GameTicket);
    }

    public void SendJoinTable(string targetTableId)
    {
        if (string.IsNullOrEmpty(targetTableId))
        {
            Debug.LogWarning("PokerWebSocketClient: missing table id.");
            return;
        }

        var request = new JoinTableRequest { TableId = targetTableId };
        SendPacket(MsgType.JoinTableRequest, request);
    }

    public void SendLeaveTable(string reason)
    {
        var request = new LeaveTableRequest { Reason = reason ?? string.Empty };
        SendPacket(MsgType.LeaveTableRequest, request);
    }

    public void SendAction(PokerActionType action, long amount)
    {
        var request = new ActionRequest
        {
            TableId = tableId ?? string.Empty,
            Action = action,
            Amount = amount
        };
        SendPacket(MsgType.ActionRequest, request);
    }

    public void SendResume()
    {
        var request = new ResumeRequest { LastSeq = lastReceivedSeq };
        SendPacket(MsgType.ResumeRequest, request);
    }

    public void Close()
    {
        if (_socket == null)
            return;

        _socket.Close();
        _socket = null;
        isConnected = false;
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
        Debug.Log("WS open id=" + GetInstanceID());
        isConnected = true;
        _nextPingTime = Time.unscaledTime + pingIntervalSec;
        SendConnectRequest();
       
    }

    void OnBinary(WebSocket ws, byte[] data)
    {
        Debug.Log("WS recv bytes: " + (data != null ? data.Length : 0));

        if (data == null || data.Length == 0)
            return;

        if (data.Length > maxPacketBytes)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("PokerWebSocketClient: packet too large.");
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
        Debug.LogWarning($"WS closed code={code} message={message}");

    }

    void OnError(WebSocket ws, string reason)
    {
        isConnected = false;
        Debug.LogWarning("PokerWebSocketClient: socket error " + reason);
    }

    void StartReceiver()
    {
        if (_running)
            return;

        _running = true;
        _recvThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "PokerWsRecv"
        };
        _recvThread.Start();
    }

    void StopReceiver()
    {
        if (!_running)
            return;

        _running = false;
        if (_recvSignal != null)
            _recvSignal.Set();
        if (_recvThread != null)
            _recvThread.Join(200);
        _recvThread = null;
    }

    void ReceiveLoop()
    {
        while (_running)
        {
            if (_recvSignal != null)
                _recvSignal.WaitOne(200);

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
        catch (InvalidProtocolBufferException)
        {
            EnqueueMainThread(() => Debug.LogWarning("PokerWebSocketClient: invalid packet."));
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
                Debug.LogWarning("PokerWebSocketClient: protocol mismatch.");
                Close();
            });
            return false;
        }

        if (packet.MsgType > (uint)MsgType.Error)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("PokerWebSocketClient: invalid msg_type.");
                Close();
            });
            return false;
        }

        var msgType = (MsgType)packet.MsgType;
        if (!string.IsNullOrEmpty(sessionId) && msgType != MsgType.ConnectResponse)
        {
            if (!string.Equals(packet.SessionId, sessionId, StringComparison.Ordinal))
            {
                EnqueueMainThread(() =>
                {
                    Debug.LogWarning("PokerWebSocketClient: session mismatch.");
                    Close();
                });
                return false;
            }
        }

        if (packet.Seq != 0 && packet.Seq <= lastReceivedSeq)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("PokerWebSocketClient: invalid seq.");
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
                        resumeWindowSec = connectResponse.ResumeWindowSec;
                        ConnectResponseReceived?.Invoke(connectResponse);
                        MessageReceived?.Invoke(msgType, connectResponse);
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
            case MsgType.TableList:
                if (TryParsePayload(payload, PokerTableList.Parser, out var tableList))
                    EnqueueMainThread(() =>
                    {
                        TableListReceived?.Invoke(tableList);
                        MessageReceived?.Invoke(msgType, tableList);
                    });
                break;
            case MsgType.DealHoleCards:
                if (TryParsePayload(payload, DealHoleCards.Parser, out var holeCards))
                    EnqueueMainThread(() =>
                    {
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
                    Debug.Log("JoinTableResponse received:" + joinTableResponse.Success);
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
        catch (InvalidProtocolBufferException)
        {
            EnqueueMainThread(() => Debug.LogWarning("PokerWebSocketClient: invalid payload."));
            return false;
        }
    }

    void SendConnectRequest()
    {
        Debug.Log("Send ConnectRequest id=" + GetInstanceID() +
              " ticket=" + (string.IsNullOrEmpty(_pendingTicket) ? "EMPTY" : "OK") +
              " socketOpen=" + (_socket != null && _socket.IsOpen));

        var request = new ConnectRequest
        {
            GameTicket = _pendingTicket ?? string.Empty,
            AccessToken = accountAuth != null ? accountAuth.AccessToken : string.Empty,
            ClientVersion = string.IsNullOrEmpty(clientVersionOverride) ? Application.version : clientVersionOverride,
            DeviceId = string.IsNullOrEmpty(deviceIdOverride) ? SystemInfo.deviceUniqueIdentifier : deviceIdOverride,
            OperatorPublicId = !string.IsNullOrEmpty(operatorPublicIdOverride)
                ? operatorPublicIdOverride
                : (accountAuth != null ? accountAuth.OperatorPublicId : string.Empty)
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
        {
            Debug.LogWarning("SendPacket dropped: socket not open");
            return;
        }

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
        if (action == null)
            return;

        _mainThreadQueue.Enqueue(action);
    }

    void DrainMainThreadQueue()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
            action.Invoke();
    }
}
//#endif
