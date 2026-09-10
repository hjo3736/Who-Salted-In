namespace Game.Core
{
    public static class KitchenItems
    {
        public const string None = "";

        public const string Onion = "onion";
        public const string ChoppedOnion = "chopped_onion";
        public const string Shallot = "shallot";
        public const string Rice = "rice";

        public const string Pot = "pot";
        public const string Board = "board";
        public const string Knife = "knife";
        public const string Ladle = "ladle";
        public const string WaterJug = "water_jug";
        public const string FullWaterJug = "water_jug_full";

        public const string SaltShaker = "salt_shaker";
        public const string SugarJar = "sugar_jar";
        public const string SoySauce = "soy_sauce";

        public static readonly string[] All =
        {
            Onion,
            ChoppedOnion,
            Shallot,
            Rice,
            Pot,
            Board,
            Knife,
            Ladle,
            WaterJug,
            FullWaterJug,
            SaltShaker,
            SugarJar,
            SoySauce
        };

        public static int IndexOf(string itemId)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i] == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string NameOf(string itemId)
        {
            switch (itemId)
            {
                case Onion: return "파";
                case ChoppedOnion: return "썬 파";
                case Shallot: return "양파";
                case Rice: return "쌀";
                case Pot: return "냄비";
                case Board: return "도마";
                case Knife: return "칼";
                case Ladle: return "국자";
                case WaterJug: return "물병";
                case FullWaterJug: return "물이 든 물병";
                case SaltShaker: return "소금통";
                case SugarJar: return "설탕";
                case SoySauce: return "간장";
                default: return itemId;
            }
        }

        public static string At(int index) =>
            index >= 0 && index < All.Length ? All[index] : None;
    }
}
