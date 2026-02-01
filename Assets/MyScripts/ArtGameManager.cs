using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Com.poker.Core;
using Google.Protobuf;
using UnityEngine.UI;

public class ArtGameManager : MonoBehaviour
{
    public string selectedTableID;
    public string selectedTableCode;
    public int selectedMaxSizeID;
    public long selectedMinBuyIn;
    public long selectedMaxBuyIn;
    public string selectedTable;
    public string playerID;
    public static ArtGameManager Instance;
    public gameLoader GameLoader;
    public bool end;
    [Header("Runtime Injected Data")]
    public string gameTokenID;
    public bool enableSound;
    public GameObject selecPlayerNow;
    public RectTransform imageParentContainer;   // Image RectTransform
    public RectTransform imageSelectContainer;
    public GameObject tablePrefab;          // UI prefab (must have RectTransform)
    public GameObject selectTablePrefab;
    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }
    public int currentScore;
    public TMP_Text coinText;
    public bool isInitialized;
    public int CurrentScore
    {
        get { return currentScore; }
        set
        {
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
        if (IsMatchSizeList(tableInfo))
        {
            //Eto yong part na mag gegenerate ng match size options sa UI
            //Debug.Log($"[LobbySample] MatchSize list for table={selectedTableCode} count={tableInfo.Tables.Count}");
            ChildClear(imageSelectContainer);
            foreach (var t in tableInfo.Tables)
            {
                Debug.Log($"  matchSizeId={t.TableId} maxPlayers={t.MaxPlayers} minStart={t.MinPlayersToStart}");
                //dito mo add yung UI spawn code for match size options
                SelectSpawn(t);
            }
        }
        else
        {
            //Eto yong part na mag gegenreate ng table sa UI
            foreach (var item in tableInfo.Tables)
            {
                Spawn(item);
            }
        }
    }
    static bool IsMatchSizeList(PokerTableList list)
    {
        if (list == null || list.Tables == null || list.Tables.Count == 0)
         return false;

        var code = list.Tables[0].TableCode;
        foreach (var t in list.Tables)
        {
            if (t == null)
                return false;
            if (!string.Equals(t.TableCode, code))
                return false;
            if (!int.TryParse(t.TableId, out _))
                return false;
        }
        return true;
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
        tableDataInfo.tableCode = tableItem.TableCode;
        tableDataInfo.smallBlind = tableItem.SmallBlind;
        tableDataInfo.minBuy = tableItem.MinBuyIn;
        tableDataInfo.maxBuy = tableItem.MaxBuyIn;
        tableDataInfo.SetText();
    }

    public void SelectSpawn(PokerTableInfo tableItem)
    {
        GameObject instance = Instantiate(selectTablePrefab, imageSelectContainer);
        RectTransform rt = instance.GetComponent<RectTransform>();
        // Reset transform so it fits correctly
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var childTableDataInfo = instance.GetComponent<ChildTableData>();
        childTableDataInfo.childTableCode = tableItem.TableCode;
        childTableDataInfo.minPlayers = tableItem.MinPlayersToStart;
        childTableDataInfo.maxPlayers = tableItem.MaxPlayers;
        childTableDataInfo.minBuyIn = tableItem.MinBuyIn;
        childTableDataInfo.maxBuyIn = tableItem.MaxBuyIn;
        if (!int.TryParse(tableItem.TableId, out var matchSizeId))
        {
            Debug.LogWarning($"[LobbySample] Invalid match size id '{tableItem.TableId}' for table {tableItem.TableCode}.");
            matchSizeId = 0;
        }
        childTableDataInfo.matchSizeId = matchSizeId;
        childTableDataInfo.ChildSetText();
    }

    public void ChildClear(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    public void PopUpSelectPlayer()
    {
        selecPlayerNow.SetActive(true);
    }

    public void ClosePopUpSelectPlayer()
    {
        selecPlayerNow.SetActive(false);
    }

    public void PlayPopUpSelectPlayer()
    {
        //GameServerClient.SendJoinTableStatic(tableCode, 0);
        SceneManager.LoadScene("PokerGame");
    }
}

public enum SettingType
{
    Music,
    Sound,
    Notifications
}
