////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class BettingSystem
    {
        public int pot;

        public void Reset() => pot = 0;

        public void CollectBets(List<PokerPlayer> players)
        {
            foreach (var p in players)
            {
                pot += p.currentBet;
                p.currentBet = 0;
            }
        }

        public bool OnlyOneLeft(List<PokerPlayer> players)
        {
            return players.Count(p => !p.folded) == 1;
        }
    }
}
