 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] Image _cardImage;

        Card _cardInfo;

        public void InitCarView(Card cardInfo, Sprite cardSprite)
        {
            _cardInfo = cardInfo;
            _cardImage.sprite = cardSprite;
        }

    }
}
