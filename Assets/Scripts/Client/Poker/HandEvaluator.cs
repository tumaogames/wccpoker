////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;
using System.Linq;

namespace WCC.Poker.Client
{
    public class HandScore
    {
        public int rank;
        public List<int> values = new List<int>();
    }

    public static class HandEvaluator
    {
        public static HandScore EvaluateBest(List<Card> cards)
        {
            var combos = Get5CardCombos(cards);
            HandScore best = null;

            foreach (var c in combos)
            {
                var score = Evaluate5(c);
                if (best == null || Compare(score, best) > 0)
                    best = score;
            }
            return best;
        }

        static HandScore Evaluate5(List<Card> cards)
        {
            cards = cards.OrderByDescending(c => (int)c.rank).ToList();

            bool flush = cards.All(c => c.suit == cards[0].suit);
            bool straight = cards.Select(c => (int)c.rank)
                                  .Distinct().Count() == 5 &&
                                  (int)cards[0].rank - (int)cards[4].rank == 4;

            var groups = cards.GroupBy(c => c.rank)
                               .OrderByDescending(g => g.Count())
                               .ThenByDescending(g => g.Key)
                               .ToList();

            if (straight && flush)
                return new HandScore { rank = 9, values = new List<int> { (int)cards[0].rank } };

            if (groups[0].Count() == 4)
                return new HandScore { rank = 8, values = groups.Select(g => (int)g.Key).ToList() };

            if (groups[0].Count() == 3 && groups[1].Count() == 2)
                return new HandScore { rank = 7, values = groups.Select(g => (int)g.Key).ToList() };

            if (flush)
                return new HandScore { rank = 6, values = cards.Select(c => (int)c.rank).ToList() };

            if (straight)
                return new HandScore { rank = 5, values = new List<int> { (int)cards[0].rank } };

            if (groups[0].Count() == 3)
                return new HandScore { rank = 4, values = groups.Select(g => (int)g.Key).ToList() };

            if (groups[0].Count() == 2 && groups[1].Count() == 2)
                return new HandScore { rank = 3, values = groups.Select(g => (int)g.Key).ToList() };

            if (groups[0].Count() == 2)
                return new HandScore { rank = 2, values = groups.Select(g => (int)g.Key).ToList() };

            return new HandScore { rank = 1, values = cards.Select(c => (int)c.rank).ToList() };
        }

        public static int Compare(HandScore a, HandScore b)
        {
            if (a.rank != b.rank) return a.rank.CompareTo(b.rank);
            for (int i = 0; i < a.values.Count; i++)
                if (a.values[i] != b.values[i])
                    return a.values[i].CompareTo(b.values[i]);
            return 0;
        }

        static List<List<Card>> Get5CardCombos(List<Card> cards)
        {
            var result = new List<List<Card>>();
            for (int a = 0; a < cards.Count; a++)
                for (int b = a + 1; b < cards.Count; b++)
                    for (int c = b + 1; c < cards.Count; c++)
                        for (int d = c + 1; d < cards.Count; d++)
                            for (int e = d + 1; e < cards.Count; e++)
                                result.Add(new List<Card> { cards[a], cards[b], cards[c], cards[d], cards[e] });
            return result;
        }
    }
}