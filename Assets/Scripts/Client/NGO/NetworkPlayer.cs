////////////////////
//       RECK       //
////////////////////


using Unity.Netcode;
using UnityEngine;
using static WCC.Poker.Shared.GlobalHawk;


namespace WCC.Poker.Client
{
    public class NetworkPlayer : NetworkBehaviour
    {
        public int playerIndex;   // index in PokerGameManager.players

        // Called by UI buttons
        public void OnRaise(int amount)
        {
            if (!IsOwner) return;
            RaiseServerRpc(amount);
        }

        public void OnCall()
        {
            if (!IsOwner) return;
            CallServerRpc();
        }

        public void OnFold()
        {
            if (!IsOwner) return;
            FoldServerRpc();
        }

        // -------- SERVER --------

        [ServerRpc]
        void RaiseServerRpc(int amount)
        {
            PokerGameManager.main.ServerHandleAction(playerIndex, PlayerAction.Raise, amount);
        }

        [ServerRpc]
        void CallServerRpc()
        {
            PokerGameManager.main.ServerHandleAction(playerIndex, PlayerAction.Call, 0);
        }

        [ServerRpc]
        void FoldServerRpc()
        {
            PokerGameManager.main.ServerHandleAction(playerIndex, PlayerAction.Fold, 0);
        }
    }
}
