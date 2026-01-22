////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WCC.Poker.Shared.GlobalHawk;


namespace WCC.Poker.Client
{
    public class HumanBettingUI : MonoBehaviour
    {
        public TMP_InputField betInput;

        public void OnCall()
        {
            PokerGameManager.main.HumanAction(PlayerAction.Call, 0);
        }

        public void OnRaise()
        {
            int amount = int.Parse(betInput.text);
            PokerGameManager.main.HumanAction(PlayerAction.Raise, amount);
        }

        public void OnFold()
        {
            PokerGameManager.main.HumanAction(PlayerAction.Fold, 0);
        }
    }
}
