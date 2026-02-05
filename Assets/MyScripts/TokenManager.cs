using System;
using UnityEngine;

public class TokenManager : MonoBehaviour
{
    public static TokenManager Instance;
    public static event Action<string> TokenSet;

    public TokenPopup gameTokenPopup;
    public string gameTokenID;
    public bool hasToken;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var existing = FindObjectOfType<TokenManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var go = new GameObject("TokenManager");
        Instance = go.AddComponent<TokenManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Guard against serialized inspector state marking hasToken true with an empty token.
        if (hasToken && string.IsNullOrWhiteSpace(gameTokenID))
        {
            Debug.LogWarning("TokenManager hasToken was true but token is empty. Resetting hasToken.");
            hasToken = false;
        }
    }

    public void SetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError("TokenManager.SetToken called with empty token.");
            hasToken = false;
            return;
        }

        token = token.Trim();
        if (hasToken && gameTokenID == token)
        {
            Debug.Log("TokenManager token unchanged.");
            TokenSet?.Invoke(gameTokenID);
            return;
        }

        gameTokenID = token;
        hasToken = true;
        Debug.Log("TokenManager stored token: " + token);
        TokenSet?.Invoke(gameTokenID);
    }

    public static bool HasToken()
    {
        EnsureInstance();
        return Instance.hasToken && !string.IsNullOrWhiteSpace(Instance.gameTokenID);
    }

    public static string GetToken()
    {
        EnsureInstance();
        return Instance.gameTokenID;
    }
}
