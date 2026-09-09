using System;

namespace Game.Core
{
    public readonly struct MatchPacing
    {
        public const int RoundsWhenSmallParty = 3;
        public const int RoundsWhenLargeParty = 2;
        public const int LargePartyThreshold = 7;

        public int PlayerCount { get; }
        public int Rounds { get; }
        public int SecondsPerPlayer { get; }
        public int MinSeconds { get; }
        public int MaxSeconds { get; }

        MatchPacing(int playerCount, int rounds, int secondsPerPlayer, int minSeconds, int maxSeconds)
        {
            PlayerCount = playerCount;
            Rounds = rounds;
            SecondsPerPlayer = secondsPerPlayer;
            MinSeconds = minSeconds;
            MaxSeconds = maxSeconds;
        }

        public int TotalTurns => PlayerCount * Rounds;

        public int EstimatedMatchSeconds => PlayerCount * SecondsPerPlayer;

        public int EstimatedWaitSeconds => (PlayerCount - 1) * (SecondsPerPlayer / Rounds);

        public static MatchPacing ForPlayers(int playerCount, int averageTurnSeconds, int minSeconds)
        {
            if (playerCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount,
                    "At least one player is required.");
            }

            if (minSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minSeconds), minSeconds,
                    "Minimum turn length must be positive.");
            }

            if (averageTurnSeconds <= minSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(averageTurnSeconds), averageTurnSeconds,
                    $"Average turn must exceed the minimum of {minSeconds}.");
            }

            var rounds = playerCount < LargePartyThreshold
                ? RoundsWhenSmallParty
                : RoundsWhenLargeParty;

            var maxSeconds = averageTurnSeconds + (averageTurnSeconds - minSeconds) * 3 / 2;

            return new MatchPacing(
                playerCount,
                rounds,
                rounds * averageTurnSeconds,
                minSeconds,
                maxSeconds);
        }
    }
}
