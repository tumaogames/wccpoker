using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("UI")]
    public Image loadingSlider;
    public TMP_Text loadingText;
    public string loadingTextFormat = "Loading... {0}%";

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.15f;

    [Header("Popup")]
    public GameObject tokenPopup;   // ADD THIS

    [Header("Scene")]
    public string mainSceneName = "MainScene";

    [Header("Options")]
    public float minLoadTime = 1.5f;

    private AsyncOperation loadOp;
    private bool loadStarted;
    private Coroutine loadRoutine;
    float _targetProgress;
    float _displayProgress;
    float _velocity;
    int _lastPercent = -1;

    void Start()
    {
        TokenManager.EnsureInstance();
        AutoWire();
        TryBeginLoad("start");
    }

    void OnEnable()
    {
        TokenManager.EnsureInstance();
        AutoWire();
        TokenManager.TokenSet += OnTokenSet;
    }

    void OnDisable()
    {
        TokenManager.TokenSet -= OnTokenSet;
    }

    void OnTokenSet(string token)
    {
        TryBeginLoad("token-set");
    }

    void TryBeginLoad(string reason)
    {
        if (loadStarted)
            return;

        if (!TokenManager.HasToken())
        {
            if (tokenPopup != null && !tokenPopup.activeSelf)
                tokenPopup.SetActive(true);
            return;
        }

        if (tokenPopup != null)
        {
            tokenPopup.SetActive(false);
        }

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);
        loadRoutine = StartCoroutine(LoadMainScene(reason));
    }

    void AutoWire()
    {
        if (loadingText != null)
            return;

        var texts = GetComponentsInChildren<TMP_Text>(true);
        TMP_Text fallback = null;
        for (int i = 0; i < texts.Length; i++)
        {
            var name = texts[i].gameObject.name;
            if (name == "PercentText")
            {
                loadingText = texts[i];
                return;
            }
            if (fallback == null && name == "LoadingText")
                fallback = texts[i];
        }

        if (fallback != null)
            loadingText = fallback;
    }

    IEnumerator LoadMainScene(string reason)
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogError("[SceneLoader] mainSceneName is empty. Load aborted.");
            yield break;
        }

        loadStarted = true;
        float timer = 0f;
        _targetProgress = 0f;
        _displayProgress = 0f;
        _velocity = 0f;
        _lastPercent = -1;

        loadOp = SceneManager.LoadSceneAsync(mainSceneName);
        loadOp.allowSceneActivation = false;

        while (loadOp.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;

            _targetProgress = Mathf.Clamp01(loadOp.progress / 0.9f);
            UpdateVisuals();

            yield return null;
        }

        _targetProgress = 1f;

        // Ensure minimum load time
        while (timer < minLoadTime)
        {
            timer += Time.unscaledDeltaTime;
            UpdateVisuals();
            yield return null;
        }

        // Snap to 100% before activation
        _displayProgress = 1f;
        UpdateVisuals(force: true);
        loadOp.allowSceneActivation = true;
    }

    void UpdateVisuals(bool force = false)
    {
        _displayProgress = Mathf.SmoothDamp(
            _displayProgress,
            _targetProgress,
            ref _velocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        if (loadingSlider != null)
            loadingSlider.fillAmount = _displayProgress;

        int percent = Mathf.Clamp(Mathf.RoundToInt(_displayProgress * 100f), 0, 100);
        if (force || percent != _lastPercent)
        {
            _lastPercent = percent;
            if (loadingText != null)
                loadingText.text = string.Format(loadingTextFormat, percent);
        }
    }

    public void ConfirmToken(string token)
    {
        token = token?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("ConfirmToken called with empty token.");
            return;
        }

        Debug.Log("SceneLoader confirmed token: " + token);
        TokenManager.EnsureInstance();
        TokenManager.Instance.SetToken(token);
        TryBeginLoad("confirm");
    }
}

