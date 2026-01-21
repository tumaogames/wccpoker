////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;


namespace WCC.Poker.Client
{
    public class PokerPlayer
    {
        public string name;
        public List<Card> hand = new(2);
        public int chips = 1000;
        public int currentBet;
        public bool folded;
        public bool allIn;

        public void ResetForRound()
        {
            hand.Clear();
            currentBet = 0;
            folded = false;
            allIn = false;
        }
    }
}
