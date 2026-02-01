using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectInitializer : MonoBehaviour
{
    private ScrollRect scrollRect;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    IEnumerator Start()
    {
        // Wait 1 frame so layout groups & content size are ready
        yield return null;

        scrollRect.horizontalNormalizedPosition = 0f;
    }
}
