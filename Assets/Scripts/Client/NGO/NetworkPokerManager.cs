 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using Unity.Netcode;
using WCC.Poker.Shared.Exposed;


namespace WCC.Poker.Client
{
    public class NetworkPokerManager : Exposing<NetworkPokerManager>
    {

        [ServerRpc]
        public void DealServerRpc()
        {
            PokerGameManager.main.StartRound();
        }

        [ClientRpc]
        public void UpdateCardsClientRpc(int playerId, Card card)
        {
            // sync card visuals
        }

        [ClientRpc]
        public void SyncStateClientRpc()
        {
            //FindObjectOfType<PokerUIController>().Refresh();
        }
    }

}
