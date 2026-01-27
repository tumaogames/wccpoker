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
        [SerializeField] CardAnimation _flipAnimation;
        [SerializeField] CardData.CardsInfo _cardInfo;
        [SerializeField] GameObject _cardOutline;
        [SerializeField] GameObject _cardSleepImage;

        Sprite _closeCardSprite;
        Quaternion _parentLocalRotation;
        bool _isOpenCard = false;

        public void InitializeCarView(CardData.CardsInfo cardInfo, Transform parent)
        {
            _closeCardSprite = _cardImage.sprite;
            _cardInfo = cardInfo;
            _parentLocalRotation = parent.localRotation;
        }
       
        public void SetOpenCard()
        {
            if (_isOpenCard) return;
            _flipAnimation.SetFlipAnimation(() =>
            {
                _cardImage.sprite = _cardInfo.CardSprite;
                transform.parent.localRotation = Quaternion.Euler(0, 0, 0);
            });
            _isOpenCard = true;
        }

        public void SetCloseCard()
        {
            if (!_isOpenCard) return;
            _flipAnimation.SetFlipAnimation(() =>
            {
                _cardImage.sprite = _closeCardSprite;
                transform.parent.localRotation = _parentLocalRotation;
            });
            _isOpenCard = false;
        }

        public void SetFlipCardAnimation(UnityAction callback) => _flipAnimation.SetFlipAnimation(callback);

        public void SetShowOutline(bool show)
        {
            _cardOutline.SetActive(show);
            _flipAnimation.SetBloomCard();
        }

        public void SetSleepCard(bool e) => _cardSleepImage.SetActive(e);
    }
}
