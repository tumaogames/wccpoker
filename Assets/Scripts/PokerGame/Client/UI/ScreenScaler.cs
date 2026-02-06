 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Client
{
    public class ScreenScaler : MonoBehaviour
    {
        [SerializeField] GameObject _screenViewGroup;

        [Header("Resolution Overrides")]
        [SerializeField] bool _useResolutionScaleList = true;
        [SerializeField] ResolutionScale[] _resolutionScales;

        [Header("Auto Fit Target")]
        [SerializeField] bool _autoFitTarget = true;
        [SerializeField] RectTransform _targetRectToFit;
        [SerializeField, HideInInspector] Renderer _targetRendererToFit;
        [SerializeField] RectTransform _viewRect;
        [SerializeField, HideInInspector] Camera _worldCamera;
        [SerializeField, Min(0f)] float _fitPaddingPixels = 16f;
        [SerializeField] Vector2 _minMaxScale = new Vector2(0.6f, 1.2f);

        Vector2Int _lastScreen;
        Vector3 _baseScale = Vector3.one;

        [System.Serializable]
        public class ResolutionScale
        {
            public Vector2Int Resolution;
            public Vector3 Scale = Vector3.one;
        }

        void OnEnable()
        {
            ApplyScaleForCurrentScreen();
        }

        void Update()
        {
            var now = new Vector2Int(Screen.width, Screen.height);
            if (now != _lastScreen)
            {
                _lastScreen = now;
                ApplyScaleForCurrentScreen();
            }
        }

        public void ApplyScaleForCurrentScreen()
        {
            var root = _screenViewGroup != null ? _screenViewGroup.transform : transform;
            _baseScale = GetScaleForCurrentResolution();
            root.localScale = _baseScale;

            if (_autoFitTarget)
                FitTargetToScreen(root);
        }

        Vector3 GetScaleForCurrentResolution()
        {
            if (!_useResolutionScaleList || _resolutionScales == null || _resolutionScales.Length == 0)
                return Vector3.one;

            var current = new Vector2(Screen.width, Screen.height);
            var currentAspect = current.x / Mathf.Max(1f, current.y);

            ResolutionScale best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _resolutionScales.Length; i++)
            {
                var entry = _resolutionScales[i];
                if (entry == null) continue;

                var res = entry.Resolution;
                if (res.x <= 0 || res.y <= 0) continue;

                var aspect = res.x / Mathf.Max(1f, res.y);
                var aspectDiff = Mathf.Abs(currentAspect - aspect);
                var sizeDiff = Mathf.Abs(current.x - res.x) + Mathf.Abs(current.y - res.y);
                var score = aspectDiff * 10000f + sizeDiff;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = entry;
                }
            }

            return best != null ? best.Scale : Vector3.one;
        }

        void FitTargetToScreen(Transform root)
        {
            if (_targetRectToFit != null)
            {
                var view = _viewRect != null ? _viewRect : root as RectTransform;
                if (view == null)
                {
                    var parentCanvas = root.GetComponentInParent<Canvas>();
                    if (parentCanvas != null)
                        view = parentCanvas.GetComponent<RectTransform>();
                }
                if (view == null)
                    return;

                var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view, _targetRectToFit);
                var viewRect = view.rect;

                var uiUsableWidth = Mathf.Max(1f, viewRect.width - (_fitPaddingPixels * 2f));
                var uiUsableHeight = Mathf.Max(1f, viewRect.height - (_fitPaddingPixels * 2f));

                if (targetBounds.size.x > uiUsableWidth || targetBounds.size.y > uiUsableHeight)
                {
                    var scaleFactor = Mathf.Min(uiUsableWidth / targetBounds.size.x, uiUsableHeight / targetBounds.size.y);
                    var clamped = Mathf.Clamp(scaleFactor, _minMaxScale.x, _minMaxScale.y);
                    root.localScale = _baseScale * clamped;
                }
                return;
            }

            if (_targetRendererToFit == null)
                return;

            var cam = _worldCamera != null ? _worldCamera : Camera.main;
            if (cam == null)
                return;

            var bounds = _targetRendererToFit.bounds;
            var corners = new Vector3[8];
            var ext = bounds.extents;
            var cen = bounds.center;
            corners[0] = cen + new Vector3(-ext.x, -ext.y, -ext.z);
            corners[1] = cen + new Vector3(ext.x, -ext.y, -ext.z);
            corners[2] = cen + new Vector3(-ext.x, ext.y, -ext.z);
            corners[3] = cen + new Vector3(ext.x, ext.y, -ext.z);
            corners[4] = cen + new Vector3(-ext.x, -ext.y, ext.z);
            corners[5] = cen + new Vector3(ext.x, -ext.y, ext.z);
            corners[6] = cen + new Vector3(-ext.x, ext.y, ext.z);
            corners[7] = cen + new Vector3(ext.x, ext.y, ext.z);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < corners.Length; i++)
            {
                var sp = cam.WorldToScreenPoint(corners[i]);
                min = Vector2.Min(min, sp);
                max = Vector2.Max(max, sp);
            }

            var rectSize = max - min;
            var screenUsableWidth = Mathf.Max(1f, Screen.width - (_fitPaddingPixels * 2f));
            var screenUsableHeight = Mathf.Max(1f, Screen.height - (_fitPaddingPixels * 2f));

            if (rectSize.x > screenUsableWidth || rectSize.y > screenUsableHeight)
            {
                var scaleFactor = Mathf.Min(screenUsableWidth / rectSize.x, screenUsableHeight / rectSize.y);
                var clamped = Mathf.Clamp(scaleFactor, _minMaxScale.x, _minMaxScale.y);
                root.localScale = _baseScale * clamped;
            }
        }
    }
}
