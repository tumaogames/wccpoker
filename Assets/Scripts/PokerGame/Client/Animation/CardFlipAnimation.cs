////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;


namespace WCC.Poker.Client
{
    public class CardFlipAnimation : MonoBehaviour
    {
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

    }
}
