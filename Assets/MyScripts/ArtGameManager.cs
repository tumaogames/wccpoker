using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Com.poker.Core;
using Com.Poker.Core;
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
    string _lastToken;
    [Header("Performance")]
    [SerializeField] bool usePooling = true;
    [SerializeField] bool clearTablesOnRefresh = true;
    readonly List<GameObject> _activeTables = new List<GameObject>();
    readonly List<GameObject> _activeSelectTables = new List<GameObject>();
    readonly Dictionary<GameObject, Queue<GameObject>> _pool = new Dictionary<GameObject, Queue<GameObject>>();
    Transform _poolRoot;
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
        ApplyToken(token, "event");
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
        StartCoroutine(WaitForToken());
    }

    IEnumerator WaitForToken()
    {
        // Safety net: if token arrives after Start or OnEnable missed it, pull once here.
        while (!isInitialized)
        {
            if (TokenManager.HasToken())
            {
                ApplyToken(TokenManager.GetToken(), "late-grab");
                yield break;
            }
            yield return null;
        }
    }

    void ApplyToken(string token, string reason)
    {
        token = token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return;

        if (_lastToken == token && isInitialized)
            return;

        _lastToken = token;
        gameTokenID = token;
        isInitialized = true;
        Debug.Log($"[ArtGameManager] Token set ({reason}).");
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
        if (tableInfo == null || tableInfo.Tables == null)
            return;

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
            if (clearTablesOnRefresh)
                ClearTables();
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
        GameObject instance = GetFromPool(tablePrefab, imageParentContainer, _activeTables);
        if (instance == null)
            return;
        RectTransform rt = instance.GetComponent<RectTransform>();
        ResetRectTransform(rt);
        var tableDataInfo = instance.GetComponent<TableData>();
        if (tableDataInfo == null)
            return;
        tableDataInfo.tableName = tableItem.TableName;
        tableDataInfo.tableCode = tableItem.TableCode;
        tableDataInfo.smallBlind = tableItem.SmallBlind;
        tableDataInfo.minBuy = tableItem.MinBuyIn;
        tableDataInfo.maxBuy = tableItem.MaxBuyIn;
        tableDataInfo.SetText();
    }

    public void SelectSpawn(PokerTableInfo tableItem)
    {
        GameObject instance = GetFromPool(selectTablePrefab, imageSelectContainer, _activeSelectTables);
        if (instance == null)
            return;
        RectTransform rt = instance.GetComponent<RectTransform>();
        ResetRectTransform(rt);
        var childTableDataInfo = instance.GetComponent<ChildTableData>();
        if (childTableDataInfo == null)
            return;
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
        ClearList(_activeSelectTables, content);
    }

    void ClearTables()
    {
        ClearList(_activeTables, imageParentContainer);
    }

    void ClearList(List<GameObject> list, RectTransform fallbackParent)
    {
        if (list.Count == 0 && fallbackParent != null)
        {
            for (int i = fallbackParent.childCount - 1; i >= 0; i--)
            {
                ReturnToPool(fallbackParent.GetChild(i).gameObject);
            }
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            ReturnToPool(list[i]);
        }
        list.Clear();
    }

    GameObject GetFromPool(GameObject prefab, RectTransform parent, List<GameObject> activeList)
    {
        if (prefab == null || parent == null)
            return null;

        GameObject instance = null;
        if (usePooling && _pool.TryGetValue(prefab, out var queue))
        {
            while (queue.Count > 0 && instance == null)
            {
                instance = queue.Dequeue();
            }
        }

        if (instance == null)
        {
            instance = Instantiate(prefab, parent);
            if (usePooling)
                TagPooledItem(instance, prefab);
        }
        else
        {
            instance.transform.SetParent(parent, false);
            instance.SetActive(true);
        }

        activeList.Add(instance);
        return instance;
    }

    void ReturnToPool(GameObject instance)
    {
        if (instance == null)
            return;

        if (!usePooling)
        {
            Destroy(instance);
            return;
        }

        var pooled = instance.GetComponent<PooledUIItem>();
        if (pooled == null || pooled.prefab == null)
        {
            Destroy(instance);
            return;
        }

        if (!_pool.TryGetValue(pooled.prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pool[pooled.prefab] = queue;
        }

        EnsurePoolRoot();
        instance.SetActive(false);
        instance.transform.SetParent(_poolRoot, false);
        queue.Enqueue(instance);
    }

    void TagPooledItem(GameObject instance, GameObject prefab)
    {
        if (instance == null)
            return;

        var pooled = instance.GetComponent<PooledUIItem>();
        if (pooled == null)
            pooled = instance.AddComponent<PooledUIItem>();
        pooled.prefab = prefab;
    }

    void EnsurePoolRoot()
    {
        if (_poolRoot != null)
            return;

        var go = new GameObject("UIItemPool");
        go.SetActive(false);
        _poolRoot = go.transform;
        _poolRoot.SetParent(transform, false);
    }

    static void ResetRectTransform(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void PopUpSelectPlayer()
    {
        selecPlayerNow.SetActive(true);
    }

    public void SelectNow()
    {
        if (LayoutHoverResizeDOTween.TryConfirmSelected())
            return;

        Debug.LogWarning("[ArtGameManager] Select Now clicked but no table is selected.");
    }

    public void ClosePopUpSelectPlayer()
    {
        selecPlayerNow.SetActive(false);
    }

    public void PlayPopUpSelectPlayer()
    {
        if (!TryGetSelectedTable(out var tableCode, out var matchSizeId))
        {
            Debug.LogWarning("[ArtGameManager] Play skipped. Table selection is incomplete.");
            return;
        }

        ApplyGlobalSelection(tableCode, matchSizeId);
        SceneManager.LoadScene("PokerGame");
    }

    public void PlaySelectedChildTable()
    {
        if (ChildLayoutHoverResizeDOTween.TryConfirmSelected())
            return;

        Debug.LogWarning("[ArtGameManager] Play clicked but no child table is selected.");
    }

    bool TryGetSelectedTable(out string tableCode, out int matchSizeId)
    {
        tableCode = selectedTableCode;
        if (string.IsNullOrWhiteSpace(tableCode))
        {
            matchSizeId = 0;
            return false;
        }

        matchSizeId = selectedMaxSizeID;
        if (matchSizeId <= 0)
            matchSizeId = GlobalSharedData.MySelectedMatchSizeID;

        return matchSizeId > 0;
    }

    void ApplyGlobalSelection(string tableCode, int matchSizeId)
    {
        GlobalSharedData.MyPlayerID = playerID;
        GlobalSharedData.MySelectedTableCode = tableCode;
        GlobalSharedData.MySelectedMatchSizeID = matchSizeId;

        if (GameLoader != null)
        {
            GlobalSharedData.MyLaunchToken = GameLoader.gameToken;
            GlobalSharedData.MyWebsocketUrl = GameLoader.websocketUrl;
            GlobalSharedData.MyOperatorGameID = GameLoader.opId;
        }
        else
        {
            GlobalSharedData.MyLaunchToken = gameTokenID;
        }
    }
}

public enum SettingType
{
    Music,
    Sound,
    Notifications
}
