using System;
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
        public void DistinctReadings_CollapseRepeatedLabels()
        {
            var salt = PrototypeRecipe.Create()[PrototypeRecipe.AddSalt];

            CollectionAssert.AreEqual(
                new[] { "간이 약한 것 같다", "간이 맞는 것 같다", "짠 것 같다" },
                salt.DistinctReadings);
        }

        [Test]
        public void ReadingIndex_LeaksNothingBetweenOnceAndTwice()
        {
            var salt = PrototypeRecipe.Create()[PrototypeRecipe.AddSalt];

            Assert.AreEqual(0, salt.ReadingIndexFor(0));
            Assert.AreEqual(1, salt.ReadingIndexFor(1));
            Assert.AreEqual(1, salt.ReadingIndexFor(2));
            Assert.AreEqual(2, salt.ReadingIndexFor(3));
        }

        [Test]
        public void ReadingIndex_IsMinusOneWhenUnobservable()
        {
            var recipe = PrototypeRecipe.Create();

            Assert.AreEqual(-1, recipe[PrototypeRecipe.TurnOnHeat].ReadingIndexFor(0));
            Assert.AreEqual(-1, recipe[PrototypeRecipe.ChopOnion].ReadingIndexFor(1));
        }

        [Test]
        public void Log_RecordsEveryAppliedEvent()
        {
            var state = Started();

            state.Apply(new Observed(10, PrototypeRecipe.AddSalt, "간이 약한 것 같다"));
            state.Apply(new StepPerformed(10, PrototypeRecipe.AddSalt));

            Assert.AreEqual(3, state.Log.Count);
            Assert.IsInstanceOf<MatchStarted>(state.Log[0]);
            Assert.IsInstanceOf<Observed>(state.Log[1]);
            Assert.IsInstanceOf<StepPerformed>(state.Log[2]);
        }

        [Test]
        public void Log_DoesNotRecordRejectedEvents()
        {
            var state = new MatchState();

            Assert.Throws<InvalidOperationException>(() => state.Apply(new TurnAdvanced(0)));
            Assert.AreEqual(0, state.Log.Count);
        }

        [Test]
        public void Interrupt_LeavesPartialProgressForTheNextCook()
        {
            var state = Started();

            state.Apply(new StepInterrupted(10, PrototypeRecipe.ChopOnion, 3.5f));

            Assert.AreEqual(3.5f, state.Kitchen.ProgressOf(PrototypeRecipe.ChopOnion));
            Assert.AreEqual(0, state.Kitchen.CountOf(PrototypeRecipe.ChopOnion));

            var report = KitchenReport.Of(PrototypeRecipe.Create(), state.Kitchen);
            CollectionAssert.Contains(report.Missing, PrototypeRecipe.ChopOnion);
        }

        [Test]
        public void FinishingAStep_ClearsItsPartialProgress()
        {
            var state = Started();

            state.Apply(new StepInterrupted(10, PrototypeRecipe.ChopOnion, 3.5f));
            state.Apply(new StepPerformed(20, PrototypeRecipe.ChopOnion));

            Assert.AreEqual(0f, state.Kitchen.ProgressOf(PrototypeRecipe.ChopOnion));
            Assert.AreEqual(1, state.Kitchen.CountOf(PrototypeRecipe.ChopOnion));
        }

        [Test]
        public void SaltIsCheaperThanCheckingIt()
        {
            var recipe = PrototypeRecipe.Create();
            var salt = recipe[PrototypeRecipe.AddSalt];

            Assert.Less(salt.WorkSeconds, salt.ObserveSeconds);
        }

        [Test]
        public void ObservableStepsTakeLongEnoughToBeInterrupted()
        {
            var recipe = PrototypeRecipe.Create();

            Assert.Greater(recipe[PrototypeRecipe.ChopOnion].WorkSeconds, 3f);
        }

        [Test]
        public void Progress_RejectsNegativeValues()
        {
            var state = Started();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => state.Apply(new StepInterrupted(10, PrototypeRecipe.ChopOnion, -1f)));
        }

        [Test]
        public void AddingOnion_RequiresChoppingFirst()
        {
            var recipe = PrototypeRecipe.Create();
            var state = Started();

            Assert.AreEqual(
                PrototypeRecipe.ChopOnion,
                recipe.UnmetRequirement(PrototypeRecipe.AddOnion, state.Kitchen));

            state.Apply(new StepPerformed(10, PrototypeRecipe.ChopOnion));

            Assert.IsNull(recipe.UnmetRequirement(PrototypeRecipe.AddOnion, state.Kitchen));
        }

        [Test]
        public void HalfChoppedOnion_DoesNotSatisfyTheRequirement()
        {
            var recipe = PrototypeRecipe.Create();
            var state = Started();

            state.Apply(new StepInterrupted(10, PrototypeRecipe.ChopOnion, 5.9f));

            Assert.AreEqual(
                PrototypeRecipe.ChopOnion,
                recipe.UnmetRequirement(PrototypeRecipe.AddOnion, state.Kitchen));
        }

        [Test]
        public void SaltHasNoRequirements()
        {
            var recipe = PrototypeRecipe.Create();

            CollectionAssert.IsEmpty(recipe[PrototypeRecipe.AddSalt].Requires);
        }

        [Test]
        public void ChoppingNeedsAKnifeAndABoardAndAnOnion()
        {
            var chop = PrototypeRecipe.Create()[PrototypeRecipe.ChopOnion];

            Assert.AreEqual(KitchenItems.Knife, chop.RequiredItem);
            CollectionAssert.AreEqual(
                new[] { KitchenItems.Board, KitchenItems.Onion },
                chop.RequiredNearby);
            Assert.AreEqual(KitchenItems.ChoppedOnion, chop.NearbyBecomes);
        }

        [Test]
        public void EveryPotStepNeedsThePotInPlace()
        {
            var recipe = PrototypeRecipe.Create();

            foreach (var id in new[]
                     {
                         PrototypeRecipe.AddOnion,
                         PrototypeRecipe.PourWater,
                         PrototypeRecipe.TurnOnHeat,
                         PrototypeRecipe.AddSalt
                     })
            {
                CollectionAssert.Contains(recipe[id].RequiredNearby, KitchenItems.Pot, id);
            }
        }

        [Test]
        public void TastingNeedsALadleButSaltingDoesNot()
        {
            var salt = PrototypeRecipe.Create()[PrototypeRecipe.AddSalt];

            Assert.AreEqual(KitchenItems.Ladle, salt.ObserveItem);
            Assert.AreEqual(KitchenItems.SaltShaker, salt.RequiredItem);
        }

        [Test]
        public void PouringWaterNeedsAFilledJug()
        {
            var recipe = PrototypeRecipe.Create();

            Assert.AreEqual(KitchenItems.FullWaterJug, recipe[PrototypeRecipe.PourWater].RequiredItem);
            Assert.AreEqual(KitchenItems.FullWaterJug, recipe[PrototypeRecipe.FillJug].ProducesItem);
            CollectionAssert.Contains(recipe[PrototypeRecipe.PourWater].Requires, PrototypeRecipe.FillJug);
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
