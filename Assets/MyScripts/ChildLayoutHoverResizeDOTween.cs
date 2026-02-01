using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Com.Poker.Core;
using UnityEngine.SceneManagement;

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
        ArtGameManager.Instance.currentSelectedChildTable = GetComponent<ChildLayoutHoverResizeDOTween>().gameObject;
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
        ArtGameManager.Instance.PopUpSelectPlayer();
        SetGlobalSharedData();
        //GameServerClient.SendJoinTableStatic(ArtGameManager.Instance.selectedTableCode, 0);
    }

    public void SetGlobalSharedData()
    {
        ArtGameManager.Instance.selectedTableCode = GetComponent<ChildTableData>().childTableCode;
        ArtGameManager.Instance.selectedMatchSizeID = GetComponent<ChildTableData>().maxPlayers;
        GlobalSharedData.MyLaunchToken = ArtGameManager.Instance.gameTokenID;
        GlobalSharedData.MyPlayerID = ArtGameManager.Instance.playerID;
        GlobalSharedData.MySelectedTableCode = ArtGameManager.Instance.selectedTableCode;
        GlobalSharedData.MyLaunchToken = ArtGameManager.Instance.GameLoader.gameToken;
        GlobalSharedData.MyWebsocketUrl = ArtGameManager.Instance.GameLoader.websocketUrl;
        GlobalSharedData.MySelectedMatchSizeID = ArtGameManager.Instance.selectedMatchSizeID;
        GlobalSharedData.MyOperatorGameID = ArtGameManager.Instance.GameLoader.opId;
        Debug.Log(
        $"[GlobalSharedData SET]\n" +
        $"MyPlayerID: {GlobalSharedData.MyPlayerID}\n" +
        $"MyLaunchToken: {GlobalSharedData.MyLaunchToken}\n" +
        $"MySelectedTableCode: {GlobalSharedData.MySelectedTableCode}\n" +
        $"MyWebsocketUrl: {GlobalSharedData.MyWebsocketUrl}\n" +
        $"MyOperatorGameID: {GlobalSharedData.MyOperatorGameID}"
        );
        SceneManager.LoadScene("PokerGame");
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
        ArtGameManager.Instance.selectedTable = null;
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
