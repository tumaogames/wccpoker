// ResponsiveUIManager.cs
// Unity 2022.3+ / C#10+
//
// Features
// ✔ CanvasScaler profiles per aspect (auto MatchWidthOrHeight tuning)
// ✔ Portrait/Landscape detection OR forced mode
// ✔ Separate reference resolutions per orientation
// ✔ Safe Area application (notches, rounded corners)
// ✔ Strict-aspect subtrees (letter/pillar box)
// ✔ Auto switch UI layouts (Portrait root vs Landscape root)
// ✔ DPI legibility clamp
// ✔ Raycast hygiene for decorative Images
// ✔ Editor + device runtime updates, with debug overlay
//
// Setup
// 1) Add a Canvas + CanvasScaler (Screen Space – Overlay).
// 2) Add this ResponsiveUIManager to the same GameObject.
// 3) Set Portrait/ Landscape reference resolutions as you design.
// 4) Drag any top-level panels into Safe Area Panels (if needed).
// 5) For auto layout switching: add OrientationLayoutSwitcher to a parent,
//    assign PortraitRoot and LandscapeRoot (two child GameObjects).
//    The manager will toggle them on orientation changes.
// 6) Press Play.

using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
public sealed class ResponsiveUIManager : MonoBehaviour
{
    // ---------- Orientation ----------
    public enum OrientationMode { Auto, Portrait, Landscape }

    [Header("Orientation")]
    public OrientationMode forceOrientation = OrientationMode.Auto;
    public Vector2 portraitReferenceResolution = new(1080, 1920);
    public Vector2 landscapeReferenceResolution = new(1920, 1080);

    ScreenOrientation _lastOrientation;

    // ---------- Canvas Scaler ----------
    [Header("Canvas Scaler (auto-managed)")]
    public bool manageCanvasScaler = true;
    [Tooltip("Default reference Resolution if you don’t want orientation-specific ones.")]
    public Vector2 referenceResolution = new(1080, 1920);
    [Range(0f, 1f)] public float defaultMatchWidthOrHeight = 0.5f;

    [Header("Aspect Handling")]
    [Tooltip("If ON, uses normalized aspect = max(width,height)/min(width,height) so profiles work in both portrait and landscape. If OFF, uses width/height (portrait < 1, landscape > 1).")]
    public bool useOrientationAgnosticAspect = true;

    [Header("Aspect Profiles (Match width/height by aspect)")]
    public AspectProfile[] aspectProfiles = new[]
    {
        // ===================== SQUARE & TABLETS =====================
        new AspectProfile { maxAspect = 1.00f,   matchWidthOrHeight = 0.0f, label = "1:1 Square → favor height" },
        new AspectProfile { maxAspect = 5f/4f,   matchWidthOrHeight = 0.0f, label = "5:4 (1.25) → favor height" },
        new AspectProfile { maxAspect = 4f/3f,   matchWidthOrHeight = 0.0f, label = "4:3 (1.33) tablets → favor height" },   // iPad classic
        new AspectProfile { maxAspect = 3f/2f,   matchWidthOrHeight = 0.0f, label = "3:2 (1.50) → favor height" },            // Surface/Nexus
        new AspectProfile { maxAspect = 16f/10f, matchWidthOrHeight = 0.0f, label = "16:10 (1.60) → favor height" },          // Android tablets

        // ===================== LANDSCAPE / CLASSIC PHONES =====================
        new AspectProfile { maxAspect = 16f/9f,  matchWidthOrHeight = 0.5f, label = "16:9 (1.78) → balanced (TVs, older phones)" },
        new AspectProfile { maxAspect = 17f/9f,  matchWidthOrHeight = 0.5f, label = "17:9 (1.89) → balanced (cinema-ish)" },
        new AspectProfile { maxAspect = 19f/10f, matchWidthOrHeight = 0.5f, label = "19:10 (1.90) → balanced" },

        // ===================== MODERN TALL / PORTRAIT PHONES =====================
        new AspectProfile { maxAspect = 18f/9f,   matchWidthOrHeight = 1.0f, label = "18:9 / 2:1 (2.00) → favor width (portrait)" },
        new AspectProfile { maxAspect = 19f/9f,   matchWidthOrHeight = 1.0f, label = "19:9 (2.11) → favor width (portrait)" },
        new AspectProfile { maxAspect = 19.5f/9f, matchWidthOrHeight = 1.0f, label = "19.5:9 (2.17) → favor width (portrait)" },  // iPhone X–15
        new AspectProfile { maxAspect = 19.8f/9f, matchWidthOrHeight = 1.0f, label = "19.8:9 (2.20) → favor width (portrait)" },
        new AspectProfile { maxAspect = 20f/9f,   matchWidthOrHeight = 1.0f, label = "20:9 (2.22) → favor width (portrait)" },

        // ===================== ULTRAWIDE LANDSCAPE =====================
        new AspectProfile { maxAspect = 21f/9f, matchWidthOrHeight = 0.0f, label = "21:9 (2.33) → favor height (landscape ultrawide)" },
        new AspectProfile { maxAspect = 22f/9f, matchWidthOrHeight = 0.0f, label = "22:9 (2.44) → favor height (landscape ultrawide)" },
        new AspectProfile { maxAspect = 23f/9f, matchWidthOrHeight = 0.0f, label = "23:9 (2.56) → favor height (landscape ultrawide)" },

        // ===================== EXTREME TALL / SAFETY =====================
        new AspectProfile { maxAspect = 3.00f, matchWidthOrHeight = 1.0f, label = "≥2.56–3.00 → favor width (extreme tall portrait / folds)" },
    };


    [Header("DPI Clamp (legibility)")]
    public bool enableDpiClamp = true;
    public float dpiClampThreshold = 450f;
    public float dpiClampMax = 450f;

    // ---------- Safe Area / Strict Aspect ----------
    [Header("Safe Area")]
    public RectTransform[] safeAreaPanels;

    [Header("Strict Aspect (optional)")]
    public StrictAspectEntry[] strictAspectPanels;

    // ---------- Hygiene / Debug ----------
    [Header("Raycast Hygiene")]
    public bool disableDecorativeRaycasts = true;

    [Header("Debug")]
    public bool showDebugOverlay = true;
    public Color debugTextShadow = new(0, 0, 0, 0.5f);
    [Range(8, 24)] public int debugTextSize = 11;

    [Header("Aspect Profile Override")]
    [Tooltip("If ON, forces the selected AspectProfileIndex below instead of auto-picking by aspect.")]
    public bool overrideAspectProfile = false;

    [Tooltip("Index into AspectProfiles to use when Override is ON.")]
    public int overrideAspectProfileIndex = 0;

    [Header("Runtime (read-only)")]
    [Tooltip("The index of the currently applied aspect profile. -1 if none.")]
    public int activeAspectProfileIndex = -1;
    [Tooltip("The label of the currently applied aspect profile.")]
    public string activeAspectProfileLabel = "";


    // ---------- Internals ----------
    Canvas _canvas;
    CanvasScaler _scaler;
    Vector2 _lastScreen;
    Rect _lastSafe;
    float _lastDpi;
    float _lastMatch;
    float _lastDpp;
    float _cachedDeviceDpi;

    // Event others can subscribe to (e.g., OrientationLayoutSwitcher)
    public static event Action<ScreenOrientation> OnOrientationChanged;

    void Reset()
    {
        TryCache();
        if (_scaler)
        {
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = referenceResolution;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = defaultMatchWidthOrHeight;
        }
    }

    void OnEnable()
    {
        TryCache();
        _lastOrientation = DetectOrientation();
        ApplyOrientation(_lastOrientation, force: true);
        ApplyAll(force: true);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorTick;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorTick;
#endif
    }

#if UNITY_EDITOR
    void EditorTick() { ApplyFrame(); }
#endif

    void Update() { ApplyFrame(); }

    void ApplyFrame()
    {
        // Orientation check first
        var current = DetectOrientation();
        if (current != _lastOrientation)
        {
            _lastOrientation = current;
            ApplyOrientation(current, force: true);
            OnOrientationChanged?.Invoke(current); // notify subscribers (layout switchers)
        }

        // Then general updates
        ApplyAll();
    }

    void TryCache()
    {
        if (!_canvas) _canvas = GetComponent<Canvas>();
        if (!_scaler) _scaler = GetComponent<CanvasScaler>();
        if (_scaler && _scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        if (_cachedDeviceDpi <= 0f) _cachedDeviceDpi = Mathf.Max(Screen.dpi, 0f);
    }

    ScreenOrientation DetectOrientation()
    {
        if (forceOrientation == OrientationMode.Portrait) return ScreenOrientation.Portrait;
        if (forceOrientation == OrientationMode.Landscape) return ScreenOrientation.LandscapeLeft;
        return Screen.width >= Screen.height ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait;
    }

    void ApplyOrientation(ScreenOrientation o, bool force)
    {
        if (!manageCanvasScaler || !_scaler) return;

        // Use dedicated ref res per orientation; if left to (0,0), fall back to referenceResolution
        Vector2 useRef = referenceResolution;
        if (o == ScreenOrientation.Portrait && portraitReferenceResolution != Vector2.zero)
            useRef = portraitReferenceResolution;
        else if (o == ScreenOrientation.LandscapeLeft && landscapeReferenceResolution != Vector2.zero)
            useRef = landscapeReferenceResolution;

        if (force || _scaler.referenceResolution != useRef)
            _scaler.referenceResolution = useRef;
    }

    void ApplyAll(bool force = false)
    {
        float w = Screen.width;
        float h = Mathf.Max(1, Screen.height);
        float dpi = Mathf.Max(Screen.dpi, _cachedDeviceDpi);

        bool changed = force
            || !Mathf.Approximately(_lastScreen.x, w)
            || !Mathf.Approximately(_lastScreen.y, h)
            || !Mathf.Approximately(_lastDpi, dpi)
            || Screen.safeArea != _lastSafe;

        if (!changed) return;

        _lastScreen = new Vector2(w, h);
        _lastDpi = dpi;
        _lastSafe = Screen.safeArea;

        // CanvasScaler tuning
        if (manageCanvasScaler && _scaler)
        {
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = ChooseMatchByAspect(w, h, defaultMatchWidthOrHeight);

            // DPI legibility clamp
            float effectiveDpi = dpi;
            if (enableDpiClamp && dpi > dpiClampThreshold) effectiveDpi = Mathf.Min(dpi, dpiClampMax);

            float baseDpp = 1f;
            float dpp = baseDpp * Mathf.Max(1f, effectiveDpi / 326f); // 326 ~ Retina baseline
            dpp = Mathf.Clamp(dpp, 1f, 2.25f);
            if (!Mathf.Approximately(_lastDpp, dpp))
            {
                _scaler.dynamicPixelsPerUnit = dpp;
                _lastDpp = dpp;
            }
            _lastMatch = _scaler.matchWidthOrHeight;
        }

        // Layout adjustments
        ApplySafeArea();
        ApplyStrictAspect();
        if (disableDecorativeRaycasts) StripDecorativeRaycasts();
    }

    float ChooseMatchByAspect(float width, float height, float fallback)
    {
        activeAspectProfileIndex = -1;
        activeAspectProfileLabel = "";

        if (aspectProfiles == null || aspectProfiles.Length == 0)
            return fallback;

        // Manual override wins
        if (overrideAspectProfile)
        {
            int idx = Mathf.Clamp(overrideAspectProfileIndex, 0, aspectProfiles.Length - 1);
            var p = aspectProfiles[idx];
            activeAspectProfileIndex = idx;
            activeAspectProfileLabel = p.label;
            return AdjustForOrientation(p.matchWidthOrHeight);
        }

        float aspect = useOrientationAgnosticAspect
            ? (Mathf.Max(width, height) / Mathf.Max(1f, Mathf.Min(width, height)))
            : (width / Mathf.Max(1f, height));

        int chosenIndex = aspectProfiles.Length - 1;
        for (int i = 0; i < aspectProfiles.Length; i++)
        {
            if (aspect <= aspectProfiles[i].maxAspect)
            {
                chosenIndex = i;
                break;
            }
        }

        var chosen = aspectProfiles[chosenIndex];
        activeAspectProfileIndex = chosenIndex;
        activeAspectProfileLabel = chosen.label;

        return AdjustForOrientation(chosen.matchWidthOrHeight);
    }

    float AdjustForOrientation(float value)
    {
        // In portrait → use as-is
        // In landscape → reverse (1 - value)
        bool isLandscape = (forceOrientation == OrientationMode.Landscape)
                           || (forceOrientation == OrientationMode.Auto && Screen.width >= Screen.height);

        return isLandscape ? 1f - value : value;
    }


    void ApplySafeArea()
    {
        if (safeAreaPanels == null) return;

        Rect sa = Screen.safeArea;
        float w = Mathf.Max(1, Screen.width);
        float h = Mathf.Max(1, Screen.height);

        Vector2 min = sa.position;
        Vector2 max = sa.position + sa.size;
        min.x /= w; min.y /= h;
        max.x /= w; max.y /= h;

        for (int i = 0; i < safeAreaPanels.Length; i++)
        {
            var rt = safeAreaPanels[i];
            if (!rt) continue;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    void ApplyStrictAspect()
    {
        if (strictAspectPanels == null) return;

        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        foreach (var entry in strictAspectPanels)
        {
            if (!entry.panel) continue;
            var rt = entry.panel;

            float targetAspect = entry.targetAspect > 0f
                ? entry.targetAspect
                : referenceResolution.x / Mathf.Max(1f, referenceResolution.y);

            if (screenAspect > targetAspect)
            {
                // Wider than target → clamp width
                float widthScale = targetAspect / screenAspect;
                float side = (1f - widthScale) * 0.5f;
                rt.anchorMin = new Vector2(side, 0f);
                rt.anchorMax = new Vector2(1f - side, 1f);
            }
            else
            {
                // Taller than target → clamp height
                float heightScale = screenAspect / targetAspect;
                float tb = (1f - heightScale) * 0.5f;
                rt.anchorMin = new Vector2(0f, tb);
                rt.anchorMax = new Vector2(1f, 1f - tb);
            }
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            if (entry.backgroundBars) entry.backgroundBars.enabled = true;
        }
    }

    void StripDecorativeRaycasts()
    {
        if (!_canvas) return;
        var images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var img = images[i];
            if (!img) continue;
            bool interactive = img.GetComponentInParent<Selectable>(true) != null;
            if (!interactive && img.raycastTarget) img.raycastTarget = false;
        }
    }

    public void ForceAspectProfile(int index)
    {
        overrideAspectProfile = true;
        overrideAspectProfileIndex = Mathf.Clamp(index, 0, aspectProfiles.Length - 1);
        ApplyAll(force: true);
    }

    public void ClearAspectProfileOverride()
    {
        overrideAspectProfile = false;
        ApplyAll(force: true);
    }


#if UNITY_EDITOR
    void OnGUI()
    {
        if (!showDebugOverlay) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = debugTextSize,
            richText = true
        };

        // ---------- Build text ----------
        var sb = new System.Text.StringBuilder(512);

        float w = Screen.width;
        float h = Mathf.Max(1, Screen.height);
        string aspectRaw = $"{(w / h):0.###}:1";
        float aspectNorm = Mathf.Max(w, h) / Mathf.Max(1f, Mathf.Min(w, h));
        string aspectStr = useOrientationAgnosticAspect
            ? $"{(int)w}×{(int)h} (norm {aspectNorm:0.###}:1)"
            : $"{(int)w}×{(int)h} ({aspectRaw})";

        string dpiStr = _lastDpi > 0 ? $"{_lastDpi:0} dpi (eff DPPU {_lastDpp:0.00})" : "dpi n/a";
        string matchStr = (manageCanvasScaler && _scaler)
            ? $"{_scaler.matchWidthOrHeight:0.00}"
            : $"{_lastMatch:0.00}";

        sb.AppendLine("<b>ResponsiveUIManager</b>");
        sb.AppendLine($"Orientation: {_lastOrientation}");
        sb.AppendLine($"Aspect: {aspectStr}");
        sb.AppendLine($"SafeArea: {Screen.safeArea}");
        sb.AppendLine($"DPI: {dpiStr}");
        sb.AppendLine($"Match (W↔H): {matchStr}");
        sb.AppendLine($"RefRes: {(_scaler ? _scaler.referenceResolution.x : referenceResolution.x):0}×{(_scaler ? _scaler.referenceResolution.y : referenceResolution.y):0}");
        sb.AppendLine($"Override: {(overrideAspectProfile ? $"<color=#FFD54F>ON</color> (idx {overrideAspectProfileIndex})" : "OFF")}");
        sb.AppendLine($"Active Profile: {(activeAspectProfileIndex >= 0 ? $"[{activeAspectProfileIndex}] {activeAspectProfileLabel}" : "n/a")}");

        if (aspectProfiles != null && aspectProfiles.Length > 0)
        {
            sb.AppendLine("\n<b>Aspect Profiles</b>");
            for (int i = 0; i < aspectProfiles.Length; i++)
            {
                var p = aspectProfiles[i];
                bool isActive = (i == activeAspectProfileIndex);
                string line = $"[{i}] ≤ {p.maxAspect:0.###}  match={p.matchWidthOrHeight:0.00}  {p.label}";
                sb.AppendLine(isActive ? $"<color=#80FF80><b>{line}</b></color>" : line);
            }
        }

        string text = sb.ToString();

        // ---------- Draw with shadow ----------
        var r = new Rect(10, 10, 560, 1200);
        var prev = GUI.color;

        GUI.color = debugTextShadow;
        GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);

        GUI.color = Color.white;
        GUI.Label(r, text, style);

        GUI.color = prev;
    }

#endif

    // ---------- Types ----------
    [Serializable]
    public struct AspectProfile
    {
        public float maxAspect;
        [Range(0f, 1f)] public float matchWidthOrHeight;
        public string label;
    }

    [Serializable]
    public struct StrictAspectEntry
    {
        public RectTransform panel;
        [Tooltip("Target aspect ratio (width/height). If <= 0, uses referenceResolution aspect.")]
        public float targetAspect;
        [Tooltip("Optional Image used as background bars (enable when clamped).")]
        public Image backgroundBars;
    }
}
