namespace Game.Core
{
    public interface IGameEvent
    {
        void Apply(MatchState state);
    }
}
