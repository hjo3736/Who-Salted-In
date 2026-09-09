using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class MatchStarted : IGameEvent
    {
        public IReadOnlyList<ulong> Order { get; }

        public MatchStarted(IReadOnlyList<ulong> order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }

        public void Apply(MatchState state) => state.SetOrder(Order);
    }
}
