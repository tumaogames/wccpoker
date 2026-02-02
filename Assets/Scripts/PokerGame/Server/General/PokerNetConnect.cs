////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using UnityEngine;
using UnityEngine.Events;
using WCC.Core;

public class PokerNetConnect : MonoBehaviour
{
    [SerializeField] PokerNetData _netInfo;
    [SerializeField] UnityEvent<string> _onPrintRoundStatusEvent;

    public static string OwnerPlayerID;
    public static event Action<MsgType, IMessage> OnMessageEvent;

    void Awake() => Application.runInBackground = true;

    void Start()
    {
        if(!HasConnection()) return;
       
        var isVault = IsVautActive();

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
    }

    void OnDisable()
    {
        GameServerClient.MessageReceivedStatic -= OnMessage;
        GameServerClient.ConnectResponseReceivedStatic -= OnConnect;
    }
    #endregion LISTENERS

    void ConfigureGame(string launchToken, string serverURL, string operatorPublicID)
    {
        GameServerClient.Configure(serverURL);
        GameServerClient.ConnectWithLaunchToken(launchToken, operatorPublicID);
    }

    void OnConnect(ConnectResponse resp)
    {
        OwnerPlayerID = resp.PlayerId;

        if (!_netInfo.AutoSpectateOnConnect && !IsVautActive())
            return;

        ConnectToTable();
    }

    void ConnectToTable()
    {
        if (IsVautActive())
        {
            var sampleTestMatchSizeId = 3;
            var localTableCode = _netInfo.IsPlayerEnable ? _netInfo.PlayerTableCode : _netInfo.BotsTableCode;
            var vaultTBCode = PokerSharedVault.TableCode ?? localTableCode;
            var matchSizeID = PokerSharedVault.MatchSizeId == -1 ? PokerSharedVault.MatchSizeId : sampleTestMatchSizeId;
            GameServerClient.SendJoinTableStatic(vaultTBCode, matchSizeID);
        }
    }

    bool IsVautActive() => PokerSharedVault.TableCode != string.Empty && PokerSharedVault.LaunchToken != string.Empty && PokerSharedVault.MatchSizeId != -1;

    void OnMessage(MsgType type, IMessage msg)
    {
        OnMessageEvent?.Invoke(type, msg);

        if (type == MsgType.TableSnapshot)
        {
            var m = (TableSnapshot)msg;
            _onPrintRoundStatusEvent?.Invoke($"Round Status: {m.State}");
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
        if (!_netInfo.AutoConnectOnStart && !IsVautActive())
            return false;

        if (string.IsNullOrWhiteSpace(_netInfo.ServerUrl) ||
            string.IsNullOrWhiteSpace(_netInfo.LaunchToken) ||
            string.IsNullOrWhiteSpace(_netInfo.OperatorPublicId))
        {
            Debug.LogError("[BotDump] Missing serverUrl/launchToken/operatorPublicId. Auto-connect skipped.");
            return false;
        }

        if (IsVautActive())
        {
            if (string.IsNullOrWhiteSpace(PokerSharedVault.ServerURL) ||
                string.IsNullOrWhiteSpace(PokerSharedVault.LaunchToken) ||
                string.IsNullOrWhiteSpace(PokerSharedVault.OperatorPublicID))
            {
                Debug.LogError("[BotDump] Missing serverUrl/launchToken/operatorPublicId. Auto-connect skipped.");
                return false;
            }
        }

        return true;
    }
}
