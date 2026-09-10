using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Net
{
    public sealed class MatchRuntime : MonoBehaviour
    {
        [SerializeField] KitchenDriver kitchen;

        public MatchState State { get; } = new MatchState();

        public Recipe Recipe { get; } = PrototypeRecipe.Create();

        public bool IsMyTurn { get; private set; }

        public bool Working { get; private set; }

        public float WorkFrom { get; private set; }

        public float WorkDuration { get; private set; }

        public float WorkStartedAt { get; private set; }

        public event Action<bool> TurnChanged;

        public event Action<int> WorkCompleted;

        public event Action<IReadOnlyList<ItemPlacement>> Restored;

        public event Action<IReadOnlyList<int>, IReadOnlyList<int>> Looked;

        public float WorkFraction
        {
            get
            {
                if (!Working || WorkDuration <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01((WorkFrom + (Time.time - WorkStartedAt)) / WorkDuration);
            }
        }

        public void SetMyTurn(bool mine)
        {
            IsMyTurn = mine;
            TurnChanged?.Invoke(mine);
        }

        public void Begin(int stepIndex, bool taste) => kitchen.BeginWork(stepIndex, taste);

        public void Cancel()
        {
            Working = false;
            kitchen.CancelWork();
        }

        public void Look() => kitchen.RequestLook();

        public void RequestRestore() => kitchen.RequestRestore();

        public void Settle(IReadOnlyList<ItemPlacement> placements) => kitchen.Settle(placements);

        public void BeginLocalWork(int stepIndex, float from, float duration)
        {
            Working = true;
            WorkFrom = from;
            WorkDuration = duration;
            WorkStartedAt = Time.time;
        }

        public void EndLocalWork(int stepIndex)
        {
            Working = false;
            WorkCompleted?.Invoke(stepIndex);
        }

        public void ApplyRestore(IReadOnlyList<ItemPlacement> placements) => Restored?.Invoke(placements);

        public void ApplyLook(IReadOnlyList<int> counts, IReadOnlyList<int> partial) =>
            Looked?.Invoke(counts, partial);
    }
}
