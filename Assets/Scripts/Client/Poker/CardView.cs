////////////////////
//       RECK       //
////////////////////


using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WCC.Poker.Client
{
    public class CardView : MonoBehaviour
    {
        public TMP_Text label;
        public Image suitIcon;

        public void Set(Card card)
        {
            label.text = card.rank.ToString();
            // optional: change sprite by suit
        }
    }
}
