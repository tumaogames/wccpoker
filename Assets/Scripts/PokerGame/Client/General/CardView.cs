 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] Image _cardImage;
        [SerializeField] CardFlipAnimation _flipAnimation;
        [SerializeField] CardData.CardsInfo _cardInfo;

        Sprite _closeCardSprite;
        Quaternion _parentLocalRotation;
        bool _isOpenCard = false;

        public void InitCarView(CardData.CardsInfo cardInfo, Transform parent)
        {
            _closeCardSprite = _cardImage.sprite;
            _cardInfo = cardInfo;
            _parentLocalRotation = parent.localRotation;
        }
       
        public void OpenCard()
        {
            if (_isOpenCard) return;
            _flipAnimation.SetFlipAnimation(() =>
            {
                _cardImage.sprite = _cardInfo.CardSprite;
                transform.parent.localRotation = Quaternion.Euler(0, 0, 0);
            });
            _isOpenCard = true;
        }

        public void CloseCard()
        {
            if (!_isOpenCard) return;
            _flipAnimation.SetFlipAnimation(() =>
            {
                _cardImage.sprite = _closeCardSprite;
                transform.parent.localRotation = _parentLocalRotation;
            });
            _isOpenCard = false;
        }

        public void FlipCardAnimation(UnityAction callback) => _flipAnimation.SetFlipAnimation(callback);
    }
}
