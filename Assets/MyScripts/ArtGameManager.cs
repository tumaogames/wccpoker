using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Com.poker.Core;
using Google.Protobuf;
using UnityEngine.UI;

public class ArtGameManager : MonoBehaviour
{
    public static ArtGameManager Instance;
    public bool end;
    [Header("Runtime Injected Data")]
    public string gameTokenID;
    public bool enableSound;
    public TableData selectedTable;
    public RectTransform imageParentContainer;   // Image RectTransform
    public GameObject tablePrefab;          // UI prefab (must have RectTransform)
    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }
    public int currentScore;
    public TMP_Text coinText;
    public bool isInitialized;
    public int CurrentScore {
        get { return currentScore; }
        set {
            currentScore = value;
        }
    }

    private void OnEnable()
    {
        TokenManager.TokenSet += OnTokenSet;
    }

    private void OnDisable()
    {
        TokenManager.TokenSet -= OnTokenSet;
    }

    private void OnTokenSet(string token)
    {
        gameTokenID = token;
        isInitialized = true;
        Debug.Log(" GameManager updated token: " + gameTokenID);
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

        Debug.LogWarning(" GameManager waiting for token...");

  
        ChangeState(GameState.MainMenu);
    }

    private void Update()
    {
        // Safety net: if token arrives after Start or OnEnable missed it, pull once here.
        if (!isInitialized && TokenManager.HasToken())
        {
            gameTokenID = TokenManager.GetToken();
            isInitialized = true;
            Debug.Log(" GameManager late-grabbed token: " + gameTokenID);
        }
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

    public void LoadMainGame()
    {
        SceneManager.LoadScene("PokerGame");
    }
}

public enum SettingType
{
    Music,
    Sound,
    Notifications
}
