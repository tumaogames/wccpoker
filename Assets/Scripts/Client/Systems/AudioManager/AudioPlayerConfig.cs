//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;
using UnityEngine.Events;

namespace WCC.Poker.Client.Audio
{
    public class AudioPlayerConfig : MonoBehaviour
    {
        [Header("[AUDIO TYPE]")]
        [SerializeField] string _clipKey;
        [SerializeField] int _clipElement;

        [Header("[AUDIO SETTINGS]")]
        [SerializeField] bool _isParent;
        [SerializeField] bool _playOnAwake = false;
        [SerializeField] bool _stopBGWhenPlaying = false;
        [SerializeField] AudioManager.AudioSettings _audioSettings = new();

        AudioManager _audioManager;
        GameObject _audioHolder;
        AudioSource _audioSource;

        bool _isDoneParenting = false;

        [Header("[EVENTS]")]
        [SerializeField] UnityEvent _onPlayAwakedEvent;
        [SerializeField] UnityEvent<GameObject> _onChangeOwnerEvent;

        #region 
        private void Start()
        {
            _audioManager = AudioManager.main;

            if (_playOnAwake)
            {
                _onPlayAwakedEvent?.Invoke();
                PlayingAudio();
            }
        }
        private void OnValidate()
        {
            if (_audioSettings.MinDistance > _audioSettings.MaxDistance) _audioSettings.MaxDistance = _audioSettings.MinDistance;
        }

        private void OnEnable()
        {
            if (_audioHolder == null) return;
            OnSetActiveAudio();
        }
        private void OnDisable()
        {
            if (_audioHolder == null) return;
            OnSetActiveAudio();

            if (_stopBGWhenPlaying) _audioManager.SetEnableBackgroundMusic(true);
        }
        void OnSetActiveAudio() => _audioHolder.SetActive(gameObject.activeInHierarchy);

        #endregion

        void PlayingAudio()
        {
            Audio();
            Music();
        }

        void Audio()
        {
            if (_audioManager == null) return;

            if (_audioSource == null)
            {
                (var audioHolder, var audioSource) = _audioManager.PlayAudio(_clipKey, _clipElement, transform.position, _audioSettings);
                if (audioHolder != null && audioSource != null)
                {
                    _audioHolder = audioHolder;
                    _audioSource = audioSource;
                }
            }
            else if (_audioSource != null && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }

            if (_audioSource != null && _isParent && !_isDoneParenting)
            {
                _audioSource.transform.SetParent(transform);
                _audioSource.transform.localPosition = new Vector3(0f, 0f, -10f);
                _isDoneParenting = true;
            }

        }

        void Music()
        {
            if (_stopBGWhenPlaying) _audioManager.SetEnableBackgroundMusic(false);
        }

        public void PlayAudio() => PlayingAudio();

        public void StopAudio()
        {
            if (_audioSource == null) return;
            if (!_audioSource.isPlaying) return;
            _audioSource.Stop();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _audioSettings.MinDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _audioSettings.MaxDistance);
        }
        public void GetOwnerToEvent() => _onChangeOwnerEvent?.Invoke(_audioHolder);
        public void SetOwnerFromEvent(GameObject audioOwner)
        {
            _audioHolder = audioOwner;
            OnSetActiveAudio();
        }
    }
}