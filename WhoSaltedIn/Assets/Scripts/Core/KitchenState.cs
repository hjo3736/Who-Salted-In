namespace Game.Core
{
    public sealed class KitchenState
    {
    }

    public interface IKitchenEvent
    {
        void Apply(KitchenState state);
    }
}
