using Com.poker.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public static class sharedData
{
    public static readonly ConcurrentDictionary<string, PokerTableInfo> pokerTables = new();
    public static string myPlayerID = "";
    public static string mySelectedTableCode = "";
    public static int mySelectedMatchSizeID = 0;
    public static string pendingMatchSizeTableCode = "";
    public static string myLaunchToken = "";
}
