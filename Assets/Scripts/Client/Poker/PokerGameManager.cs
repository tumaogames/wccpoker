////////////////////
//       RECK       //
////////////////////

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WCC.Poker.Client
{
    public class PokerGameManager : MonoBehaviour
    {
        public List<PokerPlayer> players = new();
        public List<Card> community = new();

        Deck deck = new Deck();
        BettingSystem betting = new BettingSystem();

        PokerPhase phase;

        bool waitingForHuman;
        int humanIndex = 0;
        int highestBet = 0;

        void Start()
        {
            players.Add(new PokerPlayer { name = "You" });
            players.Add(new PokerPlayer { name = "AI 1" });
            players.Add(new PokerPlayer { name = "AI 2" });

            StartRound();
        }

        void StartRound()
        {
            community.Clear();
            betting.Reset();

            foreach (var p in players)
                p.ResetForRound();

            deck.Create();
            deck.Shuffle();
            Deal();
            phase = PokerPhase.PreFlop;
            NextPhase();
        }

        void Deal()
        {
            for (int i = 0; i < 2; i++)
                foreach (var p in players)
                    p.hand.Add(deck.Draw());
        }

        void NextPhase()
        {
            switch (phase)
            {
                case PokerPhase.PreFlop:
                    SimulateBetting();
                    phase = PokerPhase.Flop;
                    RevealFlop();
                    break;

                case PokerPhase.Flop:
                    SimulateBetting();
                    phase = PokerPhase.Turn;
                    RevealCard();
                    break;

                case PokerPhase.Turn:
                    SimulateBetting();
                    phase = PokerPhase.River;
                    RevealCard();
                    break;

                case PokerPhase.River:
                    SimulateBetting();
                    phase = PokerPhase.Showdown;
                    Showdown();
                    break;
            }
        }

        void RevealFlop()
        {
            community.Add(deck.Draw());
            community.Add(deck.Draw());
            community.Add(deck.Draw());
            NextPhase();
        }

        void RevealCard()
        {
            community.Add(deck.Draw());
            NextPhase();
        }

        void SimulateBetting()
        {
            foreach (var p in players.Where(x => !x.folded))
            {
                int bet = Random.Range(10, 50);
                bet = Mathf.Min(bet, p.chips);
                p.chips -= bet;
                p.currentBet += bet;
            }
            betting.CollectBets(players);
        }

        void Showdown()
        {
            PokerPlayer winner = null;
            HandScore best = null;

            foreach (var p in players.Where(x => !x.folded))
            {
                var cards = new List<Card>();
                cards.AddRange(p.hand);
                cards.AddRange(community);

                var score = HandEvaluator.EvaluateBest(cards);

                if (best == null || HandEvaluator.Compare(score, best) > 0)
                {
                    best = score;
                    winner = p;
                }
            }

            winner.chips += betting.pot;
            Debug.Log($"Winner: {winner.name} won {betting.pot} chips!");

            Invoke(nameof(StartRound), 2f);
        }

        public void HumanAction(PlayerAction action, int amount)
        {
            var p = players[humanIndex];

            if (action == PlayerAction.Fold)
                p.folded = true;

            if (action == PlayerAction.Call)
                Bet(p, 20);

            if (action == PlayerAction.Raise)
                Bet(p, amount);

            waitingForHuman = false;
        }

        void Bet(PokerPlayer p, int amount)
        {
            amount = Mathf.Min(amount, p.chips);   // can't bet more than you have

            p.chips -= amount;
            p.currentBet += amount;

            if (p.currentBet > highestBet)
                highestBet = p.currentBet;

            Debug.Log($"{p.name} bets {amount}");
        }
    }

}
