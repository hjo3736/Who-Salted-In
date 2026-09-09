using System;

namespace Game.Core
{
    public sealed class Observed : IGameEvent
    {
        public ulong Player { get; }

        public string StepId { get; }

        public string Reading { get; }

        public Observed(ulong player, string stepId, string reading)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new ArgumentException("Step id is required.", nameof(stepId));
            }

            Player = player;
            StepId = stepId;
            Reading = reading;
        }

        public void Apply(MatchState state)
        {
        }
    }
}
