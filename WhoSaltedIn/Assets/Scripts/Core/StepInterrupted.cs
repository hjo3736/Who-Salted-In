using System;

namespace Game.Core
{
    public sealed class StepInterrupted : IGameEvent
    {
        public ulong Player { get; }

        public string StepId { get; }

        public float Progress { get; }

        public StepInterrupted(ulong player, string stepId, float progress)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new ArgumentException("Step id is required.", nameof(stepId));
            }

            Player = player;
            StepId = stepId;
            Progress = progress;
        }

        public void Apply(MatchState state) => state.Kitchen.SetProgress(StepId, Progress);
    }
}
