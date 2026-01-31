 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;

[CreateAssetMenu(fileName = "NetData", menuName = "WCC/NET/Data")]
public class PokerNetData : ScriptableObject
{
    [Header("[TOKEN]")]
    [TextArea(3, 3)] public string LaunchToken = "";

    [Space(20)]

    [Header("[CONFIG]")]
    public bool AutoConnectOnStart = true;
    [TextArea(3, 3)] public string ServerUrl = "ws://51.79.160.227:26001/ws";
    [TextArea(3, 3)] public string OperatorPublicId = "cde5ce9f-8df7-f011-ace1-b2b431609323";

    [Header("[SPECTATOR]")]
    public bool AutoSpectateOnConnect = true;
    public bool IsPlayerEnable = true;
    public string PlayerTableCode = "WCC-TBL-POKER-001";
    public string BotsTableCode = "DEV-BOT-TABLE";
}