 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Shared
{
    public static class GlobalHawk
    {
        public enum Suit { Clubs, Diamonds, Hearts, Spades }
        public enum Rank { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
        public enum PokerPhase { PreFlop, Flop, Turn, River, Showdown }
        public enum PlayerAction { Fold, Call, Raise, Check, AllIn }

        public static Rank TranslateCardRank(int rank)
        {
            return rank switch
            {
                2 => Rank.Two,
                3 => Rank.Three,
                4 => Rank.Four,
                5 => Rank.Five,
                6 => Rank.Six,
                7 => Rank.Seven,
                8 => Rank.Eight,
                9 => Rank.Nine,
                10 => Rank.Ten,
                11 => Rank.Jack,
                12 => Rank.Queen,
                13 => Rank.King,
                14 => Rank.Ace,
                _ => Rank.Two,
            };
        }
    }
}
