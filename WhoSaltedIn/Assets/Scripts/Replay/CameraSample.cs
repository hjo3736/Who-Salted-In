namespace Game.Replay
{
    public readonly struct CameraSample
    {
        public readonly float Time;
        public readonly float PosX, PosY, PosZ;
        public readonly float RotX, RotY, RotZ, RotW;

        public CameraSample(
            float time,
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ, float rotW)
        {
            Time = time;
            PosX = posX; PosY = posY; PosZ = posZ;
            RotX = rotX; RotY = rotY; RotZ = rotZ; RotW = rotW;
        }
    }
}
