// -------------------------------------------------------------
// Auto layout switcher: toggles two roots per orientation.
// Add to any parent that has two children: PortraitRoot and LandscapeRoot.
// -------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;
[DisallowMultipleComponent]
public sealed class OrientationLayoutSwitcher : MonoBehaviour
{
    [Header("Assign two sibling roots")]
    public GameObject portraitRoot;
    public GameObject landscapeRoot;

    [Header("Optional: Safe Area inside these roots")]
    public bool applySafeAreaToRoots = false;

    void OnEnable()
    {
        ResponsiveUIManager.OnOrientationChanged += Handle;
        // Initialize immediately with current orientation guess
        var o = Screen.width >= Screen.height ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
        Handle(o);
    }

    void OnDisable()
    {
        ResponsiveUIManager.OnOrientationChanged -= Handle;
    }

    void Handle(ScreenOrientation o)
    {
        bool portrait = (o == ScreenOrientation.Portrait);
        if (portraitRoot) portraitRoot.SetActive(portrait);
        if (landscapeRoot) landscapeRoot.SetActive(!portrait);

        if (applySafeAreaToRoots)
        {
            ApplySafe(portrait ? portraitRoot : landscapeRoot);
        }
    }

    void ApplySafe(GameObject root)
    {
        if (!root) return;
        var rt = root.GetComponent<RectTransform>();
        if (!rt) return;

        Rect sa = Screen.safeArea;
        float w = Mathf.Max(1, Screen.width);
        float h = Mathf.Max(1, Screen.height);

        Vector2 min = sa.position;
        Vector2 max = sa.position + sa.size;
        min.x /= w; min.y /= h;
        max.x /= w; max.y /= h;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}