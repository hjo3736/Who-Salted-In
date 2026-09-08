using Game.Core;

namespace Game.Net
{
    public static class NetBootstrap
    {
        public static void ApplyLocal(KitchenState state, IKitchenEvent e) => e.Apply(state);
    }
}
