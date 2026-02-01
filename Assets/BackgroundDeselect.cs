using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundDeselect :
    MonoBehaviour,
    IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        LayoutHoverResizeDOTween.DeselectCurrent();
    }
}

