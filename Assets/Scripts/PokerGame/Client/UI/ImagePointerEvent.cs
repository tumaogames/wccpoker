 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace WCC.Poker.Client
{
    public class ImagePointerEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] UnityEvent<bool> _onPointerEnterExitEvent;
        [SerializeField] UnityEvent _onPointerEnterEvent;
        [SerializeField] UnityEvent _onPointerExitEvent;
      
        /// <summary>
        /// This function ay para sa pag enter ng pointer
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _onPointerEnterExitEvent?.Invoke(true);
            _onPointerEnterEvent?.Invoke();
        }

        /// <summary>
        /// Itong function na ito ay para sa pag exit ng pointer
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            _onPointerEnterExitEvent?.Invoke(false);
            _onPointerExitEvent?.Invoke();
        }
    }
}
