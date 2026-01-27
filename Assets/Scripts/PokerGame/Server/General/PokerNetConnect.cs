////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using UnityEngine;

public class PokerNetConnect : MonoBehaviour
{
    [SerializeField] PokerNetData _netInfo;
    [SerializeField] string _debug_tableID;
    public static string OwnerPlayerID;

    public static event Action<MsgType, IMessage> OnMessageEvent;

    void Start()
    {

        if (!_netInfo.AutoConnectOnStart)
            return;

        if (string.IsNullOrWhiteSpace(_netInfo.ServerUrl) ||
            string.IsNullOrWhiteSpace(_netInfo.LaunchToken) ||
            string.IsNullOrWhiteSpace(_netInfo.OperatorPublicId))
        {
            Debug.LogWarning("[BotDump] Missing serverUrl/launchToken/operatorPublicId. Auto-connect skipped.");
            return;
        }

        GameServerClient.Configure(_netInfo.ServerUrl);
        GameServerClient.ConnectWithLaunchToken(_netInfo.LaunchToken, _netInfo.OperatorPublicId);
    }


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

    void OnConnect(ConnectResponse resp)
    {
        OwnerPlayerID = resp.PlayerId;

        if (!_netInfo.AutoSpectateOnConnect)
            return;

        if (string.IsNullOrWhiteSpace(_netInfo.SpectateTableCode))
        {
            Debug.LogWarning("[BotDump] spectateTableCode is empty.");
            return;
        }

        //GameServerClient.SendSpectateStatic(_netInfo.SpectateTableCode);
        GameServerClient.SendJoinTableStatic(_debug_tableID);
    }


    void OnMessage(MsgType type, IMessage msg) => OnMessageEvent?.Invoke(type, msg);

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