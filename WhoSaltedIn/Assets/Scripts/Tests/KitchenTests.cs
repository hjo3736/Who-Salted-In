using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class KitchenTests
    {
        static readonly ulong[] Order = { 10, 20, 30 };
        static readonly int[] Seconds = { 30, 30, 30 };

        static MatchState Started()
        {
            var state = new MatchState();
            state.Apply(new MatchStarted(Order, Seconds));
            return state;
        }

        static string Read(Recipe recipe, MatchState state, string stepId) =>
            recipe[stepId].ReadingFor(state.Kitchen.CountOf(stepId));

        [Test]
        public void Salt_ReadsTheSameAtOnceAndTwice()
        {
            var salt = PrototypeRecipe.Create()[PrototypeRecipe.AddSalt];

            Assert.AreEqual("간이 약한 것 같다", salt.ReadingFor(0));
            Assert.AreEqual("간이 맞는 것 같다", salt.ReadingFor(1));
            Assert.AreEqual("간이 맞는 것 같다", salt.ReadingFor(2));
            Assert.AreEqual("짠 것 같다", salt.ReadingFor(3));
        }

        [Test]
        public void Salt_ReadingSaturatesBeyondLastBucket()
        {
            var salt = PrototypeRecipe.Create()[PrototypeRecipe.AddSalt];
            Assert.AreEqual("짠 것 같다", salt.ReadingFor(9));
        }

        [Test]
        public void ObservableStep_ReadsExactCount()
        {
            var recipe = PrototypeRecipe.Create();
            var state = Started();

            state.Apply(new StepPerformed(10, PrototypeRecipe.PourWater));
            state.Apply(new StepPerformed(20, PrototypeRecipe.PourWater));

            Assert.AreEqual("2", Read(recipe, state, PrototypeRecipe.PourWater));
        }

        [Test]
        public void HiddenIdempotentStep_CannotBeObserved()
        {
            var heat = PrototypeRecipe.Create()[PrototypeRecipe.TurnOnHeat];

            Assert.IsFalse(heat.CanBeObserved);
            Assert.IsNull(heat.ReadingFor(0));
            Assert.IsNull(heat.ReadingFor(1));
        }

        [Test]
        public void TwoCooksReadingTheSameBucket_ProduceDuplicateSalt()
        {
            var recipe = PrototypeRecipe.Create();
            var state = Started();

            state.Apply(new StepPerformed(10, PrototypeRecipe.ChopOnion));
            state.Apply(new StepPerformed(10, PrototypeRecipe.PourWater));
            state.Apply(new StepPerformed(10, PrototypeRecipe.TurnOnHeat));

            var firstReading = Read(recipe, state, PrototypeRecipe.AddSalt);
            state.Apply(new Observed(10, PrototypeRecipe.AddSalt, firstReading));
            state.Apply(new StepPerformed(10, PrototypeRecipe.AddSalt));

            var secondReading = Read(recipe, state, PrototypeRecipe.AddSalt);
            state.Apply(new Observed(20, PrototypeRecipe.AddSalt, secondReading));
            state.Apply(new StepPerformed(20, PrototypeRecipe.AddSalt));

            Assert.AreEqual("간이 약한 것 같다", firstReading);
            Assert.AreEqual("간이 맞는 것 같다", secondReading);
            Assert.AreEqual(2, state.Kitchen.CountOf(PrototypeRecipe.AddSalt));

            var report = KitchenReport.Of(recipe, state.Kitchen);
            CollectionAssert.Contains(report.Duplicated, PrototypeRecipe.AddSalt);
        }

        [Test]
        public void SecondCookIsNeverToldTheTruthIsAlreadyReached()
        {
            var salt = PrototypeRecipe.Create()[PrototypeRecipe.AddSalt];
            Assert.AreEqual(salt.ReadingFor(1), salt.ReadingFor(2));
        }

        [Test]
        public void Report_FlagsMissingSteps()
        {
            var recipe = PrototypeRecipe.Create();
            var state = Started();

            state.Apply(new StepPerformed(10, PrototypeRecipe.ChopOnion));

            var report = KitchenReport.Of(recipe, state.Kitchen);

            CollectionAssert.DoesNotContain(report.Missing, PrototypeRecipe.ChopOnion);
            CollectionAssert.Contains(report.Missing, PrototypeRecipe.AddSalt);
            CollectionAssert.Contains(report.Missing, PrototypeRecipe.TurnOnHeat);
            Assert.IsFalse(report.IsClean);
        }

        [Test]
        public void Report_IgnoresRepeatsOfIdempotentSteps()
        {
            var recipe = PrototypeRecipe.Create();
            var state = Started();

            foreach (var step in recipe.Steps)
            {
                state.Apply(new StepPerformed(10, step.Id));
            }

            state.Apply(new StepPerformed(20, PrototypeRecipe.ChopOnion));
            state.Apply(new StepPerformed(30, PrototypeRecipe.TurnOnHeat));

            var report = KitchenReport.Of(recipe, state.Kitchen);

            Assert.IsTrue(report.IsClean);
        }

        [Test]
        public void ReplayingEvents_RebuildsTheSameKitchen()
        {
            var events = new List<IGameEvent>
            {
                new MatchStarted(Order, Seconds),
                new StepPerformed(10, PrototypeRecipe.ChopOnion),
                new Observed(20, PrototypeRecipe.AddSalt, "간이 약한 것 같다"),
                new StepPerformed(20, PrototypeRecipe.AddSalt),
                new StepPerformed(30, PrototypeRecipe.AddSalt)
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

            CollectionAssert.AreEquivalent(live.Kitchen.Counts, replay.Kitchen.Counts);
            Assert.AreEqual(2, replay.Kitchen.CountOf(PrototypeRecipe.AddSalt));
        }

        [Test]
        public void Observed_DoesNotChangeTheKitchen()
        {
            var state = Started();

            state.Apply(new Observed(10, PrototypeRecipe.AddSalt, "간이 약한 것 같다"));

            Assert.AreEqual(0, state.Kitchen.CountOf(PrototypeRecipe.AddSalt));
        }
    }
}
