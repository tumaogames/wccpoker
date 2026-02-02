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
      
        public void OnPointerEnter(PointerEventData eventData)
        {
            _onPointerEnterExitEvent?.Invoke(true);
            _onPointerEnterEvent?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onPointerEnterExitEvent?.Invoke(false);
            _onPointerExitEvent?.Invoke();
        }
    }
}
