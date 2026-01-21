 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Client
{
    public class PokerUIController : MonoBehaviour
    {
        public UIHandRenderer[] hands;

        public void Refresh()
        {
            for (int i = 0; i < hands.Length; i++)
                hands[i].Render(PokerGameManager.main.players[i].hand);
        }
    }
}
