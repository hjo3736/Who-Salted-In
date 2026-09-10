using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    public static class ItemColors
    {
        public static Color Of(string itemId)
        {
            switch (itemId)
            {
                case KitchenItems.Onion: return new Color(0.85f, 0.95f, 0.6f);
                case KitchenItems.ChoppedOnion: return new Color(0.55f, 0.85f, 0.4f);
                case KitchenItems.Shallot: return new Color(0.85f, 0.7f, 0.5f);
                case KitchenItems.Rice: return new Color(0.95f, 0.94f, 0.88f);
                case KitchenItems.Pot: return new Color(0.5f, 0.5f, 0.55f);
                case KitchenItems.Board: return new Color(0.72f, 0.55f, 0.35f);
                case KitchenItems.Knife: return new Color(0.8f, 0.82f, 0.85f);
                case KitchenItems.Ladle: return new Color(0.75f, 0.7f, 0.5f);
                case KitchenItems.WaterJug: return new Color(0.4f, 0.65f, 0.88f);
                case KitchenItems.FullWaterJug: return new Color(0.25f, 0.5f, 0.95f);
                case KitchenItems.SaltShaker: return new Color(0.95f, 0.95f, 0.92f);
                case KitchenItems.SugarJar: return new Color(0.9f, 0.86f, 0.78f);
                case KitchenItems.SoySauce: return new Color(0.3f, 0.2f, 0.15f);
                default: return Color.magenta;
            }
        }
    }
}
