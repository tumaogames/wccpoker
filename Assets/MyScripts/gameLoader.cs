using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gameLoader : MonoBehaviour
{
    public string gameToken;
    public string opId;
    // Start is called before the first frame update
    void Start()
    {
        GameServerClient.Configure("ws://51.79.160.227:26001/ws");
        GameServerClient.ConnectWithLaunchToken(gameToken, opId);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
