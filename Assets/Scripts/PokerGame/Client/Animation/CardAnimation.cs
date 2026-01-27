////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;


namespace WCC.Poker.Client
{
    public class CardAnimation : MonoBehaviour
    {
        [SerializeField] Transform _cardRoot;

        float _cardRootDefaultY;
        bool _isFlip = false;

        public void SetFlipAnimation(UnityAction callback)
        {
            ResetRootTransform();
            transform.DOLocalRotate(new Vector3(0, !_isFlip ? 90 : 0, 0), 0.1f).SetEase(Ease.InOutSine).OnComplete(()=>
            {
                transform.localRotation = Quaternion.Euler(0, 270f, 0);
                transform.DOLocalRotate(new Vector3(0, !_isFlip ? 360 : 0, 0), 0.1f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                    _isFlip = !_isFlip;
                    callback();
                });
            });
        }
        //

        public void SetBloomCard()
        {
            _cardRootDefaultY = _cardRoot.position.y;
            _cardRoot.DOScale(new Vector3(1.085f, 1.085f, 1.085f), 0.5f);
            _cardRoot.DOMoveY(_cardRoot.position.y + 2.5f, 0.5f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _cardRoot.DOMoveY(_cardRootDefaultY, 0.3f).SetEase(Ease.InOutSine);
                _cardRoot.DOScale(Vector3.one, 0.5f);
            });
           
        }

        void ResetRootTransform()
        {
            _cardRoot.localPosition = Vector3.zero;
            _cardRoot.localScale = Vector3.one;
        }
    }
}
