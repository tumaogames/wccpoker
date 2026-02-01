using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("UI")]
    public Image loadingSlider;

    [Header("Scene")]
    public string mainSceneName = "MainScene";

    [Header("Options")]
    public float minLoadTime = 1.5f; // prevents instant flash

    void Start()
    {
        StartCoroutine(LoadMainScene());
    }

    IEnumerator LoadMainScene()
    {
        float timer = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(mainSceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(op.progress / 0.9f);
            loadingSlider.fillAmount = progress;

            yield return null;
        }

        // Force 100%
        loadingSlider.fillAmount = 1f;

        // Ensure minimum load time
        while (timer < minLoadTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        op.allowSceneActivation = true;
    }
}

