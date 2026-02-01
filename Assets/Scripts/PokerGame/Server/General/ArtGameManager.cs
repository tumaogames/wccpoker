using Com.poker.Core;
using TMPro;
using UnityEngine;

public class ArtGameManager : MonoBehaviour
{
    public static ArtGameManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text coinText;

    [Header("State")]
    public string playerID;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void GenerateTable(PokerTableList tableList)
    {
        // Hook up your table UI here.
    }
}
