using System;

namespace Game.Core
{
    public sealed class StepPerformed : IGameEvent
    {
        public ulong Player { get; }

        public string StepId { get; }

        public StepPerformed(ulong player, string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new ArgumentException("Step id is required.", nameof(stepId));
            }

            Player = player;
            StepId = stepId;
        }

        public void Apply(MatchState state) => state.Kitchen.Increment(StepId);
    }
}
