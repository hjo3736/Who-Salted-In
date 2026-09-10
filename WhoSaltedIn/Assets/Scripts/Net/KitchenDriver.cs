using System.Collections.Generic;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Net
{
    public sealed class KitchenDriver : NetworkBehaviour
    {
        [SerializeField] MatchRuntime runtime;

        bool working;
        ulong worker;
        int workStep;
        bool workIsTaste;
        float elapsed;

        public Recipe Recipe => runtime.Recipe;

        bool Live => IsSpawned && NetworkManager != null && NetworkManager.IsListening;

        public void BeginWork(int stepIndex, bool taste)
        {
            if (Live)
            {
                BeginWorkRpc(stepIndex, taste);
            }
        }

        public void CancelWork()
        {
            if (Live)
            {
                CancelWorkRpc();
            }
        }

        public void RequestLook()
        {
            if (Live)
            {
                RequestLookRpc();
            }
        }

        public void RequestRestore()
        {
            if (Live)
            {
                RequestRestoreRpc();
            }
        }

        public void Settle(IReadOnlyList<ItemPlacement> placements)
        {
            if (!Live)
            {
                return;
            }

            var keys = new int[placements.Count];
            var ids = new int[placements.Count];
            var present = new int[placements.Count];
            var values = new float[placements.Count * 7];

            for (var i = 0; i < placements.Count; i++)
            {
                var p = placements[i];

                keys[i] = KitchenItems.IndexOf(p.Key);
                ids[i] = KitchenItems.IndexOf(p.ItemId);
                present[i] = p.Present ? 1 : 0;

                values[i * 7 + 0] = p.PosX;
                values[i * 7 + 1] = p.PosY;
                values[i * 7 + 2] = p.PosZ;
                values[i * 7 + 3] = p.RotX;
                values[i * 7 + 4] = p.RotY;
                values[i * 7 + 5] = p.RotZ;
                values[i * 7 + 6] = p.RotW;
            }

            SettleRpc(keys, ids, present, values);
        }

        static ItemPlacement[] Unpack(int[] keys, int[] ids, int[] present, float[] values)
        {
            var placements = new ItemPlacement[ids.Length];

            for (var i = 0; i < ids.Length; i++)
            {
                placements[i] = new ItemPlacement(
                    KitchenItems.At(keys[i]),
                    KitchenItems.At(ids[i]),
                    present[i] != 0,
                    values[i * 7 + 0], values[i * 7 + 1], values[i * 7 + 2],
                    values[i * 7 + 3], values[i * 7 + 4], values[i * 7 + 5], values[i * 7 + 6]);
            }

            return placements;
        }

        RecipeStep WorkingStep => runtime.Recipe.Steps[workStep];

        float WorkDuration => workIsTaste ? WorkingStep.ObserveSeconds : WorkingStep.WorkSeconds;

        bool Allowed(ulong sender, int stepIndex) =>
            runtime.State.IsCurrentPlayer(sender) &&
            stepIndex >= 0 &&
            stepIndex < runtime.Recipe.Steps.Count;

        void Update()
        {
            if (!IsServer || !working)
            {
                return;
            }

            if (!runtime.State.IsCurrentPlayer(worker))
            {
                Interrupt();
                return;
            }

            elapsed += Time.deltaTime;

            if (elapsed >= WorkDuration)
            {
                Complete();
            }
        }

        void Complete()
        {
            var step = WorkingStep;
            var target = RpcTarget.Single(worker, RpcTargetUse.Temp);

            if (workIsTaste)
            {
                var count = runtime.State.Kitchen.CountOf(step.Id);
                var readingIndex = step.ReadingIndexFor(count);

                if (readingIndex >= 0)
                {
                    runtime.State.Apply(new Observed(worker, step.Id, step.ReadingFor(count)));
                }

                TasteResultRpc(workStep, readingIndex, target);
            }
            else
            {
                runtime.State.Apply(new StepPerformed(worker, step.Id));
                StepDoneRpc(workStep, target);
            }

            working = false;
        }

        void Interrupt()
        {
            if (!workIsTaste && elapsed > 0f)
            {
                runtime.State.Apply(new StepInterrupted(worker, WorkingStep.Id, elapsed));
            }

            working = false;
        }

        [Rpc(SendTo.Server)]
        void BeginWorkRpc(int stepIndex, bool taste, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (working)
            {
                Interrupt();
            }

            if (!Allowed(sender, stepIndex))
            {
                DeniedRpc(-1, RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            var step = runtime.Recipe.Steps[stepIndex];

            if (!taste)
            {
                var missing = runtime.Recipe.UnmetRequirement(step.Id, runtime.State.Kitchen);

                if (missing != null)
                {
                    DeniedRpc(runtime.Recipe.IndexOf(missing),
                        RpcTarget.Single(sender, RpcTargetUse.Temp));
                    return;
                }
            }

            working = true;
            worker = sender;
            workStep = stepIndex;
            workIsTaste = taste;
            elapsed = taste ? 0f : runtime.State.Kitchen.ProgressOf(step.Id);

            WorkStartedRpc(stepIndex, elapsed, WorkDuration, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        void CancelWorkRpc(RpcParams rpcParams = default)
        {
            if (working && rpcParams.Receive.SenderClientId == worker)
            {
                Interrupt();
            }
        }

        [Rpc(SendTo.Server)]
        void RequestLookRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (!runtime.State.IsCurrentPlayer(sender))
            {
                return;
            }

            var steps = runtime.Recipe.Steps;
            var counts = new int[steps.Count];
            var partial = new int[steps.Count];

            for (var i = 0; i < steps.Count; i++)
            {
                if (!steps[i].Observable)
                {
                    counts[i] = -1;
                    partial[i] = -1;
                    continue;
                }

                counts[i] = runtime.State.Kitchen.CountOf(steps[i].Id);
                partial[i] = Mathf.RoundToInt(
                    100f * runtime.State.Kitchen.ProgressOf(steps[i].Id) / Mathf.Max(steps[i].WorkSeconds, 0.01f));
            }

            LookResultRpc(counts, partial, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        void SettleRpc(int[] keys, int[] ids, int[] present, float[] values, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (working && worker == sender)
            {
                Interrupt();
            }

            try
            {
                runtime.State.Apply(new KitchenSettled(sender, Unpack(keys, ids, present, values)));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Settle] 서버 적용 실패: {e}");
                return;
            }

            var cook = runtime.State.CurrentPlayer;

            Debug.Log($"[Settle] 서버 수신 {ids.Length}개, 저장 {runtime.State.Kitchen.Placements.Count}개, " +
                      $"보낸이={sender} 현재수행자={(cook.HasValue ? cook.Value.ToString() : "없음")}");

            if (cook.HasValue && cook.Value != sender)
            {
                PushRestore(cook.Value);
            }
        }

        void PushRestore(ulong player)
        {
            var stored = runtime.State.Kitchen.Placements;
            var keys = new int[stored.Count];
            var ids = new int[stored.Count];
            var present = new int[stored.Count];
            var values = new float[stored.Count * 7];
            var i = 0;

            foreach (var pair in stored)
            {
                var p = pair.Value;

                keys[i] = KitchenItems.IndexOf(p.Key);
                ids[i] = KitchenItems.IndexOf(p.ItemId);
                present[i] = p.Present ? 1 : 0;

                values[i * 7 + 0] = p.PosX;
                values[i * 7 + 1] = p.PosY;
                values[i * 7 + 2] = p.PosZ;
                values[i * 7 + 3] = p.RotX;
                values[i * 7 + 4] = p.RotY;
                values[i * 7 + 5] = p.RotZ;
                values[i * 7 + 6] = p.RotW;
                i++;
            }

            RestoreRpc(keys, ids, present, values, RpcTarget.Single(player, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        void RequestRestoreRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (runtime.State.IsCurrentPlayer(sender))
            {
                PushRestore(sender);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void RestoreRpc(int[] keys, int[] ids, int[] present, float[] values, RpcParams rpcParams) =>
            runtime.ApplyRestore(Unpack(keys, ids, present, values));

        [Rpc(SendTo.SpecifiedInParams)]
        void DeniedRpc(int missingStep, RpcParams rpcParams) =>
            Debug.Log(missingStep < 0
                ? "[Kitchen] 지금은 할 수 없다"
                : $"[Kitchen] {runtime.Recipe.Steps[missingStep].DisplayName} 부터 해야 한다");

        [Rpc(SendTo.SpecifiedInParams)]
        void WorkStartedRpc(int stepIndex, float from, float duration, RpcParams rpcParams) =>
            runtime.BeginLocalWork(stepIndex, from, duration);

        [Rpc(SendTo.SpecifiedInParams)]
        void StepDoneRpc(int stepIndex, RpcParams rpcParams)
        {
            runtime.EndLocalWork(stepIndex);
            Debug.Log($"[Kitchen] {runtime.Recipe.Steps[stepIndex].DisplayName} 끝냈다");
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void TasteResultRpc(int stepIndex, int readingIndex, RpcParams rpcParams)
        {
            runtime.EndLocalWork(-1);

            var step = runtime.Recipe.Steps[stepIndex];

            Debug.Log(readingIndex < 0
                ? $"[Kitchen] {step.DisplayName} 는 알 방법이 없다"
                : $"[Kitchen] {step.DistinctReadings[readingIndex]}");
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void LookResultRpc(int[] counts, int[] partial, RpcParams rpcParams)
        {
            runtime.ApplyLook(counts, partial);

            var steps = runtime.Recipe.Steps;

            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] < 0)
                {
                    continue;
                }

                Debug.Log(partial[i] > 0
                    ? $"[Kitchen] {steps[i].DisplayName} — {counts[i]}회, 하다 만 게 {partial[i]}%"
                    : $"[Kitchen] {steps[i].DisplayName} — {counts[i]}회");
            }
        }
    }
}
