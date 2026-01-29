////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using UnityEngine;


namespace WCC.Core
{
    public class Bouncy_mod : MonoBehaviour
    {
        [SerializeField] float _bounceHeight = 20f;
        [SerializeField] float _duration = 0.5f;

        Tween _bounceTween;

        private void OnEnable()
        {
            _bounceTween = transform
            .DOMoveY(transform.position.y + _bounceHeight, _duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        }

        void OnDisable() => _bounceTween?.Kill();
    }
}
