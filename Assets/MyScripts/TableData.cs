using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TableData : MonoBehaviour
{
    public string tableName;
    public string tableCode;
    public string playerID;
    public string matchSizeID;
    public long smallBlind;
    public long bigBlind;
    public int maxPlayers;
    public long minBuy;
    public long maxBuy;

    //----UGUI----//
    public TMP_Text tableNameTxt;
    public TMP_Text blindTxt;
    public TMP_Text buyInTxt;

    public void SetText()
    {
        if (tableNameTxt != null)
            tableNameTxt.text = tableName ?? string.Empty;
        if (blindTxt != null)
            blindTxt.text = "Php " + smallBlind + " / Php " + bigBlind;
        if (buyInTxt != null)
            buyInTxt.text = "Buy In: Php " + minBuy + " - Php " + maxBuy;
    }
}
