using UnityEngine;

namespace Game.Unity
{
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField] string label = "무언가";
        [SerializeField] int performStep = -1;
        [SerializeField] int tasteStep = -1;

        public string Label => label;

        public int PerformStep => performStep;

        public int TasteStep => tasteStep;

        public bool CanPerform => performStep >= 0;

        public bool CanTaste => tasteStep >= 0;
    }
}
