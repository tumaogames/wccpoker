 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;

[CreateAssetMenu(fileName = "NetData", menuName = "WCC/NET/Data")]
public class PokerNetData : ScriptableObject
{
    public bool AutoConnectOnStart = true;
    public string ServerUrl = "ws://51.79.160.227:26001/ws";
    public string LaunchToken = "";
    public string OperatorPublicId = "cde5ce9f-8df7-f011-ace1-b2b431609323";

    public bool AutoSpectateOnConnect = true;
    public string SpectateTableCode = "DEV-BOT-TABLE";
}