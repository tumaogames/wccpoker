using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextEffect : MonoBehaviour
{
    public TextMeshProUGUI textMeshProUGUI;
    public Image textMeshPro;
    public float floatDistance = 50f;
    public float duration = 1.5f;
    private Vector3 originalPosition;
    private Vector3 originalPosition1;

    private void Awake()
    {
        if(textMeshProUGUI == null)
        {
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            originalPosition = textMeshProUGUI.rectTransform.anchoredPosition;
            originalPosition1 = textMeshPro.rectTransform.anchoredPosition;
            textMeshProUGUI.alpha = 0f;
        }
    }

    public void ShowFloatingText(string message)
    {
        textMeshProUGUI.text = "+$" + message;
        gameObject.SetActive(true);
        StartCoroutine(AnimateText());
    }

    public void ShowFloatingText1()
    {
        StartCoroutine(AnimateText1());
    }

    public IEnumerator AnimateText()
    {
        float time = 0f;
        Vector3 startPos = originalPosition; ;
        Vector3 endPos = originalPosition + new Vector3(0, floatDistance,0);

        while(time < duration)
        {
            float t = time / duration;

            float alpha = t < 0.5f ? Mathf.Lerp(0, 1, t * 2) : Mathf.Lerp(1, 0, (t - 0.5f) * 2);
            textMeshProUGUI.alpha = alpha;

            textMeshProUGUI.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        textMeshProUGUI.alpha = 0f;
        textMeshProUGUI.rectTransform.anchoredPosition = originalPosition;
        gameObject.SetActive(false);
    }
    public IEnumerator AnimateText1()
    {
        float time = 0f;
        Vector3 startPos = originalPosition1;
        Vector3 endPos = originalPosition1 + new Vector3(0, floatDistance, 0);

        while (time < duration)
        {
            float t = time / duration;

            float alpha = t < 0.5f ? Mathf.Lerp(0, 1, t * 2) : Mathf.Lerp(1, 0, (t - 0.5f) * 2);
            textMeshPro.GetComponent<CanvasGroup>().alpha = alpha;

            textMeshPro.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        textMeshPro.GetComponent<CanvasGroup>().alpha = 0f;
        textMeshPro.rectTransform.anchoredPosition = originalPosition1;
        gameObject.SetActive(false);
    }
}
