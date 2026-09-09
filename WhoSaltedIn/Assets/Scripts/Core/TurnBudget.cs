using System;
using System.Collections.Generic;

namespace Game.Core
{
    public static class TurnBudget
    {
        public static IReadOnlyList<int> Split(int total, int turns, int min, int max, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (turns < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turns), turns, "Turns must be at least 1.");
            }

            if (min < 1 || max < min)
            {
                throw new ArgumentOutOfRangeException(nameof(min), $"Requires 1 <= min <= max, got {min}..{max}.");
            }

            if (total < turns * min || total > turns * max)
            {
                throw new ArgumentOutOfRangeException(nameof(total), total,
                    $"{turns} turns of {min}..{max} cannot add up to {total}.");
            }

            var result = new int[turns];
            var remaining = total;

            for (var i = 0; i < turns; i++)
            {
                var rest = turns - i - 1;
                var low = Math.Max(min, remaining - rest * max);
                var high = Math.Min(max, remaining - rest * min);

                result[i] = random.Next(low, high + 1);
                remaining -= result[i];
            }

            for (var i = result.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }
    }
}
