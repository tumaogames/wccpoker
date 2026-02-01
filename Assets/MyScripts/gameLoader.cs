using Com.poker.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class gameLoader : MonoBehaviour
{
    public string gameToken;
    public string opId;
    // Start is called before the first frame update
    void Start()
    {
        GameServerClient.Configure("ws://51.79.160.227:26001/ws");
        GameServerClient.ConnectWithLaunchToken(gameToken, opId);
        sharedData.launcherToken = gameToken;
        sharedData.operatorID = opId;
    }

    //// Update is called once per frame
    //void Update()
    //{

    //}
    void OnEnable()
    {
        GameServerClient.MessageReceivedStatic += OnMsg;

    }
    void OnDisable()
    {
        GameServerClient.MessageReceivedStatic -= OnMsg;

    }
    void OnMsg(MsgType type, Google.Protobuf.IMessage msg)
    {
        switch (type)
        {
            case MsgType.TableList:
                {
                    var list = (PokerTableList)msg;
                    HandleTableList(list);
                    
                    break;
                }

            case MsgType.JoinTableResponse:
                var join = (JoinTableResponse)msg;
                Debug.Log("Joined table: " + join.TableId);
                break;
            case MsgType.TableSnapshot:
                var snap = (TableSnapshot)msg;
                break;
        }
    }
    void HandleTableList(PokerTableList list)
    {
        if (list == null || list.Tables == null || list.Tables.Count == 0)
            return;

        if (IsMatchSizeList(list))
        {
            Debug.Log("matchSize List Detected");
            RenderMatchSizes(list);
            sharedData.pendingMatchSizeTableCode = "";
            return;
        }
        else
        {
            ArtGameManager.Instance.GenerateTable(list);
        }


            //foreach (var t in list.Tables)
            //{
            //    if (t == null || string.IsNullOrEmpty(t.TableCode))
            //        continue;
            //    sharedData.pokerTables[t.TableCode] = t;
            //}

            
    }
    void RenderMatchSizes(PokerTableList list)
    {
      

        foreach (var table in list.Tables)
        {
       

           
                int.TryParse(table.TableId, out var matchSizeId);
             
                Debug.Log("matchSizeId:" +  matchSizeId.ToString() + ",table.MaxPlayers:" + table.MaxPlayers.ToString());
                
               
        }

    }
    bool IsMatchSizeList(PokerTableList list)
    {
        Debug.Log("sharedData.pendingMatchSizeTableCode:" + sharedData.pendingMatchSizeTableCode);
        if (string.IsNullOrEmpty(sharedData.pendingMatchSizeTableCode))
            return false;

        foreach (var t in list.Tables)
        {
            if (!string.Equals(t.TableCode, sharedData.pendingMatchSizeTableCode))
                return false;
            if (!int.TryParse(t.TableId, out _))
                return false;
        }

        return true;
    }
}