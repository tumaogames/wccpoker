using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectInitializer : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] bool resetOnContentChange = true;
    [SerializeField] bool useEndOfFrame = true;

    ScrollRect scrollRect;
    Coroutine snapRoutine;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    void OnEnable()
    {
        EnsureContentWatcher();
        ScheduleSnap();
    }

    void OnDisable()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }
    }

    IEnumerator Start()
    {
        // Wait 1 frame so layout groups & content size are ready
        yield return null;
        ScheduleSnap();
    }

    void EnsureContentWatcher()
    {
        if (!resetOnContentChange || scrollRect == null || scrollRect.content == null)
            return;

        var watcher = scrollRect.content.GetComponent<ScrollRectContentWatcher>();
        if (watcher == null)
            watcher = scrollRect.content.gameObject.AddComponent<ScrollRectContentWatcher>();
        watcher.Bind(this);
    }

    void ScheduleSnap()
    {
        if (!isActiveAndEnabled)
            return;

        if (snapRoutine != null)
            StopCoroutine(snapRoutine);
        snapRoutine = StartCoroutine(SnapNextFrame());
    }

    IEnumerator SnapNextFrame()
    {
        if (useEndOfFrame)
            yield return new WaitForEndOfFrame();
        else
            yield return null;

        if (scrollRect == null)
            yield break;

        if (scrollRect.content != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }

        scrollRect.horizontalNormalizedPosition = 0f;
        snapRoutine = null;
    }

    internal void NotifyContentChanged()
    {
        if (!resetOnContentChange)
            return;
        ScheduleSnap();
    }

    [DisallowMultipleComponent]
    sealed class ScrollRectContentWatcher : MonoBehaviour
    {
        ScrollRectInitializer owner;

        public void Bind(ScrollRectInitializer target)
        {
            owner = target;
        }

        void OnTransformChildrenChanged()
        {
            owner?.NotifyContentChanged();
        }

        void OnTransformParentChanged()
        {
            owner?.NotifyContentChanged();
        }

        void OnRectTransformDimensionsChange()
        {
            owner?.NotifyContentChanged();
        }
    }
}
