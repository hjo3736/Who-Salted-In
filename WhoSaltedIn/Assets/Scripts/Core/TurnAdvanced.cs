namespace Game.Core
{
    public sealed class TurnAdvanced : IGameEvent
    {
        public int TurnIndex { get; }

        public TurnAdvanced(int turnIndex)
        {
            TurnIndex = turnIndex;
        }

        public void Apply(MatchState state) => state.SetTurnIndex(TurnIndex);
    }
}
