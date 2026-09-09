using Game.Core;
using UnityEngine;

namespace Game.Net
{
    public sealed class MatchRuntime : MonoBehaviour
    {
        [SerializeField] KitchenDriver kitchen;

        public MatchState State { get; } = new MatchState();

        public Recipe Recipe { get; } = PrototypeRecipe.Create();

        public void Step(int stepIndex) => kitchen.RequestStep(stepIndex);

        public void Taste(int stepIndex) => kitchen.RequestTaste(stepIndex);

        public void Look() => kitchen.RequestLook();
    }
}
