using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity
{
    public sealed class Slot : MonoBehaviour
    {
        static readonly List<Slot> all = new List<Slot>();

        public static IReadOnlyList<Slot> All => all;

        [SerializeField] string[] accepts = new string[0];

        public Holdable Occupant { get; private set; }

        public bool IsFree => Occupant == null;

        void Awake()
        {
            all.Add(this);
        }

        void OnDestroy()
        {
            all.Remove(this);
        }

        public bool Accepts(string itemId)
        {
            foreach (var allowed in accepts)
            {
                if (allowed == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        public void Take(Holdable item)
        {
            Occupant = item;
            item.PlaceInSlot(this);
        }

        public void Clear(Holdable item)
        {
            if (Occupant == item)
            {
                Occupant = null;
            }
        }

        public static Slot FreeSlotFor(Transform station, string itemId)
        {
            foreach (var slot in all)
            {
                if (slot.IsFree && slot.Accepts(itemId) && slot.transform.IsChildOf(station))
                {
                    return slot;
                }
            }

            return null;
        }

        public static void ResnapNearby()
        {
            foreach (var slot in all)
            {
                if (!slot.IsFree)
                {
                    continue;
                }

                foreach (var item in Holdable.All)
                {
                    if (item.Held || !item.gameObject.activeInHierarchy || !slot.Accepts(item.ItemId))
                    {
                        continue;
                    }

                    if ((item.transform.position - slot.transform.position).sqrMagnitude < 0.09f)
                    {
                        slot.Take(item);
                        break;
                    }
                }
            }
        }
    }
}
