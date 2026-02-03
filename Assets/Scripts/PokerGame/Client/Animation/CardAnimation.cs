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

        Vector3 _cardRootDefaultLocalPos;
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
            _cardRootDefaultLocalPos = _cardRoot.localPosition;
            _cardRoot.DOScale(new Vector3(1.085f, 1.085f, 1.085f), 0.5f);
            _cardRoot.DOLocalMoveY(_cardRootDefaultLocalPos.y + 2.5f, 0.5f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _cardRoot.DOLocalMove(_cardRootDefaultLocalPos, 0.3f).SetDelay(3f).SetEase(Ease.InOutSine);
                _cardRoot.DOScale(Vector3.one, 0.5f).SetDelay(3f);
            });
           
        }

        void ResetRootTransform()
        {
            _cardRoot.localPosition = Vector3.zero;
            _cardRoot.localScale = Vector3.one;
        }
    }
}
