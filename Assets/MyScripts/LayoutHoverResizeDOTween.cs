using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(LayoutElement))]
public class LayoutHoverResizeDOTween :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Hover Size")]
    public float hoverPreferredWidth = 400f;
    public float hoverPreferredHeight = 400f;

    [Header("Animation")]
    public float duration = 0.25f;
    public Ease ease = Ease.OutBack;

    [Header("Child Toggle")]
    public int childIndexToToggle = 1;

    // 🔒 Global selection
    private static LayoutHoverResizeDOTween currentSelected;

    private LayoutElement layoutElement;
    private RectTransform rectTransform;

    private float originalWidth;
    private float originalHeight;

    private Tween sizeTween;
    private bool isSelected;

    void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = (RectTransform)transform;

        originalWidth = layoutElement.preferredWidth;
        originalHeight = layoutElement.preferredHeight;

        ToggleChild(false);
    }

    // =====================
    // POINTER EVENTS
    // =====================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSelected != null && currentSelected != this)
            currentSelected.Deselect();

        AnimateSize(hoverPreferredWidth, hoverPreferredHeight);
        ToggleChild(true);

        AudioManager.Instance.PlaySFX("OnSelect");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;

        AnimateSize(originalWidth, originalHeight);
        ToggleChild(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
        eventData.Use(); // ⛔ prevents background click
    }

    // =====================
    // SELECTION API
    // =====================

    private void Select()
    {
        isSelected = true;
        currentSelected = this;

        AnimateSize(hoverPreferredWidth, hoverPreferredHeight);
        ToggleChild(true);

        AudioManager.Instance.PlaySFX("OnClick");
        GameManager.Instance.selectedTable = GetComponent<TableData>();
    }

    private void Deselect()
    {
        isSelected = false;

        AnimateSize(originalWidth, originalHeight);
        ToggleChild(false);
    }

    public static void DeselectCurrent()
    {
        if (currentSelected == null) return;

        currentSelected.Deselect();
        currentSelected = null;
        GameManager.Instance.selectedTable = null;
    }

    // =====================
    // ANIMATION
    // =====================

    private void AnimateSize(float targetWidth, float targetHeight)
    {
        sizeTween?.Kill();

        Vector2 start = new Vector2(
            layoutElement.preferredWidth,
            layoutElement.preferredHeight
        );

        sizeTween = DOTween.To(
            () => start,
            v =>
            {
                layoutElement.preferredWidth = v.x;
                layoutElement.preferredHeight = v.y;
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            },
            new Vector2(targetWidth, targetHeight),
            duration
        ).SetEase(ease);
    }

    private void ToggleChild(bool state)
    {
        if (transform.childCount > childIndexToToggle)
            transform.GetChild(childIndexToToggle).gameObject.SetActive(state);
    }

    void OnDisable()
    {
        sizeTween?.Kill();
    }
}
