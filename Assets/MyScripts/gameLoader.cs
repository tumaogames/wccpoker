using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gameLoader : MonoBehaviour
{
    public string gameToken;
    public string opId;
    public string websocketUrl = "ws://51.79.160.227:26001/ws";
    string _lastConnectedToken;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        Debug.Log("Runs");
        TokenManager.EnsureInstance();

        // Wait until ArtGameManager exists and has a token
        while (ArtGameManager.Instance == null)
        {
            yield return null;
        }

        while (string.IsNullOrWhiteSpace(ArtGameManager.Instance.gameTokenID))
        {
            yield return null;
        }

        gameToken = ArtGameManager.Instance.gameTokenID;
        ConnectWithToken(gameToken, "start");
    }

    void OnEnable()
    {
        TokenManager.TokenSet += OnTokenSet;
    }

    void OnDisable()
    {
        TokenManager.TokenSet -= OnTokenSet;
    }

    void OnTokenSet(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        gameToken = token;
        ConnectWithToken(token, "token-change");
    }

    void ConnectWithToken(string token, string reason)
    {
        if (string.IsNullOrWhiteSpace(opId))
        {
            Debug.LogWarning("[gameLoader] opId is empty. Connect skipped.");
            return;
        }

        if (string.IsNullOrWhiteSpace(websocketUrl))
        {
            Debug.LogWarning("[gameLoader] websocketUrl is empty. Connect skipped.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_lastConnectedToken) && _lastConnectedToken == token)
        {
            Debug.Log($"[gameLoader] Token unchanged. Connect skipped ({reason}).");
            return;
        }

        _lastConnectedToken = token;
        var tokenLen = string.IsNullOrEmpty(token) ? 0 : token.Length;
        NetworkDebugLogger.LogSend("Connect", $"url={websocketUrl} opId={opId} tokenLen={tokenLen} reason={reason}");
        GameServerClient.Configure(websocketUrl);
        GameServerClient.ForceConnectWithLaunchToken(token, opId);
        Debug.Log($"[gameLoader] Connecting ({reason}) token={token}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
