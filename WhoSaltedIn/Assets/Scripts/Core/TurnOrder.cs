using System;
using System.Collections.Generic;

namespace Game.Core
{
    public static class TurnOrder
    {
        public static IReadOnlyList<T> Build<T>(IReadOnlyList<T> players, int rounds, int seed)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            if (players.Count == 0)
            {
                throw new ArgumentException("At least one player is required.", nameof(players));
            }

            if (rounds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rounds), rounds, "Rounds must be at least 1.");
            }

            var random = new Random(seed);
            var result = new List<T>(players.Count * rounds);
            var bag = new List<T>(players.Count);

            for (var round = 0; round < rounds; round++)
            {
                bag.Clear();
                bag.AddRange(players);
                Shuffle(bag, random);
                AvoidBackToBack(bag, result, random);
                result.AddRange(bag);
            }

            return result;
        }

        static void AvoidBackToBack<T>(IList<T> bag, IReadOnlyList<T> dealt, Random random)
        {
            if (bag.Count < 2 || dealt.Count == 0)
            {
                return;
            }

            if (!EqualityComparer<T>.Default.Equals(bag[0], dealt[dealt.Count - 1]))
            {
                return;
            }

            var swapWith = 1 + random.Next(bag.Count - 1);
            (bag[0], bag[swapWith]) = (bag[swapWith], bag[0]);
        }

        static void Shuffle<T>(IList<T> items, Random random)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
