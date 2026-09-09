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

        void Awake()
        {
            Show(false);
        }

        void OnEnable()
        {
            if (runtime != null)
            {
                runtime.TurnChanged += Show;
            }
        }

        void OnDisable()
        {
            if (runtime != null)
            {
                runtime.TurnChanged -= Show;
            }
        }

        void Show(bool mine)
        {
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
