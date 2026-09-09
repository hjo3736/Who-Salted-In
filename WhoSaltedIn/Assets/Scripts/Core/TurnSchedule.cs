using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class TurnSchedule
    {
        public IReadOnlyList<ulong> Order { get; }

        public IReadOnlyList<int> Seconds { get; }

        TurnSchedule(IReadOnlyList<ulong> order, IReadOnlyList<int> seconds)
        {
            Order = order;
            Seconds = seconds;
        }

        public static TurnSchedule Build(
            IReadOnlyList<ulong> players,
            int rounds,
            int secondsPerPlayer,
            int minSeconds,
            int maxSeconds,
            int seed)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            var random = new Random(seed);
            var order = TurnOrder.Build(players, rounds, seed);

            var budgets = new Dictionary<ulong, IReadOnlyList<int>>(players.Count);
            var used = new Dictionary<ulong, int>(players.Count);

            foreach (var player in players)
            {
                budgets[player] = TurnBudget.Split(secondsPerPlayer, rounds, minSeconds, maxSeconds, random);
                used[player] = 0;
            }

            var seconds = new int[order.Count];

            for (var i = 0; i < order.Count; i++)
            {
                var player = order[i];
                seconds[i] = budgets[player][used[player]];
                used[player]++;
            }

            return new TurnSchedule(order, seconds);
        }
    }
}
