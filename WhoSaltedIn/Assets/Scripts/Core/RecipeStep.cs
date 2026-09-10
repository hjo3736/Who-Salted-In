using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class RecipeStep
    {
        public string Id { get; }

        public string DisplayName { get; }

        public float WorkSeconds { get; }

        public float ObserveSeconds { get; }

        public IReadOnlyList<string> Requires { get; }

        public string RequiredItem { get; }

        public string ProducesItem { get; }

        public bool ConsumesItem { get; }

        public IReadOnlyList<string> RequiredNearby { get; }

        public string NearbyBecomes { get; }

        public string ObserveItem { get; }

        public bool Observable { get; }

        public Accumulation Accumulation { get; }

        public int RequiredCount { get; }

        public IReadOnlyList<string> Readings { get; }

        public IReadOnlyList<string> DistinctReadings { get; }

        public RecipeStep(
            string id,
            bool observable,
            Accumulation accumulation,
            int requiredCount = 1,
            IReadOnlyList<string> readings = null,
            string displayName = null,
            float workSeconds = 3f,
            float observeSeconds = 3f,
            IReadOnlyList<string> requires = null,
            string requiredItem = null,
            string producesItem = null,
            bool consumesItem = false,
            IReadOnlyList<string> requiredNearby = null,
            string nearbyBecomes = null,
            string observeItem = null)
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
            Requires = requires ?? System.Array.Empty<string>();
            RequiredItem = requiredItem ?? KitchenItems.None;
            ProducesItem = producesItem ?? KitchenItems.None;
            ConsumesItem = consumesItem;
            RequiredNearby = requiredNearby ?? System.Array.Empty<string>();
            NearbyBecomes = nearbyBecomes ?? KitchenItems.None;
            ObserveItem = observeItem ?? KitchenItems.None;
            WorkSeconds = workSeconds;
            ObserveSeconds = observeSeconds;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Observable = observable;
            Accumulation = accumulation;
            RequiredCount = requiredCount;
            Readings = readings;

            if (readings == null)
            {
                DistinctReadings = null;
            }
            else
            {
                var distinct = new List<string>();

                foreach (var reading in readings)
                {
                    if (!distinct.Contains(reading))
                    {
                        distinct.Add(reading);
                    }
                }

                DistinctReadings = distinct;
            }
        }

        public int ReadingIndexFor(int count)
        {
            var reading = ReadingFor(count);

            if (reading == null || Observable)
            {
                return -1;
            }

            for (var i = 0; i < DistinctReadings.Count; i++)
            {
                if (DistinctReadings[i] == reading)
                {
                    return i;
                }
            }

            return -1;
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
