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
        public GlobalHawk.Suit suit;
        public GlobalHawk.Rank rank;

        public override string ToString() => $"{rank} of {suit}";
    }
}
