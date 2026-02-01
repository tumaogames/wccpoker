using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gameLoader : MonoBehaviour
{
    public string gameToken;
    public string opId;
    public string websocketUrl = "ws://51.79.160.227:26001/ws";
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
        GameServerClient.Configure(websocketUrl);
        GameServerClient.ConnectWithLaunchToken(gameToken, opId);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
