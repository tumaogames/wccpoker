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

        bool _isFlip = false;

        public void SetFlipAnimation(UnityAction callback)
        {
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
            _cardRoot.DOMoveY(_cardRoot.position.y + 1.3f, 1f).SetEase(Ease.InOutSine);
            _cardRoot.DOScale(new Vector3(1.08f, 1.08f, 1.08f), 0.5f);
        }
    }
}
