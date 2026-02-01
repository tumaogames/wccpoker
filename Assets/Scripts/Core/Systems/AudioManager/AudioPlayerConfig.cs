//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;
using UnityEngine.Events;

namespace WCC.Core.Audio
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

        #region MONO
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, _audioSettings.MinDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _audioSettings.MaxDistance);
        }
        #endregion MONO

        /// <summary>
        /// Enable and Disable the gameobect from the current audio
        /// </summary>
        void OnSetActiveAudio() => _audioHolder.SetActive(gameObject.activeInHierarchy);

        /// <summary>
        /// This function executes the group of functions to play the audio
        /// </summary>
        void PlayingAudio()
        {
            Audio();
            Music();
        }

        /// <summary>
        /// This function handles to play the audio and setting the audio position
        /// </summary>
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

        /// <summary>
        /// This function checks if the background music flag is TRUE then it will stop the playing background music
        /// </summary>
        void Music()
        {
            if (_stopBGWhenPlaying) _audioManager.SetEnableBackgroundMusic(false);
        }

        //--------------------------------------------------------------------------

        /// <summary>
        /// This function Play's the audio
        /// </summary>
        public void PlayAudio() => PlayingAudio();

        /// <summary>
        /// This function stops the audio
        /// </summary>
        public void StopAudio()
        {
            if (_audioSource == null) return;
            if (!_audioSource.isPlaying) return;
            _audioSource.Stop();
        }

        /// <summary>
        /// This function gets the gameobjects from the audio to the event
        /// Basically its like a updater to throw a Audio GameObject for the Event parameter
        /// </summary>
        public void GetOwnerToEvent() => _onChangeOwnerEvent?.Invoke(_audioHolder);

        /// <summary>
        /// This function sets the gameobject to the owner
        /// </summary>
        /// <param name="audioOwner"></param>
        public void SetOwnerFromEvent(GameObject audioOwner)
        {
            _audioHolder = audioOwner;
            OnSetActiveAudio();
        }
    }
}