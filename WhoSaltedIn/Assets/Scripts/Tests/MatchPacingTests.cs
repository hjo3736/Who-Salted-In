using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class MatchPacingTests
    {
        static MatchPacing For(int players) => MatchPacing.ForPlayers(players, 30, 20);

        [Test]
        public void SmallParty_RunsThreeRounds()
        {
            Assert.AreEqual(3, For(3).Rounds);
            Assert.AreEqual(3, For(6).Rounds);
        }

        [Test]
        public void LargeParty_DropsToTwoRounds()
        {
            Assert.AreEqual(2, For(7).Rounds);
            Assert.AreEqual(2, For(8).Rounds);
        }

        [Test]
        public void SecondsPerPlayer_IsRoundsTimesAverage()
        {
            Assert.AreEqual(90, For(3).SecondsPerPlayer);
            Assert.AreEqual(60, For(8).SecondsPerPlayer);
        }

        [Test]
        public void MatchLengthStaysUnderTenMinutes()
        {
            for (var players = 3; players <= 8; players++)
            {
                Assert.LessOrEqual(For(players).EstimatedMatchSeconds, 600, $"{players} players");
            }
        }

        [Test]
        public void BudgetIsAlwaysFeasible()
        {
            for (var players = 3; players <= 8; players++)
            {
                var pacing = For(players);
                Assert.GreaterOrEqual(pacing.SecondsPerPlayer, pacing.Rounds * pacing.MinSeconds);
                Assert.LessOrEqual(pacing.SecondsPerPlayer, pacing.Rounds * pacing.MaxSeconds);
            }
        }

        [Test]
        public void BudgetLeavesRoomForVariance()
        {
            for (var players = 3; players <= 8; players++)
            {
                var pacing = For(players);
                Assert.Greater(pacing.SecondsPerPlayer, pacing.Rounds * pacing.MinSeconds,
                    $"{players} players would force every turn to the floor");
            }
        }

        [Test]
        public void RejectsAverageAtOrBelowMinimum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MatchPacing.ForPlayers(3, 20, 20));
        }

        [Test]
        public void RejectsEmptyParty()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MatchPacing.ForPlayers(0, 30, 20));
        }
    }
}
