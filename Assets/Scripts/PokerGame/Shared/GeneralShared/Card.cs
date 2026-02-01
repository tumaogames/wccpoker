////////////////////
//       RECK       //
////////////////////


using System;
using UnityEngine;
using WCC.Poker.Shared;


namespace WCC.Poker.Client
{
    [Serializable]
    public struct Card
    {
        public GlobalHawk.Rank Rank;
        public GlobalHawk.Suit Suit; 
    }
}
