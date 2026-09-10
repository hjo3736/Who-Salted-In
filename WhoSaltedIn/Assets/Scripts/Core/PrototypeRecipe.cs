namespace Game.Core
{
    public static class PrototypeRecipe
    {
        public const string ChopOnion = "chop_onion";
        public const string AddOnion = "add_onion";
        public const string FillJug = "fill_jug";
        public const string PourWater = "pour_water";
        public const string TurnOnHeat = "turn_on_heat";
        public const string AddSalt = "add_salt";

        public static readonly string[] SaltReadings =
        {
            "간이 약한 것 같다",
            "간이 맞는 것 같다",
            "간이 맞는 것 같다",
            "짠 것 같다"
        };

        public static Recipe Create() => new Recipe(new[]
        {
            new RecipeStep(ChopOnion, true, Accumulation.Idempotent, 1, null, "파 썰기", 6f, 3f,
                null, KitchenItems.Knife, null, false,
                new[] { KitchenItems.Board, KitchenItems.Onion }, KitchenItems.ChoppedOnion),

            new RecipeStep(AddOnion, true, Accumulation.Cumulative, 1, null, "썬 파 넣기", 2f, 3f,
                new[] { ChopOnion }, KitchenItems.ChoppedOnion, null, true,
                new[] { KitchenItems.Pot }),

            new RecipeStep(FillJug, true, Accumulation.Idempotent, 1, null, "물 받기", 3f, 3f,
                null, KitchenItems.WaterJug, KitchenItems.FullWaterJug),

            new RecipeStep(PourWater, true, Accumulation.Cumulative, 1, null, "물 붓기", 4f, 3f,
                new[] { FillJug }, KitchenItems.FullWaterJug, KitchenItems.WaterJug, false,
                new[] { KitchenItems.Pot }),

            new RecipeStep(TurnOnHeat, false, Accumulation.Idempotent, 1, null, "불 켜기", 1f, 3f,
                null, null, null, false,
                new[] { KitchenItems.Pot }),

            new RecipeStep(AddSalt, false, Accumulation.Cumulative, 1, SaltReadings, "소금 넣기", 1f, 3f,
                null, KitchenItems.SaltShaker, null, false,
                new[] { KitchenItems.Pot }, null, KitchenItems.Ladle)
        });
    }
}
