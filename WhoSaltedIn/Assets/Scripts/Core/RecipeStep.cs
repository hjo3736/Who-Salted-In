using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class RecipeStep
    {
        public string Id { get; }

        public bool Observable { get; }

        public Accumulation Accumulation { get; }

        public int RequiredCount { get; }

        public IReadOnlyList<string> Readings { get; }

        public RecipeStep(
            string id,
            bool observable,
            Accumulation accumulation,
            int requiredCount = 1,
            IReadOnlyList<string> readings = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Step id is required.", nameof(id));
            }

            if (requiredCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCount), requiredCount,
                    "A step must be required at least once.");
            }

            if (!observable && readings != null && readings.Count == 0)
            {
                throw new ArgumentException("Readings must be null or non-empty.", nameof(readings));
            }

            Id = id;
            Observable = observable;
            Accumulation = accumulation;
            RequiredCount = requiredCount;
            Readings = readings;
        }

        public bool CanBeObserved => Observable || Readings != null;

        public string ReadingFor(int count)
        {
            if (Observable)
            {
                return count.ToString();
            }

            if (Readings == null)
            {
                return null;
            }

            var index = Math.Min(Math.Max(count, 0), Readings.Count - 1);
            return Readings[index];
        }
    }
}
