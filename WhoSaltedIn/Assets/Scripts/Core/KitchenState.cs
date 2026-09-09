using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class KitchenState
    {
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> Counts => counts;

        public int CountOf(string stepId)
        {
            if (stepId == null)
            {
                throw new ArgumentNullException(nameof(stepId));
            }

            return counts.TryGetValue(stepId, out var count) ? count : 0;
        }

        internal void Increment(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new ArgumentException("Step id is required.", nameof(stepId));
            }

            counts[stepId] = CountOf(stepId) + 1;
        }
    }
}
