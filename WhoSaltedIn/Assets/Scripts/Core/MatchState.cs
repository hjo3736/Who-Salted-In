using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class MatchState
    {
        public const int NotStarted = -1;

        readonly List<ulong> order = new List<ulong>();

        public KitchenState Kitchen { get; } = new KitchenState();

        public IReadOnlyList<ulong> Order => order;

        public int TurnIndex { get; private set; } = NotStarted;

        public bool IsRunning => TurnIndex >= 0 && TurnIndex < order.Count;

        public bool IsFinished => order.Count > 0 && TurnIndex >= order.Count;

        public ulong? CurrentPlayer => IsRunning ? order[TurnIndex] : (ulong?)null;

        public bool IsCurrentPlayer(ulong player) => IsRunning && order[TurnIndex] == player;

        public void Apply(IGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            gameEvent.Apply(this);
        }

        internal void SetOrder(IReadOnlyList<ulong> newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }

            if (newOrder.Count == 0)
            {
                throw new ArgumentException("Turn order cannot be empty.", nameof(newOrder));
            }

            order.Clear();
            order.AddRange(newOrder);
            TurnIndex = NotStarted;
        }

        internal void SetTurnIndex(int index)
        {
            if (order.Count == 0)
            {
                throw new InvalidOperationException("Match has no turn order yet.");
            }

            if (index < 0 || index > order.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Turn index must be between 0 and {order.Count}.");
            }

            TurnIndex = index;
        }
    }
}
