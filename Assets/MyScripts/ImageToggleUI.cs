using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageToggleUI : MonoBehaviour
{
    public SettingType settingType;

    [Header("State")]
    public bool isOn;

    [Header("Track")]
    public Image trackImage;
    public Sprite trackOnSprite;   // green
    public Sprite trackOffSprite;  // red

    [Header("Knob")]
    public RectTransform knob;
    public float knobOnX = 40f;
    public float knobOffX = -40f;

    [Header("Label (Image)")]
    public Image labelImage;
    public Sprite labelOnSprite;   // ON image
    public Sprite labelOffSprite;  // OFF image

    [Header("Animation")]
    public float animationDuration = 0.15f;

    void Start()
    {
        SyncFromSettings();
        ApplyInstant();
    }

    public void Toggle()
    {
        isOn = !isOn;
        StopAllCoroutines();
        StartCoroutine(AnimateToggle());
        SettingsManager.Instance.SetSetting(settingType, isOn);
    }

    void ApplyInstant()
    {
        trackImage.sprite = isOn ? trackOnSprite : trackOffSprite;
        labelImage.sprite = isOn ? labelOnSprite : labelOffSprite;

        Vector2 pos = knob.anchoredPosition;
        pos.x = isOn ? knobOnX : knobOffX;
        knob.anchoredPosition = pos;
    }

    IEnumerator AnimateToggle()
    {
        trackImage.sprite = isOn ? trackOnSprite : trackOffSprite;
        labelImage.sprite = isOn ? labelOnSprite : labelOffSprite;

        float startX = knob.anchoredPosition.x;
        float endX = isOn ? knobOnX : knobOffX;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / animationDuration;
            float x = Mathf.Lerp(startX, endX, Mathf.SmoothStep(0, 1, t));
            knob.anchoredPosition = new Vector2(x, knob.anchoredPosition.y);
            yield return null;
        }

        knob.anchoredPosition = new Vector2(endX, knob.anchoredPosition.y);
    }

    void SyncFromSettings()
    {
        switch (settingType)
        {
            case SettingType.Music:
                isOn = SettingsManager.Instance.musicOn;
                break;

            case SettingType.Sound:
                isOn = SettingsManager.Instance.soundOn;
                break;

            case SettingType.Notifications:
                isOn = SettingsManager.Instance.notificationsOn;
                break;
        }
    }
}

