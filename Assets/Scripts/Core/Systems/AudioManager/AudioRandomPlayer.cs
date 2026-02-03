//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;

namespace WCC.Core.Audio
{
    public class AudioRandomPlayer : MonoBehaviour
    {
        [Header("[AUDIO]")]
        [SerializeField] bool _isParent;
        [SerializeField] string _audioKey;

        [Header("[AUDIO SETTINGS]")]
        [SerializeField] AudioManager.AudioSettings _audioSettings;


        AudioSource _audioSource;

        public void PlayRandomAudio()
        {
            (var g, var audios) = AudioManager.main.PlayRandomAudio(_audioKey, transform.localPosition, _audioSettings);
            if (audios == null) return;

            _audioSource = audios;

            if (_isParent && _audioSource.transform.parent != transform)
            {
                _audioSource.transform.SetParent(transform);
                _audioSource.transform.localPosition = new Vector3(0f, 0f, -10f);
            }
        }

        public void StopAudio()
        {
            if (_audioSource != null && _audioSource.isPlaying) _audioSource.Stop();
        }
    }
}
