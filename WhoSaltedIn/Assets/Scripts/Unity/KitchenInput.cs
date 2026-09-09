using Game.Net;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity
{
    public sealed class KitchenInput : MonoBehaviour
    {
        [SerializeField] MatchRuntime runtime;

        static readonly Key[] StepKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        static readonly Key[] TasteKeys = { Key.Q, Key.W, Key.E, Key.R };

        void Start()
        {
            if (runtime == null)
            {
                return;
            }

            var steps = runtime.Recipe.Steps;

            for (var i = 0; i < steps.Count && i < StepKeys.Length; i++)
            {
                Debug.Log($"[Input] {i + 1} = {steps[i].Id} 하기, {TasteKeys[i]} = 확인");
            }

            Debug.Log("[Input] L = 둘러보기");
        }

        void Update()
        {
            if (runtime == null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[Key.L].wasPressedThisFrame)
            {
                runtime.Look();
            }

            var steps = runtime.Recipe.Steps.Count;

            for (var i = 0; i < steps && i < StepKeys.Length; i++)
            {
                if (Keyboard.current[StepKeys[i]].wasPressedThisFrame)
                {
                    runtime.Step(i);
                }

                if (Keyboard.current[TasteKeys[i]].wasPressedThisFrame)
                {
                    runtime.Taste(i);
                }
            }
        }
    }
}
