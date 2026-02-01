using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CanvasGroupAnimator : MonoBehaviour {

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private bool animateFade;
    [SerializeField] private bool triggerOnStart = true;
    [SerializeField] private bool isLooping;
    [SerializeField] private float fadeTo;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Ease fadeEaseType = Ease.InOutQuad;

    [ContextMenu("Trigger Animate")]
    public void TriggerAnimate() {
        Debug.Log("Triger Animate");
        canvasGroup.DOFade(fadeTo, fadeDuration)
                                   .SetEase(fadeEaseType)
                                   .SetLoops(isLooping ? -1 : 0, LoopType.Yoyo);
    }

    public void TriggerAnimateOut()
    {
        Debug.Log("Triger Animate");
        canvasGroup.DOFade(0, fadeDuration)
                                   .SetEase(fadeEaseType)
                                   .SetLoops(isLooping ? -1 : 0, LoopType.Yoyo);
    }

    public void TriggerAnimateSprite()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            sr.DOFade(fadeTo, fadeDuration)
              .SetEase(fadeEaseType)
              .SetLoops(isLooping ? -1 : 0, LoopType.Yoyo);
        }
    }

    

private void OnEnable() {
        if (!triggerOnStart) return;
        if (animateFade) TriggerAnimate();
    }
}
