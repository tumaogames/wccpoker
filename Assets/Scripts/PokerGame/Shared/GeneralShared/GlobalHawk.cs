 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Shared
{
    public static class GlobalHawk
    {
        public enum Suit { Hearts, Diamonds, Clubs, Spades }
        public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
        public enum PokerPhase { PreFlop, Flop, Turn, River, Showdown }
        public enum PlayerAction { Fold, Call, Raise, Check, AllIn }

    }
}
