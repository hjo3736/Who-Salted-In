using System.Collections.Generic;
using Game.Core;
using Game.Net;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity
{
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] MatchRuntime runtime;
        [SerializeField] Transform head;
        [SerializeField] Transform hand;
        [SerializeField] float reach = 2.4f;
        [SerializeField] float placeDistance = 1.6f;

        Holdable held;
        Holdable aimedItem;
        Interactable aimedStation;
        Interactable busyOn;
        IReadOnlyList<int> counts;
        bool tasting;
        GUIStyle style;

        void OnEnable()
        {
            if (runtime == null)
            {
                return;
            }

            if (hand == null)
            {
                Debug.LogError("[Interactor] Hand 가 비어 있다. 물건을 들어도 따라오지 않는다");
            }

            runtime.WorkCompleted += Finished;
            runtime.Looked += Remember;
            runtime.Look();
            runtime.RequestRestore();
        }

        public void ReleaseHeld()
        {
            Stop();
            Drop();
        }

        void OnDisable()
        {
            ReleaseHeld();

            if (runtime != null)
            {
                runtime.WorkCompleted -= Finished;
                runtime.Looked -= Remember;
            }
        }

        void Update()
        {
            if (head == null || runtime == null || Keyboard.current == null)
            {
                return;
            }

            Aim();

            if (Keyboard.current[Key.V].wasPressedThisFrame)
            {
                Diagnose();
            }

            if (busyOn != null)
            {
                if (aimedStation != busyOn || !Keyboard.current[tasting ? Key.Q : Key.F].isPressed)
                {
                    Stop();
                }

                return;
            }

            if (CanTasteHere() && Keyboard.current[Key.Q].wasPressedThisFrame)
            {
                Begin(aimedStation, aimedStation.TasteStep, true);
                return;
            }

            if (!Keyboard.current[Key.F].wasPressedThisFrame)
            {
                return;
            }

            if (held == null)
            {
                if (aimedItem != null)
                {
                    held = aimedItem;
                    held.Grab(hand);
                    return;
                }
            }

            var match = MatchingStep();

            if (match >= 0)
            {
                Begin(aimedStation, match, false);
                return;
            }

            if (held == null)
            {
                return;
            }

            var slot = FreeSlot();

            if (slot != null)
            {
                slot.Take(held);
                held = null;
                return;
            }

            Drop();
        }

        void Aim()
        {
            aimedItem = null;
            aimedStation = null;

            if (!Physics.Raycast(head.position, head.forward, out var hit, reach))
            {
                return;
            }

            var item = hit.collider.GetComponentInParent<Holdable>();

            if (item != null && !item.Held)
            {
                aimedItem = item;
            }

            aimedStation = hit.collider.GetComponentInParent<Interactable>();

            if (aimedStation == null)
            {
                aimedStation = Interactable.Covering(hit.point);
            }
        }

        int MatchingStep()
        {
            if (aimedStation == null)
            {
                return -1;
            }

            var inHand = held == null ? KitchenItems.None : held.ItemId;

            foreach (var index in aimedStation.PerformSteps)
            {
                var step = runtime.Recipe.Steps[index];

                if (step.RequiredItem == inHand && HasNearby(step))
                {
                    return index;
                }
            }

            return -1;
        }

        bool HasNearby(RecipeStep step)
        {
            foreach (var itemId in step.RequiredNearby)
            {
                if (aimedStation.FindNearby(itemId) == null)
                {
                    return false;
                }
            }

            return true;
        }

        bool CanTasteHere()
        {
            if (aimedStation == null || !aimedStation.CanTaste)
            {
                return false;
            }

            var step = runtime.Recipe.Steps[aimedStation.TasteStep];
            var inHand = held == null ? KitchenItems.None : held.ItemId;

            return step.ObserveItem == inHand && HasNearby(step);
        }

        string MissingFor(int stepIndex)
        {
            var step = runtime.Recipe.Steps[stepIndex];

            foreach (var itemId in step.RequiredNearby)
            {
                if (aimedStation.FindNearby(itemId) == null)
                {
                    return itemId;
                }
            }

            return KitchenItems.None;
        }

        void Begin(Interactable target, int stepIndex, bool taste)
        {
            busyOn = target;
            tasting = taste;
            runtime.Begin(stepIndex, taste);
        }

        void Stop()
        {
            if (busyOn == null)
            {
                return;
            }

            busyOn = null;

            if (runtime != null)
            {
                runtime.Cancel();
            }
        }

        void Remember(IReadOnlyList<int> observed, IReadOnlyList<int> partial)
        {
            counts = observed;
        }

        bool AlreadyDone(int stepIndex)
        {
            if (counts == null || stepIndex >= counts.Count)
            {
                return false;
            }

            return counts[stepIndex] >= runtime.Recipe.Steps[stepIndex].RequiredCount;
        }

        static Holdable FirstOccupant(Transform station)
        {
            foreach (var slot in Slot.All)
            {
                if (slot.Occupant != null && slot.transform.IsChildOf(station))
                {
                    return slot.Occupant;
                }
            }

            return null;
        }

        Slot FreeSlot()
        {
            if (held == null)
            {
                return null;
            }

            if (aimedStation != null)
            {
                var onStation = Slot.FreeSlotFor(aimedStation.transform, held.ItemId);

                if (onStation != null)
                {
                    return onStation;
                }
            }

            if (aimedItem != null)
            {
                return Slot.FreeSlotFor(aimedItem.transform, held.ItemId);
            }

            return null;
        }

        void Diagnose()
        {
            Debug.Log($"[진단] 손={(held == null ? "빈손" : KitchenItems.NameOf(held.ItemId))} " +
                      $"조준물건={(aimedItem == null ? "없음" : aimedItem.Label)} " +
                      $"조준대={(aimedStation == null ? "없음" : aimedStation.Label)}");

            if (aimedStation == null)
            {
                Debug.Log("[진단] 스테이션을 조준하지 않았다");
                return;
            }

            foreach (var index in aimedStation.PerformSteps)
            {
                var step = runtime.Recipe.Steps[index];
                var inHand = held == null ? KitchenItems.None : held.ItemId;
                var handOk = step.RequiredItem == inHand;
                var done = counts != null && index < counts.Count ? counts[index].ToString() : "?";
                var line = $"[진단] {step.DisplayName}: 완료={done}회 손 필요={KitchenItems.NameOf(step.RequiredItem)} ({(handOk ? "OK" : "X")})";

                foreach (var itemId in step.RequiredNearby)
                {
                    var found = aimedStation.FindNearby(itemId);
                    var distance = found == null ? Nearest(itemId) : 0f;

                    line += found != null
                        ? $" / {KitchenItems.NameOf(itemId)} OK"
                        : $" / {KitchenItems.NameOf(itemId)} X (가장 가까운 것 {distance:F2}m, 판정 {aimedStation.NearbyRadius:F2}m)";
                }

                Debug.Log(line);
            }
        }

        float Nearest(string itemId)
        {
            var best = -1f;

            foreach (var item in Holdable.All)
            {
                if (item.ItemId != itemId || !item.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var distance = Vector3.Distance(item.transform.position, aimedStation.transform.position);

                if (best < 0f || distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        void Drop()
        {
            if (held == null)
            {
                return;
            }

            var target = head.position + head.forward * 0.9f;

            if (Physics.Raycast(head.position, head.forward, out var hit, placeDistance))
            {
                target = hit.normal.y > 0.5f
                    ? hit.point + Vector3.up * held.HalfHeight
                    : hit.point + hit.normal * 0.2f;
            }

            held.PlaceAt(target);
            held = null;
        }

        void Finished(int stepIndex)
        {
            var busyStation = busyOn;
            busyOn = null;

            if (stepIndex < 0)
            {
                return;
            }

            var step = runtime.Recipe.Steps[stepIndex];

            if (step.NearbyBecomes != KitchenItems.None && step.RequiredNearby.Count > 0 && busyStation != null)
            {
                var subject = busyStation.FindNearby(step.RequiredNearby[step.RequiredNearby.Count - 1]);
                subject?.Become(step.NearbyBecomes);
            }

            if (held == null)
            {
                return;
            }

            if (step.ConsumesItem)
            {
                held.Consume();
                held = null;
                runtime.Look();
                return;
            }

            if (step.ProducesItem != KitchenItems.None)
            {
                held.Become(step.ProducesItem);
            }

            runtime.Look();
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var targeted = aimedItem != null || aimedStation != null;

            GUI.Label(new Rect(centre.x - 10f, centre.y - 12f, 20f, 24f), targeted ? "+" : "·", style);

            if (held != null)
            {
                GUI.Label(new Rect(centre.x - 250f, Screen.height - 60f, 500f, 28f), "손: " + held.Label, style);
            }

            if (runtime != null && runtime.Working)
            {
                var width = 220f;
                var bar = new Rect(centre.x - width * 0.5f, centre.y + 56f, width, 10f);

                GUI.Box(bar, GUIContent.none);
                GUI.Box(new Rect(bar.x, bar.y, width * runtime.WorkFraction, bar.height), GUIContent.none);
                return;
            }

            GUI.Label(new Rect(centre.x - 250f, centre.y + 24f, 500f, 28f), Hint(), style);
        }

        string Hint()
        {
            if (aimedItem != null && held == null)
            {
                return aimedItem.Label + "   [F] 집기";
            }

            if (FreeSlot() != null)
            {
                return held.Label + "   [F] 올려놓기";
            }

            if (aimedStation == null)
            {
                return held == null ? "" : "[F] 내려놓기";
            }

            if (held == null && aimedStation != null)
            {
                var occupant = FirstOccupant(aimedStation.transform);

                if (occupant != null)
                {
                    return occupant.Label + "   [F] 집기";
                }
            }

            var hint = aimedStation.Label;
            var match = MatchingStep();

            if (match >= 0)
            {
                hint += "   [F] " + runtime.Recipe.Steps[match].DisplayName;
            }
            else
            {
                foreach (var index in aimedStation.PerformSteps)
                {
                    if (AlreadyDone(index))
                    {
                        continue;
                    }

                    var missing = MissingFor(index);

                    if (missing != KitchenItems.None)
                    {
                        hint += "   (" + KitchenItems.NameOf(missing) + " 없음)";
                        break;
                    }

                    var needed = runtime.Recipe.Steps[index].RequiredItem;

                    if (needed != KitchenItems.None)
                    {
                        hint += "   (" + KitchenItems.NameOf(needed) + " 들어야 함)";
                        break;
                    }
                }
            }

            if (CanTasteHere())
            {
                hint += "   [Q] 확인";
            }

            return hint;
        }
    }
}
