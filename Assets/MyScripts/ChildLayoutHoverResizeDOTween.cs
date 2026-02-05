using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(LayoutElement))]
public class ChildLayoutHoverResizeDOTween :
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

    public float doubleClickTime = 0.3f;
    private float lastClickTime;

    // 🔒 Global selection
    private static ChildLayoutHoverResizeDOTween currentSelected;

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

        ArtAudioManager.Instance.PlaySFX("OnSelect");
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
        var manager = ArtGameManager.Instance;
        var data = GetComponent<ChildTableData>();
        manager.selectedTableCode = data.childTableCode;
        manager.selectedMatchSizeID = data.matchSizeId;
        manager.selectedMinBuyIn = data.minBuyIn.ToString();
        manager.selectedMaxBuyIn = data.maxBuyIn.ToString();
        manager.pendingMatchSizeTableCode = string.Empty;
        eventData.Use(); // ⛔ prevents background click
        var client = GameServerClient.Instance;
        if (!client.IsConnected || string.IsNullOrEmpty(client.SessionId))
        {
            Debug.LogWarning("Not connected yet.");
            return;
        }

        float timeSinceLast = Time.unscaledTime - lastClickTime;

        if (timeSinceLast <= doubleClickTime)
        {
            OnDoubleClick();
            lastClickTime = 0f; // reset
        }
        else
        {
            lastClickTime = Time.unscaledTime;
        }
        
    }

    public void OnDoubleClick()
    {
        Debug.Log("DOUBLE CLICK!");
        var manager = ArtGameManager.Instance;
        manager.PopUpSelectPlayer();
        SyncManagerState();
        var matchSizeId = manager.selectedMatchSizeID;
        if (matchSizeId <= 0)
        {
            Debug.LogWarning("MatchSizeId is invalid. Join skipped.");
            return;
        }
        var data = GetComponent<ChildTableData>();
        if (data != null)
        {
            var buyInAmount = data.minBuyIn > 0 ? data.minBuyIn : data.maxBuyIn;
            if (buyInAmount > 0)
                GameServerClient.SetPendingBuyIn(data.childTableCode, buyInAmount);
        }
        NetworkDebugLogger.LogSend("JoinTable", $"tableCode={manager.selectedTableCode} matchSizeId={matchSizeId} (pre-game)");
        GameServerClient.SendJoinTableStatic(manager.selectedTableCode, matchSizeId);
        manager.PlayPopUpSelectPlayer();
    }

    public void SyncManagerState()
    {
        var manager = ArtGameManager.Instance;
        manager.launchToken = manager.gameTokenID;

        if (manager.GameLoader != null)
        {
            manager.launchToken = manager.GameLoader.gameToken;
            manager.websocketUrl = manager.GameLoader.websocketUrl;
            manager.operatorGameID = manager.GameLoader.opId;
        }

        Debug.Log(
        $"[ArtGameManager Sync]\n" +
        $"playerID: {manager.playerID}\n" +
        $"launchToken: {manager.launchToken}\n" +
        $"selectedTableCode: {manager.selectedTableCode}\n" +
        $"websocketUrl: {manager.websocketUrl}\n" +
        $"operatorGameID: {manager.operatorGameID}"
    );
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

        ArtAudioManager.Instance.PlaySFX("OnClick");
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
    }

    public static bool TryConfirmSelected()
    {
        if (currentSelected == null)
            return false;

        var client = GameServerClient.Instance;
        if (!client.IsConnected || string.IsNullOrEmpty(client.SessionId))
        {
            Debug.LogWarning("Not connected yet.");
            return false;
        }

        currentSelected.OnDoubleClick();
        return true;
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
