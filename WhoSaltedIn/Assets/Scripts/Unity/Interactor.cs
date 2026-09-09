using Game.Net;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity
{
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] MatchRuntime runtime;
        [SerializeField] Transform head;
        [SerializeField] float reach = 2.4f;

        Interactable aimed;
        GUIStyle style;

        void Update()
        {
            if (head == null)
            {
                return;
            }

            aimed = Physics.Raycast(head.position, head.forward, out var hit, reach)
                ? hit.collider.GetComponentInParent<Interactable>()
                : null;

            if (aimed == null || runtime == null || Keyboard.current == null)
            {
                return;
            }

            if (aimed.CanPerform && Keyboard.current[Key.F].wasPressedThisFrame)
            {
                runtime.Step(aimed.PerformStep);
            }

            if (aimed.CanTaste && Keyboard.current[Key.Q].wasPressedThisFrame)
            {
                runtime.Taste(aimed.TasteStep);
            }
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            GUI.Label(new Rect(centre.x - 10f, centre.y - 12f, 20f, 24f), aimed == null ? "·" : "+", style);

            if (aimed == null)
            {
                return;
            }

            var hint = aimed.Label;

            if (aimed.CanPerform)
            {
                hint += "   [F] 하기";
            }

            if (aimed.CanTaste)
            {
                hint += "   [Q] 확인";
            }

            GUI.Label(new Rect(centre.x - 250f, centre.y + 24f, 500f, 28f), hint, style);
        }
    }
}
