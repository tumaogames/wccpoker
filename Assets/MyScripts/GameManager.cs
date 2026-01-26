using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Com.poker.Core;
using Google.Protobuf;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool end;
    public bool enableSound;
    public RectTransform imageParentContainer;   // Image RectTransform
    public GameObject tablePrefab;          // UI prefab (must have RectTransform)
    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }
    public int currentScore;
    public TMP_Text coinText;
    public int CurrentScore {
        get { return currentScore; }
        set {
            currentScore = value;
        }
    }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }


    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 1f;
                break;
        }

        Debug.Log("Game State changed to: " + newState);
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        ChangeState(GameState.Playing);
    }

    public void StartMusic()
    {
        //AudioManager.Instance.PlayMusic("BGM");
    }

    public void GenerateTable(PokerTableList tableInfo)
    {
        foreach (var item in tableInfo.Tables)
        {
            Spawn(item);
        }
    }

    public void Spawn(PokerTableInfo tableItem)
    {
        GameObject instance = Instantiate(tablePrefab, imageParentContainer);
        RectTransform rt = instance.GetComponent<RectTransform>();
        // Reset transform so it fits correctly
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tableDataInfo = instance.GetComponent<TableData>();
        tableDataInfo.tableName = tableItem.TableName;
        tableDataInfo.smallBlind = tableItem.SmallBlind;
        tableDataInfo.minBuy = tableItem.MinBuyIn;
        tableDataInfo.maxBuy = tableItem.MaxBuyIn;
        tableDataInfo.SetText();
    }
}

public enum SettingType
{
    Music,
    Sound,
    Notifications
}
