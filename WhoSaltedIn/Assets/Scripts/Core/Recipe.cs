using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class Recipe
    {
        readonly Dictionary<string, RecipeStep> byId;

        public IReadOnlyList<RecipeStep> Steps { get; }

        public Recipe(IReadOnlyList<RecipeStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            if (steps.Count == 0)
            {
                throw new ArgumentException("A recipe needs at least one step.", nameof(steps));
            }

            byId = new Dictionary<string, RecipeStep>(steps.Count);

            foreach (var step in steps)
            {
                if (byId.ContainsKey(step.Id))
                {
                    throw new ArgumentException($"Duplicate step id '{step.Id}'.", nameof(steps));
                }

                byId.Add(step.Id, step);
            }

            Steps = steps;
        }

        public RecipeStep this[string stepId]
        {
            get
            {
                if (!byId.TryGetValue(stepId, out var step))
                {
                    throw new KeyNotFoundException($"Recipe has no step '{stepId}'.");
                }

                return step;
            }
        }

        public bool Contains(string stepId) => byId.ContainsKey(stepId);

        public int IndexOf(string stepId)
        {
            for (var i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].Id == stepId)
                {
                    return i;
                }
            }

            return -1;
        }

        public string UnmetRequirement(string stepId, KitchenState kitchen)
        {
            var step = this[stepId];

            foreach (var required in step.Requires)
            {
                if (kitchen.CountOf(required) < this[required].RequiredCount)
                {
                    return required;
                }
            }

            return null;
        }
    }
}
