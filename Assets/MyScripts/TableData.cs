using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TableData : MonoBehaviour
{
    public string tableCode;
    public string tableName;
    public long smallBlind;
    public long bigBlind;
    public int maxPlayers;
    public long minBuy;
    public long maxBuy;

    //----UGUI----//
    public TMP_Text tableNameTxt;
    public TMP_Text blindTxt;
    public TMP_Text buyInTxt;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetText()
    {
        tableNameTxt.text = tableName;
        blindTxt.text = "Php " + smallBlind + " / " + "Php " + bigBlind;
        buyInTxt.text = "Buy In: Php " + minBuy + " - " + "Php " + maxBuy;
    }
}
