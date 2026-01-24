using BestHTTP;
using Com.poker.Core;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AccountAuthClient : MonoBehaviour
{
    [Serializable]
    public class LoginResponse
    {
        public string accessToken;
        public string playerId;
        public int credits;
    }

    [Serializable]
    public class GameTicketResponse
    {
        public string gameTicket;
        public string expiresAt;
    }

    [Header("Account Server")]
    public string baseUrl = "https://account.game.goscanqr.com";

    [Header("State (read-only)")]
    [SerializeField] string accessToken;
    [SerializeField] string playerId;
    [SerializeField] int credits;
    [SerializeField] string gameTicket;
    [SerializeField] string ticketExpiresAt;
    [SerializeField] string operatorPublicId;

    public string AccessToken => accessToken;
    public string PlayerId => playerId;
    public int Credits => credits;
    public string GameTicket => gameTicket;
    public string TicketExpiresAt => ticketExpiresAt;
    public string OperatorPublicId => operatorPublicId;

    //public UILabel gameTicketLbl;

    [Header("Scene Flow")]
    public string nextSceneName = "PokerTableListScene";
    bool waitForConnectSceneLoad;
    bool connectSubscribed;

    bool isBusy;

    public void LoginAndGetTicket(string username, string password)
    {
        if (isBusy)
        {
            Debug.LogWarning("AccountAuthClient: request in progress.");
            return;
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("AccountAuthClient: username/password required.");
            return;
        }

        isBusy = true;
        SendLogin(username, password);

    }

    public void LoginWithLaunchToken(string launchToken, string operatorPublicIdValue)
    {
        if (string.IsNullOrEmpty(launchToken))
        {
            Debug.LogWarning("AccountAuthClient: launchToken required.");
            return;
        }

        if (string.IsNullOrEmpty(operatorPublicIdValue))
        {
            Debug.LogWarning("AccountAuthClient: operatorPublicId required.");
            return;
        }

        Debug.Log("AccountAuthClient: launchToken set, connecting ws.");
        gameTicket = launchToken;
        operatorPublicId = operatorPublicIdValue;
        ticketExpiresAt = "";
        //if (gameTicketLbl != null)
            //gameTicketLbl.text = gameTicket;

        isBusy = false;

        waitForConnectSceneLoad = true;
        EnsureConnectSubscription();
        //GameServerClient.ConnectWithLaunchToken(gameTicket, operatorPublicId);
    }

    void SendLogin(string username, string password)
    {
        var url = new Uri(baseUrl.TrimEnd('/') + "/auth/login");
        var body = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        var req = new HTTPRequest(url, HTTPMethods.Post, OnLoginFinished);
        req.SetHeader("Content-Type", "application/json");
        req.RawData = Encoding.UTF8.GetBytes(body);
        req.Send();
    }

    void OnLoginFinished(HTTPRequest req, HTTPResponse resp)
    {
        if (!IsSuccess(req, resp, "login")) return;

        var data = JsonUtility.FromJson<LoginResponse>(resp.DataAsText);
        if (data == null || string.IsNullOrEmpty(data.accessToken))
        {
            Fail("login: invalid response body");
            return;
        }

        accessToken = data.accessToken;
        playerId = data.playerId;
        credits = data.credits;

        SendGameTicket(accessToken);
    }

    void SendGameTicket(string token)
    {
        var url = new Uri(baseUrl.TrimEnd('/') + "/auth/game-ticket");
        var req = new HTTPRequest(url, HTTPMethods.Post, OnGameTicketFinished);
        req.SetHeader("Authorization", "Bearer " + token);
        req.SetHeader("Content-Type", "application/json");
        req.RawData = Encoding.UTF8.GetBytes("{}");
        req.Send();
    }

    void OnGameTicketFinished(HTTPRequest req, HTTPResponse resp)
    {
        if (!IsSuccess(req, resp, "game-ticket")) return;

        var data = JsonUtility.FromJson<GameTicketResponse>(resp.DataAsText);
        if (data == null || string.IsNullOrEmpty(data.gameTicket))
        {
            Fail("game-ticket: invalid response body");
            return;
        }

        gameTicket = data.gameTicket;
        ticketExpiresAt = data.expiresAt;
        //gameTicketLbl.text =gameTicket;

        isBusy = false;
        Debug.Log("AccountAuthClient: ticket acquired for playerId=" + playerId);

        if (string.IsNullOrEmpty(operatorPublicId))
        {
            Debug.LogWarning("AccountAuthClient: operatorPublicId missing for websocket connect.");
            return;
        }

        waitForConnectSceneLoad = true;
        EnsureConnectSubscription();
        //GameServerClient.ConnectWithLaunchToken(gameTicket, operatorPublicId);

    }

    void OnEnable()
    {
        EnsureConnectSubscription();
    }

    void OnDisable()
    {
        if (!connectSubscribed)
            return;

        //GameServerClient.ConnectResponseReceivedStatic -= OnConnectResponse;
        connectSubscribed = false;
    }

    void OnConnectResponse(ConnectResponse resp)
    {
        Debug.Log("ConnectResponse OK. Loading scene: " + nextSceneName);
        if (!waitForConnectSceneLoad)
            return;

        waitForConnectSceneLoad = false;
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    void EnsureConnectSubscription()
    {
        if (connectSubscribed)
            return;

        //GameServerClient.ConnectResponseReceivedStatic += OnConnectResponse;
        connectSubscribed = true;
    }

    bool IsSuccess(HTTPRequest req, HTTPResponse resp, string context)
    {
        if (req.State != HTTPRequestStates.Finished || resp == null)
        {
            Fail(context + ": request failed (" + req.State + ")");
            return false;
        }

        if (resp.StatusCode < 200 || resp.StatusCode >= 300)
        {
            Fail(context + ": http " + resp.StatusCode + " " + resp.DataAsText);
            return false;
        }
        
        return true;
    }

    void Fail(string message)
    {
        isBusy = false;
        Debug.LogWarning("AccountAuthClient: " + message);
    }

    [Serializable]
    class LoginRequest
    {
        public string username;
        public string password;
    }
}
