namespace Game.Core
{
    public static class PrototypeRecipe
    {
        public const string ChopOnion = "chop_onion";
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
            new RecipeStep(ChopOnion, true, Accumulation.Idempotent, 1, null, "파 썰기"),
            new RecipeStep(PourWater, true, Accumulation.Cumulative, 1, null, "물 붓기"),
            new RecipeStep(TurnOnHeat, false, Accumulation.Idempotent, 1, null, "불 켜기"),
            new RecipeStep(AddSalt, false, Accumulation.Cumulative, 1, SaltReadings, "소금 넣기")
        });
    }
}
