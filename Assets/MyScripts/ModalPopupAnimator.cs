using UnityEngine;
using DG.Tweening;

public class ModalPopupAnimator : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup overlay;          // dark background
    public RectTransform popupRoot;      // whole modal

    [Header("Animation")]
    public float fadeDuration = 0.25f;
    public float moveDuration = 0.35f;
    public float scaleDuration = 0.35f;

    public Vector2 hiddenOffset = new Vector2(0, 150f); // from top
    public Ease moveEase = Ease.OutBack;
    public Ease scaleEase = Ease.OutBack;

    Vector2 shownPos;
    public bool isOpen;
    Sequence seq;

    void Awake()
    {
        shownPos = popupRoot.anchoredPosition;
        popupRoot.anchoredPosition = shownPos + hiddenOffset;
        popupRoot.localScale = Vector3.one * 0.85f;

        overlay.alpha = 0;
        overlay.blocksRaycasts = false;
        popupRoot.gameObject.SetActive(false);
    }

    // ================== OPEN ==================
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        seq?.Kill();

        popupRoot.gameObject.SetActive(true);
        overlay.blocksRaycasts = true;
        Debug.Log("open popup modal");
        seq = DOTween.Sequence();

        seq.Join(overlay.DOFade(1f, fadeDuration));

        seq.Join(popupRoot.DOAnchorPos(shownPos, moveDuration)
            .SetEase(moveEase));

        seq.Join(popupRoot.DOScale(1f, scaleDuration)
            .SetEase(scaleEase));

        seq.Play();
        ArtAudioManager.Instance.PlaySFX("OnPop");
    }

    // ================== CLOSE ==================
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        seq?.Kill();

        seq = DOTween.Sequence();

        seq.Join(overlay.DOFade(0f, fadeDuration));

        seq.Join(popupRoot.DOAnchorPos(shownPos + hiddenOffset, moveDuration)
            .SetEase(Ease.InBack));

        seq.Join(popupRoot.DOScale(0.85f, scaleDuration)
            .SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            overlay.blocksRaycasts = false;
            popupRoot.gameObject.SetActive(false);
        });

        seq.Play();
        ArtAudioManager.Instance.PlaySFX("OnClick");
    }
}

