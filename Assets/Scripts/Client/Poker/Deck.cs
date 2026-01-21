////////////////////
//       RECK       //
////////////////////


using System;
using System.Collections.Generic;
using UnityEngine;
using static WCC.Poker.Shared.GlobalHawk;


namespace WCC.Poker.Client
{
    public class Deck
    {
        readonly List<Card> cards = new();

        public void Create()
        {
            cards.Clear();
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
                foreach (Rank r in Enum.GetValues(typeof(Rank)))
                    cards.Add(new Card { suit = s, rank = r });
        }

        public void Shuffle()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, cards.Count);
                (cards[i], cards[rnd]) = (cards[rnd], cards[i]);
            }
        }

        public Card Draw()
        {
            Card c = cards[0];
            cards.RemoveAt(0);
            return c;
        }
    }
}
