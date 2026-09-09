using System;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class TurnOrderTests
    {
        static readonly string[] Players = { "a", "b", "c" };

        [Test]
        public void Build_LengthIsPlayersTimesRounds()
        {
            Assert.AreEqual(9, TurnOrder.Build(Players, 3, 1234).Count);
        }

        [Test]
        public void Build_EveryPlayerAppearsExactlyOncePerRound()
        {
            var order = TurnOrder.Build(Players, 3, 1234);

            for (var round = 0; round < 3; round++)
            {
                var slice = order.Skip(round * Players.Length).Take(Players.Length);
                CollectionAssert.AreEquivalent(Players, slice);
            }
        }

        [Test]
        public void Build_NeverPutsSamePlayerBackToBack()
        {
            for (var seed = 0; seed < 500; seed++)
            {
                var order = TurnOrder.Build(Players, 3, seed);

                for (var i = 1; i < order.Count; i++)
                {
                    Assert.AreNotEqual(order[i - 1], order[i],
                        $"seed {seed} put {order[i]} back to back at index {i}");
                }
            }
        }

        [Test]
        public void Build_TwoPlayersAlternate()
        {
            var order = TurnOrder.Build(new[] { "a", "b" }, 4, 12345);

            for (var i = 1; i < order.Count; i++)
            {
                Assert.AreNotEqual(order[i - 1], order[i]);
            }
        }

        [Test]
        public void Build_IsDeterministicForSameSeed()
        {
            CollectionAssert.AreEqual(
                TurnOrder.Build(Players, 3, 99),
                TurnOrder.Build(Players, 3, 99));
        }

        [Test]
        public void Build_SinglePlayerTakesEveryTurn()
        {
            CollectionAssert.AreEqual(
                new[] { "solo", "solo", "solo" },
                TurnOrder.Build(new[] { "solo" }, 3, 7));
        }

        [Test]
        public void Build_RejectsEmptyRoster()
        {
            Assert.Throws<ArgumentException>(() => TurnOrder.Build(Array.Empty<string>(), 2, 0));
        }

        [Test]
        public void Build_RejectsNonPositiveRounds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TurnOrder.Build(Players, 0, 0));
        }
    }
}
