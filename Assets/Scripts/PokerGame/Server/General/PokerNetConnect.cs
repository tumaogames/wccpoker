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
using WCC.Core;

public class PokerNetConnect : MonoBehaviour
{
    [SerializeField] UnityEvent<string> _onPrintRoundStatusEvent;
    [Header("Join Settings (no connection here)")]
    [SerializeField] bool _autoJoinOnEnable = true;
    [SerializeField] bool _isPlayerEnable = true;
    [SerializeField] string _playerTableCode = "";
    [SerializeField] string _botsTableCode = "DEV-BOT-TABLE";
    [SerializeField] int _defaultBotsMatchSizeId = 3;

    public static string OwnerPlayerID;
    public static event Action<MsgType, IMessage> OnMessageEvent;
    bool _joinRequested;
    bool _hasSnapshot;
    string _lastKnownTableId;
    bool _deferJoinUntilRoundEnd;
    string _pendingJoinTableCode;
    int _pendingJoinMatchSizeId;
    bool _awaitingRoundReset;

    void Awake() => Application.runInBackground = true;

    void Start()
    {
        if (!HasConnection())
            return;

        var isVault = IsVautActive();
        if (!isVault && !_netInfo.AutoConnectOnStart)
            return;

        ConfigureGame(
            isVault ? PokerSharedVault.LaunchToken : _netInfo.LaunchToken,
            isVault ? PokerSharedVault.ServerURL : _netInfo.ServerUrl,
            isVault ? PokerSharedVault.OperatorPublicID : _netInfo.OperatorPublicId
        );
    }

    #region LISTENERS
    void OnEnable()
    {
        GameServerClient.MessageReceivedStatic += OnMessage;
        GameServerClient.ConnectResponseReceivedStatic += OnConnect;
        GameServerClient.JoinTableResponseReceivedStatic += OnJoinConnect;
        GameServerClient.RejoinResponseReceivedStatic += OnRejoin;
        GameServerClient.BuyInResponseReceivedStatic += OnBuyIn;
        OwnerPlayerID = ArtGameManager.Instance != null ? ArtGameManager.Instance.playerID : OwnerPlayerID;
        _joinRequested = false;
        _hasSnapshot = false;
        _deferJoinUntilRoundEnd = false;
        _pendingJoinTableCode = "";
        _pendingJoinMatchSizeId = 0;
        _awaitingRoundReset = false;
        var existingTableId = GameServerClient.Instance != null ? GameServerClient.Instance.TableId : "";
        if (!string.IsNullOrWhiteSpace(existingTableId))
            _lastKnownTableId = existingTableId;

        if (!_autoJoinOnEnable)
        {
            Debug.Log("[PokerNetConnect] Auto-join disabled on enable.");
            return;
        }

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
        GameServerClient.RejoinResponseReceivedStatic -= OnRejoin;
        GameServerClient.BuyInResponseReceivedStatic -= OnBuyIn;
    }
    #endregion LISTENERS

    void ConfigureGame(string launchToken, string serverURL, string operatorPublicID)
    {
        GameServerClient.Configure(serverURL);
        GameServerClient.ConnectWithLaunchToken(launchToken, operatorPublicID);
    }

    void OnConnect(ConnectResponse resp)
    {
        Debug.Log("On Connect inside");
        OwnerPlayerID = resp.PlayerId;
        if (!_autoJoinOnEnable)
        {
            Debug.Log("[PokerNetConnect] Auto-join disabled on connect.");
            return;
        }
        TryJoinTable("connect");
    }

    void OnJoinConnect(JoinTableResponse resp)
    {
        if (resp == null)
            return;

        if (string.Equals(resp.Message, "already_in_match", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(resp.TableId))
                _lastKnownTableId = resp.TableId;

            _pendingJoinTableCode = GetTargetTableCode();
            _pendingJoinMatchSizeId = ResolveMatchSizeId();
            _deferJoinUntilRoundEnd = true;
            _awaitingRoundReset = false;
            _joinRequested = false;

            if (!string.IsNullOrWhiteSpace(_pendingJoinTableCode))
            {
                NetworkDebugLogger.LogSend("Spectate", $"tableCode={_pendingJoinTableCode} (already_in_match)");
                GameServerClient.SendSpectateStatic(_pendingJoinTableCode);
                Debug.Log($"[PokerNetConnect] already_in_match: spectating table={_pendingJoinTableCode} until round end.");
            }
            else
            {
                Debug.LogWarning("[PokerNetConnect] already_in_match but table code is empty. Spectate skipped.");
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
        NetworkDebugLogger.LogSend("BuyIn", $"tableId={resp.TableId} amount={minBuyIn}");
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

    void OnRejoin(RejoinResponse resp)
    {
        if (resp == null)
            return;

        if (!resp.Success)
        {
            Debug.LogWarning($"[PokerNetConnect] Rejoin failed: {resp.Message}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(resp.TableId))
            _lastKnownTableId = resp.TableId;

        if (_isPlayerEnable && resp.Stack <= 0)
        {
            var minBuyIn = ArtGameManager.Instance != null ? ArtGameManager.Instance.selectedMinBuyIn : 0;
            if (minBuyIn > 0 && !string.IsNullOrEmpty(resp.TableId))
            {
                GameServerClient.SendBuyInStatic(resp.TableId, minBuyIn);
                NetworkDebugLogger.LogSend("BuyIn", $"tableId={resp.TableId} amount={minBuyIn} (rejoin)");
                Debug.Log($"[PokerNetConnect] BuyIn requested tableId={resp.TableId} amount={minBuyIn} (rejoin)");
            }
        }

        if (!string.IsNullOrEmpty(resp.TableId) && !_hasSnapshot)
            StartCoroutine(EnsureSnapshotAfterBuyIn(resp.TableId));
    }

    void TryJoinTable(string reason)
    {
        if (_joinRequested)
            return;
        if (_deferJoinUntilRoundEnd)
        {
            Debug.Log($"[PokerNetConnect] Join deferred until round end ({reason}).");
            return;
        }

        var tableCode = GetTargetTableCode();
        if (string.IsNullOrWhiteSpace(tableCode))
        {
            Debug.LogWarning($"[PokerNetConnect] Table code not set. Join skipped ({reason}).");
            return;
        }

        var matchSizeId = ResolveMatchSizeId();

        if (matchSizeId <= 0)
        {
            Debug.LogWarning("[PokerNetConnect] MatchSizeId is invalid. Join skipped.");
            return;
        }

        Debug.Log(matchSizeId + " " + tableCode);
        GameServerClient.SendJoinTableStatic(tableCode, matchSizeId);
        NetworkDebugLogger.LogSend("JoinTable", $"tableCode={tableCode} matchSizeId={matchSizeId} reason={reason}");
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

        ConnectToTable();
    }

    void ConnectToTable()
    {
        if (IsVautActive())
        {
            var sampleTestMatchSizeId = 3;
            var localTableCode = _netInfo.IsPlayerEnable ? _netInfo.PlayerTableCode : _netInfo.BotsTableCode;
            var vaultTBCode = PokerSharedVault.TableCode ?? localTableCode;
            var matchSizeID = PokerSharedVault.MatchSizeId >= 0 ? PokerSharedVault.MatchSizeId : sampleTestMatchSizeId;
            GameServerClient.SendJoinTableStatic(vaultTBCode, matchSizeID);
            return;
        }

        if (!_netInfo.AutoSpectateOnConnect)
            return;

        var tableCode = _netInfo.IsPlayerEnable ? _netInfo.PlayerTableCode : _netInfo.BotsTableCode;
        if (string.IsNullOrWhiteSpace(tableCode))
        {
            Debug.LogWarning("[BotDump] table code is empty. Auto-join skipped.");
            return;
        }

        GameServerClient.SendJoinTableStatic(tableCode, 3);
    }

    bool IsVautActive()
        => PokerSharedVault.TableCode != "?" &&
           PokerSharedVault.LaunchToken != "?" &&
           PokerSharedVault.ServerURL != "?" &&
           PokerSharedVault.OperatorPublicID != "?";

    void OnMessage(MsgType type, IMessage msg)
    {
        OnMessageEvent?.Invoke(type, msg);

        if (type == MsgType.TableSnapshot)
        {
            var m = (TableSnapshot)msg;
            _onPrintRoundStatusEvent?.Invoke($"Round Status: {m.State}");
            _hasSnapshot = true;
            if (!string.IsNullOrWhiteSpace(m.TableId))
                _lastKnownTableId = m.TableId;

            if (_deferJoinUntilRoundEnd && _awaitingRoundReset &&
                (m.State == TableState.Waiting || m.State == TableState.Reset))
            {
                Debug.Log("[PokerNetConnect] Table reset detected. Attempting deferred join.");
                JoinAfterRound("table-reset");
                _awaitingRoundReset = false;
            }
        }
        else if (type == MsgType.HandResult)
        {
            if (_deferJoinUntilRoundEnd)
            {
                Debug.Log("[PokerNetConnect] HandResult received. Waiting for table reset to join.");
                _awaitingRoundReset = true;
            }
        }
    }

    int ResolveMatchSizeId()
    {
        var matchSizeId = GlobalSharedData.MySelectedMatchSizeID;
        if (matchSizeId <= 0 && ArtGameManager.Instance != null)
            matchSizeId = ArtGameManager.Instance.selectedMaxSizeID;
        if (!_isPlayerEnable && matchSizeId <= 0)
            matchSizeId = _defaultBotsMatchSizeId;
        return matchSizeId;
    }

    void JoinAfterRound(string reason)
    {
        if (!_deferJoinUntilRoundEnd)
            return;

        var tableCode = _pendingJoinTableCode;
        if (string.IsNullOrWhiteSpace(tableCode))
            tableCode = GetTargetTableCode();

        var matchSizeId = _pendingJoinMatchSizeId > 0 ? _pendingJoinMatchSizeId : ResolveMatchSizeId();
        if (string.IsNullOrWhiteSpace(tableCode) || matchSizeId <= 0)
        {
            Debug.LogWarning("[PokerNetConnect] Deferred join failed (missing table code or matchSizeId).");
            return;
        }

        _deferJoinUntilRoundEnd = false;
        GameServerClient.SendJoinTableStatic(tableCode, matchSizeId);
        NetworkDebugLogger.LogSend("JoinTable", $"tableCode={tableCode} matchSizeId={matchSizeId} reason={reason}");
        _joinRequested = true;
        Debug.Log($"[PokerNetConnect] Join requested ({reason}) table={tableCode} matchSizeId={matchSizeId}");
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
            NetworkDebugLogger.LogSend("Rejoin", $"tableId={tableId} (no snapshot after buy-in)");
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

    bool HasConnection()
    {
        if (IsVautActive())
        {
            if (string.IsNullOrWhiteSpace(PokerSharedVault.ServerURL) ||
                string.IsNullOrWhiteSpace(PokerSharedVault.LaunchToken) ||
                string.IsNullOrWhiteSpace(PokerSharedVault.OperatorPublicID))
            {
                Debug.LogError("[BotDump] Missing serverUrl/launchToken/operatorPublicId. Auto-connect skipped.");
                return false;
            }
            return true;
        }

        if (!_netInfo.AutoConnectOnStart)
            return false;

        if (string.IsNullOrWhiteSpace(_netInfo.ServerUrl) ||
            string.IsNullOrWhiteSpace(_netInfo.LaunchToken) ||
            string.IsNullOrWhiteSpace(_netInfo.OperatorPublicId))
        {
            Debug.LogError("[BotDump] Missing serverUrl/launchToken/operatorPublicId. Auto-connect skipped.");
            return false;
        }

        return true;
    }
}
