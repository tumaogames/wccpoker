using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Com.poker.Core;
using UnityEngine.UI;
using WCC.Core;

public class ArtGameManager : MonoBehaviour
{
    [Header("Shared Runtime Data (Inspector)")]
    [SerializeField] public string playerID = string.Empty;
    [SerializeField] public string launchToken = string.Empty;
    [SerializeField] public string selectedTableCode = string.Empty;
    [SerializeField] public int selectedMatchSizeID = 0;
    [SerializeField] public string pendingMatchSizeTableCode = string.Empty;
    [SerializeField] public string websocketUrl = string.Empty;
    [SerializeField] public string operatorGameID = string.Empty;
    [SerializeField] public string selectedMinBuyIn = string.Empty;
    [SerializeField] public string selectedMaxBuyIn = string.Empty;

    [Header("Runtime Injected Data")]
    public string gameTokenID;
    public bool enableSound;
    public GameObject selecPlayerNow;
    public RectTransform imageParentContainer;
    public RectTransform imageSelectContainer;
    public GameObject tablePrefab;
    public GameObject selectTablePrefab;
    public int currentScore;
    public TMP_Text coinText;
    public bool isInitialized;
    public static ArtGameManager Instance;
    public gameLoader GameLoader;
    public bool end;
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
        set { currentScore = value; }
    }

    void OnEnable()
    {
        TokenManager.TokenSet += OnTokenSet;
        GameServerClient.ConnectResponseReceivedStatic += OnConnectResponse;
        GameServerClient.TableListReceivedStatic += OnTableListReceived;
        SyncManagerState();
        TrySyncPlayerIdentityFromClient();
    }

    void OnDisable()
    {
        TokenManager.TokenSet -= OnTokenSet;
        GameServerClient.ConnectResponseReceivedStatic -= OnConnectResponse;
        GameServerClient.TableListReceivedStatic -= OnTableListReceived;
    }

    void OnTokenSet(string token)
    {
        ApplyToken(token, "event");
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SyncManagerState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.LogWarning(" GameManager waiting for token...");
        StartCoroutine(WaitForToken());
    }

    IEnumerator WaitForToken()
    {
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
        launchToken = token;
        gameTokenID = token;
        isInitialized = true;
        Debug.Log($"[ArtGameManager] Token set ({reason}).");
    }

    void OnConnectResponse(ConnectResponse resp)
    {
        if (resp == null)
            return;

        SetPlayerIdentity(resp.PlayerId);

        if (coinText != null)
            coinText.text = "Php " + resp.Credits;
    }

    void OnTableListReceived(PokerTableList tableList)
    {
        GenerateTable(tableList);
    }

    void TrySyncPlayerIdentityFromClient()
    {
        var client = GameServerClient.Instance;
        if (client == null)
            return;

        SetPlayerIdentity(client.PlayerId);
    }

    void SetPlayerIdentity(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        playerID = id;
    }

    void SyncManagerState()
    {
        playerID = FirstNonEmpty(playerID);

        var token = FirstNonEmpty(launchToken, gameTokenID);
        launchToken = token;
        gameTokenID = token;

        selectedTableCode = FirstNonEmpty(selectedTableCode);
        pendingMatchSizeTableCode = FirstNonEmpty(pendingMatchSizeTableCode);
        websocketUrl = FirstNonEmpty(websocketUrl);
        operatorGameID = FirstNonEmpty(operatorGameID);

        selectedMatchSizeID = Mathf.Max(0, selectedMatchSizeID);

        if (GameLoader != null)
        {
            if (string.IsNullOrWhiteSpace(launchToken) && !string.IsNullOrWhiteSpace(GameLoader.gameToken))
            {
                launchToken = GameLoader.gameToken;
                gameTokenID = GameLoader.gameToken;
            }

            if (string.IsNullOrWhiteSpace(websocketUrl) && !string.IsNullOrWhiteSpace(GameLoader.websocketUrl))
                websocketUrl = GameLoader.websocketUrl;

            if (string.IsNullOrWhiteSpace(operatorGameID) && !string.IsNullOrWhiteSpace(GameLoader.opId))
                operatorGameID = GameLoader.opId;
        }
    }

    static string FirstNonEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }
        return string.Empty;
    }

    void OnValidate()
    {
        SyncManagerState();
    }

    public void StartMusic()
    {
        //AudioManager.Instance.PlayMusic("BGM");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GenerateTable(PokerTableList tableInfo)
    {
        if (tableInfo == null || tableInfo.Tables == null)
            return;

        if (IsMatchSizeList(tableInfo))
        {
            ChildClear(imageSelectContainer);
            foreach (var t in tableInfo.Tables)
            {
                Debug.Log($"  matchSizeId={t.TableId} maxPlayers={t.MaxPlayers} minStart={t.MinPlayersToStart}");
                SelectSpawn(t);
            }
        }
        else
        {
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
        SyncVaultFromSelection();
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

        matchSizeId = selectedMatchSizeID;
        return matchSizeId > 0;
    }

    void ApplyGlobalSelection(string tableCode, int matchSizeId)
    {
        selectedTableCode = tableCode ?? string.Empty;
        selectedMatchSizeID = Mathf.Max(0, matchSizeId);

        if (GameLoader != null)
        {
            launchToken = GameLoader.gameToken;
            websocketUrl = GameLoader.websocketUrl;
            operatorGameID = GameLoader.opId;
            gameTokenID = launchToken;
        }
        else
        {
            launchToken = gameTokenID;
        }
    }

    void SyncVaultFromSelection()
    {
        // Populate PokerSharedVault so PokerNetConnect can use the lobby selection.
        var token = !string.IsNullOrWhiteSpace(launchToken) ? launchToken : gameTokenID;
        if (!string.IsNullOrWhiteSpace(token))
            PokerSharedVault.LaunchToken = token;

        if (!string.IsNullOrWhiteSpace(websocketUrl))
            PokerSharedVault.ServerURL = websocketUrl;

        if (!string.IsNullOrWhiteSpace(operatorGameID))
            PokerSharedVault.OperatorPublicID = operatorGameID;

        if (!string.IsNullOrWhiteSpace(selectedTableCode))
            PokerSharedVault.TableCode = selectedTableCode;

        if (selectedMatchSizeID > 0)
            PokerSharedVault.MatchSizeId = selectedMatchSizeID;
    }
}

public enum SettingType
{
    Music,
    Sound,
    Notifications
}
