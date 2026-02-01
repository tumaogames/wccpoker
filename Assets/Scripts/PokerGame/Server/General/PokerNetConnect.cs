////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Com.Poker.Core;

public class PokerNetConnect : MonoBehaviour
{
    [SerializeField] UnityEvent<string> _onPrintRoundStatusEvent;
    [Header("Join Settings (no connection here)")]
    [SerializeField] bool _isPlayerEnable = true;
    [SerializeField] string _playerTableCode = "";
    [SerializeField] string _botsTableCode = "DEV-BOT-TABLE";
    [SerializeField] int _defaultBotsMatchSizeId = 3;

    public static string OwnerPlayerID;
    public static event Action<MsgType, IMessage> OnMessageEvent;
    bool _joinRequested;
    bool _hasSnapshot;

    void Awake()
    {
        Application.runInBackground = true;
    }

    void OnEnable()
    {
        GameServerClient.MessageReceivedStatic += OnMessage;
        GameServerClient.ConnectResponseReceivedStatic += OnConnect;
        GameServerClient.JoinTableResponseReceivedStatic += OnJoinConnect;
        GameServerClient.BuyInResponseReceivedStatic += OnBuyIn;
        OwnerPlayerID = ArtGameManager.Instance != null ? ArtGameManager.Instance.playerID : OwnerPlayerID;
        _joinRequested = false;
        _hasSnapshot = false;

        if (IsSessionReady())
        {
            TryJoinTable("already-connected");
        }
        else
        {
            StartCoroutine(WaitForSessionThenJoin());
        }
    }

    void OnDisable()
    {
        GameServerClient.MessageReceivedStatic -= OnMessage;
        GameServerClient.ConnectResponseReceivedStatic -= OnConnect;
        GameServerClient.JoinTableResponseReceivedStatic -= OnJoinConnect;
        GameServerClient.BuyInResponseReceivedStatic -= OnBuyIn;
    }

    void OnConnect(ConnectResponse resp)
    {
        Debug.Log("On Connect inside");
        OwnerPlayerID = resp.PlayerId;
        TryJoinTable("connect");
    }

    void OnJoinConnect(JoinTableResponse resp)
    {
        if (resp == null)
            return;

        if (string.Equals(resp.Message, "already_in_match", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(resp.TableId))
            {
                GameServerClient.SendRejoinStatic(resp.TableId);
                Debug.Log($"[PokerNetConnect] Rejoin requested tableId={resp.TableId} (already_in_match)");
            }
            else
            {
                Debug.LogWarning("[PokerNetConnect] already_in_match but tableId is empty. Rejoin skipped.");
            }
            return;
        }

        if (!resp.Success)
            return;

        // Auto-buy-in so the server can start the hand.
        var minBuyIn = ArtGameManager.Instance != null ? ArtGameManager.Instance.selectedMinBuyIn : 0;
        if (minBuyIn <= 0)
        {
            Debug.LogWarning("[PokerNetConnect] MinBuyIn not set. Buy-in skipped.");
            return;
        }

        GameServerClient.SendBuyInStatic(resp.TableId, minBuyIn);
        Debug.Log($"[PokerNetConnect] BuyIn requested tableId={resp.TableId} amount={minBuyIn}");
    }

    void OnBuyIn(BuyInResponse resp)
    {
        if (resp == null)
            return;

        if (!resp.Success)
        {
            Debug.LogWarning($"[PokerNetConnect] BuyIn failed: {resp.Message}");
            return;
        }

        if (string.IsNullOrEmpty(resp.TableId))
            return;

        if (!_hasSnapshot)
            StartCoroutine(EnsureSnapshotAfterBuyIn(resp.TableId));
    }

    void TryJoinTable(string reason)
    {
        if (_joinRequested)
            return;

        var tableCode = GetTargetTableCode();
        if (string.IsNullOrWhiteSpace(tableCode))
        {
            Debug.LogWarning($"[PokerNetConnect] Table code not set. Join skipped ({reason}).");
            return;
        }

        var matchSizeId = ArtGameManager.Instance != null ? ArtGameManager.Instance.selectedMaxSizeID : 0;
        if (!_isPlayerEnable && matchSizeId <= 0)
            matchSizeId = _defaultBotsMatchSizeId;

        if (matchSizeId <= 0)
        {
            Debug.LogWarning("[PokerNetConnect] MatchSizeId is invalid. Join skipped.");
            return;
        }

        if (ArtGameManager.Instance != null)
            Debug.Log(ArtGameManager.Instance.selectedMaxSizeID + " " + tableCode);
        GameServerClient.SendJoinTableStatic(tableCode, matchSizeId);
        _joinRequested = true;
        Debug.Log($"[PokerNetConnect] Join requested ({reason}) table={tableCode} matchSizeId={matchSizeId}");
    }

    string GetTargetTableCode()
    {
        if (_isPlayerEnable)
        {
            if (ArtGameManager.Instance != null && !string.IsNullOrWhiteSpace(ArtGameManager.Instance.selectedTableCode))
                return ArtGameManager.Instance.selectedTableCode;
            return _playerTableCode;
        }

        return _botsTableCode;
    }

    bool IsSessionReady()
    {
        var client = GameServerClient.Instance;
        return client != null && client.IsConnected && !string.IsNullOrEmpty(client.SessionId);
    }

    IEnumerator WaitForSessionThenJoin()
    {
        const float timeoutSeconds = 10f;
        var start = Time.unscaledTime;
        while (!_joinRequested && Time.unscaledTime - start < timeoutSeconds)
        {
            if (IsSessionReady())
            {
                TryJoinTable("late-connect");
                yield break;
            }
            yield return null;
        }

        if (!_joinRequested)
            Debug.LogWarning("[PokerNetConnect] Join skipped (no session after timeout).");
    }


    void OnMessage(MsgType type, IMessage msg)
    {
        OnMessageEvent?.Invoke(type, msg);

        if (type == MsgType.TableSnapshot)
        {
            var m = (TableSnapshot)msg;
            _onPrintRoundStatusEvent?.Invoke($"Round Status: {m.State}");
            _hasSnapshot = true;
        }
    }

    IEnumerator EnsureSnapshotAfterBuyIn(string tableId)
    {
        const float waitSeconds = 2f;
        var start = Time.unscaledTime;
        while (!_hasSnapshot && Time.unscaledTime - start < waitSeconds)
            yield return null;

        if (!_hasSnapshot)
        {
            GameServerClient.SendRejoinStatic(tableId);
            Debug.Log($"[PokerNetConnect] Rejoin requested tableId={tableId} (no snapshot after buy-in)");
        }
    }

    public static string FormatCards(Google.Protobuf.Collections.RepeatedField<Card> cards)
    {
        // Converts list of Card -> readable string like "A_H 10_S 2_C"
        if (cards == null || cards.Count == 0) return "-";
        var parts = new string[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            parts[i] = FormatCard(cards[i]);
        return string.Join(" ", parts);
    }

    static string FormatCard(Card c)
    {
        // Converts one Card to readable name using our sprite naming
        // Example: Rank=14 Suit=2 -> "A_H"
        string r = c.Rank switch
        {
            14 => "A",
            13 => "K",
            12 => "Q",
            11 => "J",
            10 => "10",
            _ => c.Rank.ToString()
        };
        string s = c.Suit switch { 0 => "C", 1 => "D", 2 => "H", 3 => "S", _ => "?" };
        return r + "_" + s;
    }
}
