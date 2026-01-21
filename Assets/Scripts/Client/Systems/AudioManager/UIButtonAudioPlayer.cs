//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAudioPlayer : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] string _audioKey;
    [SerializeField] int _audioIndex;

    public void OnPointerDown(PointerEventData eventData) => AudioManager.main.PlayAudio(_audioKey, _audioIndex, Vector3.zero);
}