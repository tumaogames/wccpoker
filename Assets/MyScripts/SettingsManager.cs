using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    public bool musicOn;
    public bool soundOn;
    public bool notificationsOn;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        DebugLog();
    }

    public void SetSetting(SettingType type, bool value)
    {
        switch (type)
        {
            case SettingType.Music:
                musicOn = value;
                PlayerPrefs.SetInt("Music", value ? 1 : 0);
                break;

            case SettingType.Sound:
                soundOn = value;
                PlayerPrefs.SetInt("Sound", value ? 1 : 0);
                break;

            case SettingType.Notifications:
                notificationsOn = value;
                PlayerPrefs.SetInt("Notifications", value ? 1 : 0);
                break;
        }

        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        musicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        soundOn = PlayerPrefs.GetInt("Sound", 1) == 1;
        notificationsOn = PlayerPrefs.GetInt("Notifications", 1) == 1;
    }

    void DebugLog()
    {
        Debug.Log($"[Settings] Music:{musicOn} Sound:{soundOn} Notif:{notificationsOn}");
    }
}

