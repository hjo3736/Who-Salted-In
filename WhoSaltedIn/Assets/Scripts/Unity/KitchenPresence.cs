using System.Collections.Generic;
using Game.Core;
using Game.Net;
using UnityEngine;

namespace Game.Unity
{
    public sealed class KitchenPresence : MonoBehaviour
    {
        [SerializeField] MatchRuntime runtime;
        [SerializeField] GameObject kitchenRoot;
        [SerializeField] GameObject playerRoot;
        [SerializeField] Transform entrance;
        [SerializeField] Camera lobbyCamera;

        bool inside;

        void Awake()
        {
            Show(false);
        }

        void OnEnable()
        {
            if (runtime == null)
            {
                return;
            }

            runtime.TurnChanged += Show;
            runtime.Restored += Restore;
        }

        void OnDisable()
        {
            if (runtime == null)
            {
                return;
            }

            runtime.TurnChanged -= Show;
            runtime.Restored -= Restore;
        }

        void Show(bool mine)
        {
            if (inside && !mine)
            {
                if (playerRoot != null)
                {
                    var interactor = playerRoot.GetComponentInChildren<Interactor>();

                    if (interactor != null)
                    {
                        interactor.ReleaseHeld();
                    }
                }

                Settle();
            }

            inside = mine;

            if (kitchenRoot != null)
            {
                kitchenRoot.SetActive(mine);
            }

            if (playerRoot != null)
            {
                if (mine)
                {
                    MoveToEntrance();
                }

                playerRoot.SetActive(mine);
            }

            if (lobbyCamera != null)
            {
                lobbyCamera.enabled = !mine;
            }

            Debug.Log(mine ? "[View] 주방에 들어왔다" : "[View] 로비");
        }

        void Settle()
        {
            var items = Holdable.All;
            var placements = new List<ItemPlacement>(items.Count);

            foreach (var item in items)
            {
                var t = item.transform;

                placements.Add(new ItemPlacement(
                    item.Key,
                    item.ItemId,
                    item.gameObject.activeSelf,
                    t.position.x, t.position.y, t.position.z,
                    t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w));
            }

            Debug.Log($"[Settle] {placements.Count}개 전송");
            runtime.Settle(placements);
        }

        void Restore(IReadOnlyList<ItemPlacement> placements)
        {
            var matched = 0;

            foreach (var placement in placements)
            {
                foreach (var item in Holdable.All)
                {
                    if (item.Key != placement.Key)
                    {
                        continue;
                    }

                    matched++;
                    item.Become(placement.ItemId);

                    item.RestoreTo(
                        new Vector3(placement.PosX, placement.PosY, placement.PosZ),
                        new Quaternion(placement.RotX, placement.RotY, placement.RotZ, placement.RotW),
                        placement.Present);

                    break;
                }
            }

            Debug.Log($"[Restore] {placements.Count}개 수신, {matched}개 적용, 주방 물건 {Holdable.All.Count}개");
            Slot.ResnapNearby();
        }

        void MoveToEntrance()
        {
            if (entrance == null)
            {
                return;
            }

            var controller = playerRoot.GetComponent<CharacterController>();

            if (controller != null)
            {
                controller.enabled = false;
            }

            playerRoot.transform.SetPositionAndRotation(entrance.position, entrance.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }
}
