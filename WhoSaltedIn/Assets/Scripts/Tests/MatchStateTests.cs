using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class MatchStateTests
    {
        static readonly ulong[] Order = { 10, 20, 30 };
        static readonly int[] Seconds = { 20, 30, 40 };

        static MatchState Started()
        {
            var state = new MatchState();
            state.Apply(new MatchStarted(Order, Seconds));
            return state;
        }

        [Test]
        public void NewMatch_HasNoTurn()
        {
            var state = new MatchState();
            Assert.AreEqual(MatchState.NotStarted, state.TurnIndex);
            Assert.IsFalse(state.IsRunning);
            Assert.IsNull(state.CurrentPlayer);
        }

        [Test]
        public void MatchStarted_SetsScheduleButNoTurnYet()
        {
            var state = Started();
            CollectionAssert.AreEqual(Order, state.Order);
            CollectionAssert.AreEqual(Seconds, state.Seconds);
            Assert.AreEqual(MatchState.NotStarted, state.TurnIndex);
            Assert.IsNull(state.CurrentTurnSeconds);
        }

        [Test]
        public void CurrentTurnSeconds_FollowsTurnIndex()
        {
            var state = Started();

            state.Apply(new TurnAdvanced(0));
            Assert.AreEqual(20, state.CurrentTurnSeconds);

            state.Apply(new TurnAdvanced(2));
            Assert.AreEqual(40, state.CurrentTurnSeconds);

            state.Apply(new TurnAdvanced(Order.Length));
            Assert.IsNull(state.CurrentTurnSeconds);
        }

        [Test]
        public void MatchStarted_RejectsMismatchedSecondsLength()
        {
            Assert.Throws<ArgumentException>(
                () => new MatchState().Apply(new MatchStarted(Order, new[] { 20, 30 })));
        }

        [Test]
        public void TurnAdvanced_MovesCurrentPlayer()
        {
            var state = Started();

            state.Apply(new TurnAdvanced(0));
            Assert.AreEqual(10ul, state.CurrentPlayer);
            Assert.IsTrue(state.IsCurrentPlayer(10));
            Assert.IsFalse(state.IsCurrentPlayer(20));

            state.Apply(new TurnAdvanced(2));
            Assert.AreEqual(30ul, state.CurrentPlayer);
        }

        [Test]
        public void TurnAdvanced_PastLastTurn_FinishesMatch()
        {
            var state = Started();
            state.Apply(new TurnAdvanced(Order.Length));

            Assert.IsTrue(state.IsFinished);
            Assert.IsFalse(state.IsRunning);
            Assert.IsNull(state.CurrentPlayer);
        }

        [Test]
        public void ReplayingSameEvents_RebuildsSameState()
        {
            var events = new List<IGameEvent>
            {
                new MatchStarted(Order, Seconds),
                new TurnAdvanced(0),
                new TurnAdvanced(1),
                new TurnAdvanced(2)
            };

            var live = new MatchState();
            var replay = new MatchState();

            foreach (var e in events)
            {
                live.Apply(e);
            }

            foreach (var e in events)
            {
                replay.Apply(e);
            }

            Assert.AreEqual(live.TurnIndex, replay.TurnIndex);
            Assert.AreEqual(live.CurrentPlayer, replay.CurrentPlayer);
            CollectionAssert.AreEqual(live.Order, replay.Order);
        }

        [Test]
        public void ApplyingSameTurnTwice_IsIdempotent()
        {
            var state = Started();

            state.Apply(new TurnAdvanced(1));
            state.Apply(new TurnAdvanced(1));

            Assert.AreEqual(1, state.TurnIndex);
            Assert.AreEqual(20ul, state.CurrentPlayer);
        }

        [Test]
        public void TurnAdvanced_BeforeMatchStarted_Throws()
        {
            var state = new MatchState();
            Assert.Throws<InvalidOperationException>(() => state.Apply(new TurnAdvanced(0)));
        }

        [Test]
        public void TurnAdvanced_OutOfRange_Throws()
        {
            var state = Started();
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Apply(new TurnAdvanced(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Apply(new TurnAdvanced(Order.Length + 1)));
        }

        [Test]
        public void Apply_NullEvent_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchState().Apply(null));
        }
    }
}
