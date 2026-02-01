////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace WCC.Poker.Client
{
    public class UserActionsAnimations : BaseAnimation
    {
        [Header("References")]
        [SerializeField] private RectTransform targetRect;

        [Header("Values")]
        [SerializeField] private float upValue;
        [SerializeField] private float duration = 0.3f;

        [Header("EVENTS")]
        [SerializeField] UnityEvent<bool> _onShowEvent;
        [SerializeField] UnityEvent _onEnableEvent;
        [SerializeField] UnityEvent _onDisableEvent;

        bool _isShown = false;
        float _orgPosY;

        private void Start()
        {
            _orgPosY = targetRect.localPosition.y;
            RegisterAnimation(nameof(PlayActionButtonGoUp), PlayActionButtonGoUp);
            RegisterAnimation(nameof(PlayActionButtonGoDown), PlayActionButtonGoDown);
        }

        /// <summary>
        /// This function ay para mag move to UP yung rect na [User Actions]
        /// </summary>
        [Button]
        public void PlayActionButtonGoUp()
        {
            if(_isShown) return;

            targetRect.DOLocalMove(new Vector3(targetRect.localPosition.x, targetRect.localPosition.y + upValue, targetRect.localPosition.z), duration)
                      .SetEase(Ease.OutCubic);
            _isShown = true;

            _onShowEvent?.Invoke(true);
            _onEnableEvent?.Invoke();
        }

        /// <summary>
        /// This function ay para mag move to DOWN yung rect na [User Actions]
        /// </summary>
        [Button]
        public void PlayActionButtonGoDown()
        {
            if (!_isShown) return;

            targetRect.DOLocalMove(new Vector3(targetRect.localPosition.x, _orgPosY, targetRect.localPosition.z), duration)
                      .SetEase(Ease.OutCubic);
            _isShown = false;

            _onShowEvent?.Invoke(false);
            _onDisableEvent?.Invoke();
        }

    }
}
