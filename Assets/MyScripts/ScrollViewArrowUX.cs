using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ScrollViewArrowUX : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("References")]
    public ScrollRect scrollRect;
    public Button arrowButton;

    [Header("Direction")]
    public bool scrollRight = true;

    [Header("Tap Settings")]
    [Range(0.05f, 0.5f)]
    public float tapStep = 0.2f;
    public float tapDuration = 0.2f;

    [Header("Hold Settings")]
    public float holdSpeed = 0.6f; // normalized units per second
    public Ease holdEase = Ease.Linear;

    [Header("Visual Feedback")]
    public float pressScale = 0.9f;
    public float pressScaleTime = 0.08f;
    public float releaseScaleTime = 0.12f;

    private Tween scrollTween;
    private bool holding;

    void Update()
    {
        // Disable arrows at edges
        float pos = scrollRect.horizontalNormalizedPosition;

        if (scrollRight)
            arrowButton.interactable = pos < 0.99f;
        else
            arrowButton.interactable = pos > 0.01f;
    }

    // -----------------------------
    // POINTER EVENTS
    // -----------------------------

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!arrowButton.interactable) return;

        holding = true;
        PressVisual();
        StartHoldScroll();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (holding || !arrowButton.interactable) return;

        StepScroll();
    }

    // -----------------------------
    // SCROLL LOGIC
    // -----------------------------

    void StepScroll()
    {
        scrollTween?.Kill();

        float target = scrollRect.horizontalNormalizedPosition +
                       (scrollRight ? tapStep : -tapStep);

        target = Mathf.Clamp01(target);

        scrollTween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            target,
            tapDuration
        )
        .SetEase(Ease.OutCubic)
        .SetUpdate(true);
    }

    void StartHoldScroll()
    {
        scrollTween?.Kill();

        float target = scrollRight ? 1f : 0f;
        float distance = Mathf.Abs(scrollRect.horizontalNormalizedPosition - target);
        float duration = distance / holdSpeed;

        scrollTween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            target,
            duration
        )
        .SetEase(holdEase)
        .SetUpdate(true);
    }

    void Release()
    {
        if (!holding) return;

        holding = false;
        scrollTween?.Kill();
        ReleaseVisual();
    }

    // -----------------------------
    // VISUAL FEEDBACK
    // -----------------------------

    void PressVisual()
    {
        transform.DOScale(pressScale, pressScaleTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    void ReleaseVisual()
    {
        transform.DOScale(1f, releaseScaleTime)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}

