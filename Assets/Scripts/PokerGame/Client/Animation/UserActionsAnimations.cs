////////////////////
//       RECK       //
////////////////////


using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace WCC.Poker.Client
{
    public class UserActionsAnimations : BaseAnimation
    {
        [Header("References")]
        [SerializeField] private Animator _actionAnimation;

        [Header("EVENTS")]
        [SerializeField] UnityEvent<bool> _onShowEvent;
        [SerializeField] UnityEvent _onEnableEvent;
        [SerializeField] UnityEvent _onDisableEvent;

        bool _isShown = false;
        readonly static int _upAnim = Animator.StringToHash("Up");
        readonly static int _downAnim = Animator.StringToHash("Down");

        private void Start()
        {
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

            _actionAnimation.SetTrigger(_upAnim);

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

            _actionAnimation.SetTrigger(_downAnim);

            _isShown = false;

            _onShowEvent?.Invoke(false);
            _onDisableEvent?.Invoke();
        }

    }
}
