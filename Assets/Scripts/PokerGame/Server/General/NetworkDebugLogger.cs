using System;
using Com.poker.Core;
using Google.Protobuf;
using UnityEngine;

public class NetworkDebugLogger : MonoBehaviour
{
    public static NetworkDebugLogger Instance { get; private set; }

    [Header("Logging")]
    [SerializeField] bool logIncoming = true;
    [SerializeField] bool logOutgoing = true;
    [SerializeField] bool logRawMessage = false;

    [Header("Noisy Types")]
    [SerializeField] bool logTableSnapshots = false;
    [SerializeField] bool logTurnUpdates = true;
    [SerializeField] bool logPotUpdates = false;
    [SerializeField] bool logStackUpdates = false;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureInstance()
    {
        if (FindObjectOfType<NetworkDebugLogger>() != null)
            return;

        var go = new GameObject("NetworkDebugLogger");
        DontDestroyOnLoad(go);
        go.AddComponent<NetworkDebugLogger>();
    }
#endif

    void OnEnable()
    {
        Instance = this;
        GameServerClient.MessageReceivedStatic += OnMessage;
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
        GameServerClient.MessageReceivedStatic -= OnMessage;
    }

    public static void LogSend(string label, string details)
    {
        if (Instance == null || !Instance.logOutgoing)
            return;
        Debug.Log($"[NetSend] {label} {details}".Trim());
    }

    void OnMessage(MsgType type, IMessage msg)
    {
        if (!logIncoming || msg == null)
            return;

        switch (type)
        {
            case MsgType.ConnectResponse:
            {
                var m = (ConnectResponse)msg;
                Debug.Log($"[NetRecv] ConnectResponse playerId={m.PlayerId} sessionId={m.SessionId} credits={m.Credits} protocol={m.ProtocolVersion}");
                break;
            }
            case MsgType.JoinTableResponse:
            {
                var m = (JoinTableResponse)msg;
                Debug.Log($"[NetRecv] JoinTableResponse ok={m.Success} tableId={m.TableId} seat={m.Seat} maxPlayers={m.MaxPlayers} msg={m.Message}");
                break;
            }
            case MsgType.BuyinResponse:
            {
                var m = (BuyInResponse)msg;
                Debug.Log($"[NetRecv] BuyInResponse ok={m.Success} tableId={m.TableId} amount={m.Amount} balance={m.Balance} msg={m.Message}");
                break;
            }
            case MsgType.SpectateResponse:
            {
                var m = (SpectateResponse)msg;
                Debug.Log($"[NetRecv] SpectateResponse ok={m.Success} tableId={m.TableId} matchId={m.MatchId} msg={m.Message}");
                break;
            }
            case MsgType.RejoinResponse:
            {
                var m = (RejoinResponse)msg;
                Debug.Log($"[NetRecv] RejoinResponse ok={m.Success} tableId={m.TableId} stack={m.Stack} balance={m.Balance} msg={m.Message}");
                break;
            }
            case MsgType.TableSnapshot:
            {
                if (!logTableSnapshots)
                    break;
                var m = (TableSnapshot)msg;
                Debug.Log($"[NetRecv] TableSnapshot tableId={m.TableId} state={m.State} players={m.Players.Count} pot={m.PotTotal} currentBet={m.CurrentBet} turnSeat={m.CurrentTurnSeat}");
                break;
            }
            case MsgType.TurnUpdate:
            {
                if (!logTurnUpdates)
                    break;
                var m = (TurnUpdate)msg;
                Debug.Log($"[NetRecv] TurnUpdate playerId={m.PlayerId} seat={m.Seat} call={m.CallAmount} minRaise={m.MinRaise} maxRaise={m.MaxRaise}");
                break;
            }
            case MsgType.ActionBroadcast:
            {
                var m = (ActionBroadcast)msg;
                Debug.Log($"[NetRecv] ActionBroadcast playerId={m.PlayerId} action={m.Action} amount={m.Amount} pot={m.PotTotal}");
                break;
            }
            case MsgType.RoundEndNotice:
            {
                var m = (RoundEndNotice)msg;
                Debug.Log($"[NetRecv] RoundEndNotice tableId={m.TableId} completed={m.CompletedStreet} next={m.NextStreet} delay={m.DelaySeconds}s");
                break;
            }
            case MsgType.HandResult:
            {
                var m = (HandResult)msg;
                Debug.Log($"[NetRecv] HandResult tableId={m.TableId} winners={m.Winners.Count} potTotal={m.PotTotal}");
                break;
            }
            case MsgType.PotUpdate:
            {
                if (!logPotUpdates)
                    break;
                var m = (PotUpdate)msg;
                Debug.Log($"[NetRecv] PotUpdate tableId={m.TableId} potTotal={m.PotTotal} pots={m.Pots.Count}");
                break;
            }
            case MsgType.StackUpdate:
            {
                if (!logStackUpdates)
                    break;
                var m = (StackUpdate)msg;
                Debug.Log($"[NetRecv] StackUpdate playerId={m.PlayerId} stack={m.Stack}");
                break;
            }
            default:
            {
                if (logRawMessage)
                    Debug.Log($"[NetRecv] {type} {msg}");
                break;
            }
        }

        if (logRawMessage && type != MsgType.TableSnapshot)
            Debug.Log($"[NetRecvRaw] {type} {msg}");
    }
}
