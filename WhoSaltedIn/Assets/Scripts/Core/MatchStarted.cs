using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class MatchStarted : IGameEvent
    {
        public IReadOnlyList<ulong> Order { get; }

        public IReadOnlyList<int> Seconds { get; }

        public MatchStarted(IReadOnlyList<ulong> order, IReadOnlyList<int> seconds)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Seconds = seconds ?? throw new ArgumentNullException(nameof(seconds));
        }

        public MatchStarted(TurnSchedule schedule)
            : this(schedule?.Order, schedule?.Seconds)
        {
        }

        public void Apply(MatchState state) => state.SetSchedule(Order, Seconds);
    }
}
