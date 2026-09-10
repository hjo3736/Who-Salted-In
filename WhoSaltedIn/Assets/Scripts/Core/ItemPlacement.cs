namespace Game.Core
{
    public readonly struct ItemPlacement
    {
        public readonly string Key;
        public readonly string ItemId;
        public readonly bool Present;
        public readonly float PosX, PosY, PosZ;
        public readonly float RotX, RotY, RotZ, RotW;

        public ItemPlacement(
            string key,
            string itemId,
            bool present,
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW)
        {
            Key = key;
            ItemId = itemId;
            Present = present;
            PosX = posX; PosY = posY; PosZ = posZ;
            RotX = rotX; RotY = rotY; RotZ = rotZ; RotW = rotW;
        }
    }
}
