//********************//            
// DEVELOPER: ʌǝpʞɔǝᴚ //
//********************//

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WCC.Core.Audio
{
    [RequireComponent(typeof(Image))]
    public class UIButtonAudioPlayer : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] string _audioKey;
        [SerializeField] int _audioIndex;

        /// <summary>
        /// This function ay para sa user input
        /// This function ay galing sa IPointerDownHandler EventSystems
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData) => AudioManager.main.PlayAudio(_audioKey, _audioIndex, Vector3.zero);
    }
}