using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class KitchenSettled : IGameEvent
    {
        public ulong Player { get; }

        public IReadOnlyList<ItemPlacement> Placements { get; }

        public KitchenSettled(ulong player, IReadOnlyList<ItemPlacement> placements)
        {
            Player = player;
            Placements = placements ?? throw new ArgumentNullException(nameof(placements));
        }

        public void Apply(MatchState state) => state.Kitchen.SetPlacements(Placements);
    }
}
