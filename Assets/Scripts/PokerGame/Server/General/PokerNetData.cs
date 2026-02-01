 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;

[CreateAssetMenu(fileName = "NetData", menuName = "WCC/NET/Data")]
public class PokerNetData : ScriptableObject
{
    public bool AutoConnectOnStart = true;
    public string ServerUrl = "ws://51.79.160.227:26001/ws";
    public string LaunchToken = sharedData .launcherToken ;
    public string OperatorPublicId = sharedData .operatorID ;

    public bool AutoSpectateOnConnect = true;
    public string SpectateTableCode = "DEV-BOT-TABLE";
}