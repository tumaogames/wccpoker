//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;

public class AudioRandomPlayer : MonoBehaviour
{
    [SerializeField] bool _isParent;
    [SerializeField] string _audioKey;
    [SerializeField] AudioManager.AudioSettings _audioSettings;
    AudioSource _audioSource;

    bool _isDoneParenting = false;

    public void PlayRandomAudio()
    {
        if (_audioSource == null)
        {
            (var g, var audios) = AudioManager.main.PlayRandomAudio(_audioKey, transform.localPosition, _audioSettings);
            if (audios != null) _audioSource = audios;
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

    public void StopAudio()
    {
        if (_audioSource != null && _audioSource.isPlaying) _audioSource.Stop();
    }
}
