 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.UI;
using static WCC.Poker.Shared.GlobalHawk;


namespace WCC.Poker.Client
{
    public class HumanBettingUI : MonoBehaviour
    {
        public InputField betInput;

        PokerGameManager game;

        void Start()
        {
            game = FindObjectOfType<PokerGameManager>();
        }

        public void OnCall()
        {
            game.HumanAction(PlayerAction.Call, 0);
        }

        public void OnRaise()
        {
            int amount = int.Parse(betInput.text);
            game.HumanAction(PlayerAction.Raise, amount);
        }

        public void OnFold()
        {
            game.HumanAction(PlayerAction.Fold, 0);
        }
    }
}
