////////////////////
//       RECK       //
////////////////////


using Unity.Netcode;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class NetworkLauncher : MonoBehaviour
    {

        public void Host() => NetworkManager.Singleton.StartHost();
        public void Client() => NetworkManager.Singleton.StartClient();

    }
}
