using System;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class TurnBudgetTests
    {
        const int Total = 90;
        const int Turns = 3;
        const int Min = 20;
        const int Max = 45;

        static System.Collections.Generic.IReadOnlyList<int> Split(int seed) =>
            TurnBudget.Split(Total, Turns, Min, Max, new Random(seed));

        [Test]
        public void Split_AlwaysSumsToTotal()
        {
            for (var seed = 0; seed < 500; seed++)
            {
                Assert.AreEqual(Total, Split(seed).Sum(), $"seed {seed}");
            }
        }

        [Test]
        public void Split_StaysWithinBounds()
        {
            for (var seed = 0; seed < 500; seed++)
            {
                foreach (var value in Split(seed))
                {
                    Assert.GreaterOrEqual(value, Min, $"seed {seed}");
                    Assert.LessOrEqual(value, Max, $"seed {seed}");
                }
            }
        }

        [Test]
        public void Split_ProducesVariedTurnLengths()
        {
            var anyUneven = Enumerable.Range(0, 100)
                .Select(Split)
                .Any(s => s.Distinct().Count() > 1);

            Assert.IsTrue(anyUneven);
        }

        [Test]
        public void Split_IsDeterministicForSameSeed()
        {
            CollectionAssert.AreEqual(Split(42), Split(42));
        }

        [Test]
        public void Split_SingleTurnTakesEverything()
        {
            CollectionAssert.AreEqual(
                new[] { 30 },
                TurnBudget.Split(30, 1, 20, 45, new Random(1)));
        }

        [Test]
        public void Split_ExactFloorLeavesNoVariance()
        {
            CollectionAssert.AreEqual(
                new[] { Min, Min, Min },
                TurnBudget.Split(Min * Turns, Turns, Min, Max, new Random(1)));
        }

        [Test]
        public void Split_RejectsImpossibleTotals()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TurnBudget.Split(Min * Turns - 1, Turns, Min, Max, new Random(1)));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => TurnBudget.Split(Max * Turns + 1, Turns, Min, Max, new Random(1)));
        }

        [Test]
        public void Split_RejectsInvalidBounds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TurnBudget.Split(90, 3, 45, 20, new Random(1)));
        }
    }
}
