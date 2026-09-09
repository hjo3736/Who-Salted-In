using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class KitchenReport
    {
        public IReadOnlyList<string> Duplicated { get; }

        public IReadOnlyList<string> Missing { get; }

        KitchenReport(IReadOnlyList<string> duplicated, IReadOnlyList<string> missing)
        {
            Duplicated = duplicated;
            Missing = missing;
        }

        public bool IsClean => Duplicated.Count == 0 && Missing.Count == 0;

        public static KitchenReport Of(Recipe recipe, KitchenState kitchen)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (kitchen == null)
            {
                throw new ArgumentNullException(nameof(kitchen));
            }

            var duplicated = new List<string>();
            var missing = new List<string>();

            foreach (var step in recipe.Steps)
            {
                var count = kitchen.CountOf(step.Id);

                if (count < step.RequiredCount)
                {
                    missing.Add(step.Id);
                }
                else if (step.Accumulation == Accumulation.Cumulative && count > step.RequiredCount)
                {
                    duplicated.Add(step.Id);
                }
            }

            return new KitchenReport(duplicated, missing);
        }
    }
}
