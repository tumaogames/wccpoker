//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    
    [SerializeField] string _clipKey;
    [SerializeField] AudioManager.AudioSettings _settings;

    #region 
    AudioManager _audioManager;
    private void Start() => _audioManager = AudioManager.main;
    #endregion

    public void PlayAudio(int i) { if(_audioManager != null) _audioManager.PlayAudio(_clipKey, i, transform.position, _settings); }
}