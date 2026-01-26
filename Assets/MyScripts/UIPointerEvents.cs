using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(LayoutElement))]
public class LayoutHoverResizeDOTween :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover Size")]
    public float hoverPreferredWidth = 400f;
    public float hoverPreferredHeight = 400f;

    [Header("Animation")]
    public float duration = 0.25f;
    public Ease ease = Ease.OutBack;

    [Header("Child Toggle")]
    public int childIndexToToggle = 1;

    private LayoutElement layoutElement;
    private RectTransform rectTransform;

    private float originalWidth;
    private float originalHeight;

    private Tween sizeTween;

    void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = transform as RectTransform;

        originalWidth = layoutElement.preferredWidth;
        originalHeight = layoutElement.preferredHeight;

        // Ensure child starts hidden
        if (transform.childCount > childIndexToToggle)
        {
            transform.GetChild(childIndexToToggle).gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateSize(hoverPreferredWidth, hoverPreferredHeight);

        ToggleChild(true);
        AudioManager.Instance.PlaySFX("OnClick");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateSize(originalWidth, originalHeight);

        ToggleChild(false);
    }

    private void AnimateSize(float targetWidth, float targetHeight)
    {
        sizeTween?.Kill();

        float startW = layoutElement.preferredWidth;
        float startH = layoutElement.preferredHeight;

        sizeTween = DOTween.To(
            () => new Vector2(startW, startH),
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
        {
            transform.GetChild(childIndexToToggle).gameObject.SetActive(state);
        }
    }

    void OnDisable()
    {
        sizeTween?.Kill();
    }
}
