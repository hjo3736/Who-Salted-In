using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity
{
    public sealed class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> all = new List<Interactable>();

        public static IReadOnlyList<Interactable> All => all;

        [SerializeField] string label = "무언가";
        [SerializeField] int[] performSteps = new int[0];
        [SerializeField] int tasteStep = -1;
        [SerializeField] float nearbyRadius = 1.1f;

        void Awake()
        {
            all.Add(this);
        }

        void OnDestroy()
        {
            all.Remove(this);
        }

        public static Interactable Covering(Vector3 point)
        {
            foreach (var station in all)
            {
                if ((station.transform.position - point).sqrMagnitude
                    <= station.nearbyRadius * station.nearbyRadius)
                {
                    return station;
                }
            }

            return null;
        }

        public string Label => label;

        public IReadOnlyList<int> PerformSteps => performSteps;

        public int TasteStep => tasteStep;

        public bool CanTaste => tasteStep >= 0;

        public float NearbyRadius => nearbyRadius;

        public Holdable FindNearby(string itemId)
        {
            foreach (var item in Holdable.All)
            {
                if (item.Held || !item.gameObject.activeInHierarchy || item.ItemId != itemId)
                {
                    continue;
                }

                if ((item.transform.position - transform.position).sqrMagnitude <= nearbyRadius * nearbyRadius)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
