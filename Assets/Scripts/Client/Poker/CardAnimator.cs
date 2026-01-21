////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class CardAnimator : MonoBehaviour
    {
        public void Deal(Vector3 target)
        {
            transform.DOMove(target, 0.4f).SetEase(Ease.OutBack);
            transform.DORotate(Vector3.zero, 0.4f);
        }

        public void Flip()
        {
            transform.DORotate(new Vector3(0, 180, 0), 0.3f);
        }
    }
}
