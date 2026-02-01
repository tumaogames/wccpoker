using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("UI")]
    public Image loadingSlider;

    [Header("Popup")]
    public GameObject tokenPopup;   // ADD THIS

    [Header("Scene")]
    public string mainSceneName = "MainScene";

    [Header("Options")]
    public float minLoadTime = 1.5f;

    private AsyncOperation loadOp;
    private bool tokenConfirmed;


    void Start()
    {
        if (tokenPopup != null)
        {
            tokenPopup.SetActive(true);
        }
        StartCoroutine(WaitForTokenThenLoad());
    }

    IEnumerator WaitForTokenThenLoad()
    {
        // Wait until token is confirmed
        yield return new WaitUntil(() => tokenConfirmed);

        if (tokenPopup != null)
        {
            tokenPopup.SetActive(false);
        }

        float timer = 0f;

        loadOp = SceneManager.LoadSceneAsync(mainSceneName);
        loadOp.allowSceneActivation = false;

        while (loadOp.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;

            if (loadingSlider != null)
            {
                float progress = Mathf.Clamp01(loadOp.progress / 0.9f);
                loadingSlider.fillAmount = progress;
            }

            yield return null;
        }

        // Force 100%
        if (loadingSlider != null)
        {
            loadingSlider.fillAmount = 1f;
        }

        // Ensure minimum load time
        while (timer < minLoadTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        loadOp.allowSceneActivation = true;
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
        tokenConfirmed = true;
    }
}

