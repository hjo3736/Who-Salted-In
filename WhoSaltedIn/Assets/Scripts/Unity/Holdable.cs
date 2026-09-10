using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Holdable : MonoBehaviour
    {
        static readonly List<Holdable> all = new List<Holdable>();

        public static IReadOnlyList<Holdable> All => all;

        [SerializeField] string key = "";
        [SerializeField] string itemId = "";
        [SerializeField] string label = "물건";

        Rigidbody body;
        Collider shape;
        Renderer skin;
        Transform home;

        public string Key => key;

        public string ItemId => itemId;

        public string Label => label;

        public bool Held { get; private set; }

        public Slot InSlot { get; private set; }

        public float HalfHeight => shape != null ? shape.bounds.extents.y + 0.02f : 0.12f;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            shape = GetComponentInChildren<Collider>();
            skin = GetComponentInChildren<Renderer>();
            home = transform.parent;
            all.Add(this);
        }

        void OnDestroy()
        {
            all.Remove(this);
        }

        public void Grab(Transform hand)
        {
            LeaveSlot();
            Held = true;
            body.detectCollisions = false;
            body.isKinematic = true;

            if (shape != null)
            {
                shape.enabled = false;
            }

            transform.SetParent(hand, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        public void Release(Vector3 velocity)
        {
            Held = false;
            transform.SetParent(home, true);

            if (shape != null)
            {
                shape.enabled = true;
            }

            body.isKinematic = false;
            body.detectCollisions = true;
            body.linearVelocity = velocity;
        }

        public void PlaceInSlot(Slot slot)
        {
            LeaveSlot();

            Held = false;
            InSlot = slot;

            body.detectCollisions = true;
            body.isKinematic = true;

            if (shape != null)
            {
                shape.enabled = true;
            }

            transform.SetParent(slot.transform, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        void LeaveSlot()
        {
            if (InSlot == null)
            {
                return;
            }

            InSlot.Clear(this);
            InSlot = null;
        }

        public void PlaceAt(Vector3 position)
        {
            LeaveSlot();
            Held = false;
            transform.SetParent(home, true);

            if (shape != null)
            {
                shape.enabled = true;
            }

            body.isKinematic = true;
            body.detectCollisions = true;
            transform.position = position;
            body.position = position;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void Become(string newItemId)
        {
            if (itemId == newItemId)
            {
                return;
            }

            itemId = newItemId;
            label = KitchenItems.NameOf(newItemId);

            if (skin != null)
            {
                skin.material.color = ItemColors.Of(newItemId);
            }
        }

        public void Consume()
        {
            LeaveSlot();
            Held = false;
            transform.SetParent(home, true);
            gameObject.SetActive(false);
        }

        public void RestoreTo(Vector3 position, Quaternion rotation, bool present)
        {
            LeaveSlot();
            Held = false;
            transform.SetParent(home, true);
            gameObject.SetActive(present);

            if (!present)
            {
                return;
            }

            if (shape != null)
            {
                shape.enabled = true;
            }

            body.isKinematic = true;
            body.detectCollisions = true;
            transform.SetPositionAndRotation(position, rotation);
            body.position = position;
            body.rotation = rotation;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
