using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChildTableData : MonoBehaviour
{
    public string childTableCode;
    public int matchSizeId;
    public long minBuyIn;
    public long maxBuyIn;
    public int minPlayers;
    public int maxPlayers;

    //----UGUI----//
    public TMP_Text minPlayersTxt;
    public TMP_Text maxPlayersTxt;

    public void ChildSetText()
    {
        if (minPlayersTxt != null)
            minPlayersTxt.text = "Minimum players: " + minPlayers;
        if (maxPlayersTxt != null)
            maxPlayersTxt.text = "Maximum players: " + maxPlayers;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

    public void ChildSetText()
    {
        minPlayersTxt.text = "Minimum players:" + minPlayers.ToString();
        maxPlayersTxt.text = "Maximum players:" + maxPlayers.ToString();
    }
}
