//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;

namespace WCC.Core.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("[AUDIO KEY]")]
        [SerializeField] string _clipKey;

        [Header("[AUDIO SETTINGS]")]
        [SerializeField] AudioManager.AudioSettings _settings;

        #region GET-MANAGER-INSTANCE
        AudioManager _audioManager;
        private void Start() => _audioManager = AudioManager.main;
        #endregion GET-MANAGER-INSTANCE

        /// <summary>
        /// This function ay para mag play ng audio by using the audio clip index
        /// Note: Make sure naka setup yung clip key sa inspector then tamang audio click index
        /// </summary>
        /// <param name="i"></param>
        public void PlayAudio(int i) { if (_audioManager != null) _audioManager.PlayAudio(_clipKey, i, transform.position, _settings); }
    }
}