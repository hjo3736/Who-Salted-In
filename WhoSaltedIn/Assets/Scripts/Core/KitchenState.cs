using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class KitchenState
    {
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        readonly Dictionary<string, float> progress = new Dictionary<string, float>();
        readonly Dictionary<string, ItemPlacement> placements = new Dictionary<string, ItemPlacement>();

        public IReadOnlyDictionary<string, int> Counts => counts;

        public IReadOnlyDictionary<string, float> Progress => progress;

        public IReadOnlyDictionary<string, ItemPlacement> Placements => placements;

        public bool TryGetPlacement(string itemId, out ItemPlacement placement) =>
            placements.TryGetValue(itemId, out placement);

        public int CountOf(string stepId)
        {
            if (stepId == null)
            {
                throw new ArgumentNullException(nameof(stepId));
            }

            return counts.TryGetValue(stepId, out var count) ? count : 0;
        }

        public float ProgressOf(string stepId)
        {
            if (stepId == null)
            {
                throw new ArgumentNullException(nameof(stepId));
            }

            return progress.TryGetValue(stepId, out var value) ? value : 0f;
        }

        internal void Increment(string stepId)
        {
            Validate(stepId);

            counts[stepId] = CountOf(stepId) + 1;
            progress.Remove(stepId);
        }

        internal void SetProgress(string stepId, float seconds)
        {
            Validate(stepId);

            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Progress cannot be negative.");
            }

            if (seconds <= 0f)
            {
                progress.Remove(stepId);
                return;
            }

            progress[stepId] = seconds;
        }

        internal void SetPlacements(IReadOnlyList<ItemPlacement> updated)
        {
            if (updated == null)
            {
                throw new ArgumentNullException(nameof(updated));
            }

            foreach (var placement in updated)
            {
                Validate(placement.Key);
                placements[placement.Key] = placement;
            }
        }

        static void Validate(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new ArgumentException("Step id is required.", nameof(stepId));
            }
        }
    }
}
