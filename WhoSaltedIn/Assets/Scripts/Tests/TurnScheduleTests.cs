using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class TurnScheduleTests
    {
        static readonly ulong[] Players = { 10, 20, 30 };

        const int Rounds = 3;
        const int PerPlayer = 90;
        const int Min = 20;
        const int Max = 45;

        static TurnSchedule Build(int seed) =>
            TurnSchedule.Build(Players, Rounds, PerPlayer, Min, Max, seed);

        [Test]
        public void Build_OrderAndSecondsHaveSameLength()
        {
            var schedule = Build(7);
            Assert.AreEqual(Players.Length * Rounds, schedule.Order.Count);
            Assert.AreEqual(schedule.Order.Count, schedule.Seconds.Count);
        }

        [Test]
        public void Build_EveryPlayerGetsTheSameTotal()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var schedule = Build(seed);

                foreach (var player in Players)
                {
                    var total = schedule.Order
                        .Select((p, i) => (p, i))
                        .Where(x => x.p == player)
                        .Sum(x => schedule.Seconds[x.i]);

                    Assert.AreEqual(PerPlayer, total, $"seed {seed} player {player}");
                }
            }
        }

        [Test]
        public void Build_NeverPutsSamePlayerBackToBack()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var order = Build(seed).Order;

                for (var i = 1; i < order.Count; i++)
                {
                    Assert.AreNotEqual(order[i - 1], order[i], $"seed {seed}");
                }
            }
        }

        [Test]
        public void Build_IsDeterministicForSameSeed()
        {
            CollectionAssert.AreEqual(Build(3).Order, Build(3).Order);
            CollectionAssert.AreEqual(Build(3).Seconds, Build(3).Seconds);
        }
    }
}
