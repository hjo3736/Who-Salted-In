using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Net
{
    public sealed class KitchenDriver : NetworkBehaviour
    {
        [SerializeField] MatchRuntime runtime;

        public Recipe Recipe => runtime.Recipe;

        public void RequestStep(int stepIndex) => RequestStepRpc(stepIndex);

        public void RequestTaste(int stepIndex) => RequestTasteRpc(stepIndex);

        public void RequestLook() => RequestLookRpc();

        bool Allowed(ulong sender, int stepIndex) =>
            runtime.State.IsCurrentPlayer(sender) &&
            stepIndex >= 0 &&
            stepIndex < runtime.Recipe.Steps.Count;

        [Rpc(SendTo.Server)]
        void RequestStepRpc(int stepIndex, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (!Allowed(sender, stepIndex))
            {
                DeniedRpc(RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            var step = runtime.Recipe.Steps[stepIndex];
            runtime.State.Apply(new StepPerformed(sender, step.Id));

            StepDoneRpc(stepIndex, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        void RequestTasteRpc(int stepIndex, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (!Allowed(sender, stepIndex))
            {
                DeniedRpc(RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            var step = runtime.Recipe.Steps[stepIndex];
            var count = runtime.State.Kitchen.CountOf(step.Id);
            var readingIndex = step.ReadingIndexFor(count);

            if (readingIndex >= 0)
            {
                runtime.State.Apply(new Observed(sender, step.Id, step.ReadingFor(count)));
            }

            TasteResultRpc(stepIndex, readingIndex, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        void RequestLookRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (!runtime.State.IsCurrentPlayer(sender))
            {
                DeniedRpc(RpcTarget.Single(sender, RpcTargetUse.Temp));
                return;
            }

            var steps = runtime.Recipe.Steps;
            var counts = new int[steps.Count];

            for (var i = 0; i < steps.Count; i++)
            {
                counts[i] = steps[i].Observable ? runtime.State.Kitchen.CountOf(steps[i].Id) : -1;
            }

            LookResultRpc(counts, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void DeniedRpc(RpcParams rpcParams) => Debug.Log("[Kitchen] 지금은 할 수 없다");

        [Rpc(SendTo.SpecifiedInParams)]
        void StepDoneRpc(int stepIndex, RpcParams rpcParams) =>
            Debug.Log($"[Kitchen] {runtime.Recipe.Steps[stepIndex].DisplayName} 했다");

        [Rpc(SendTo.SpecifiedInParams)]
        void TasteResultRpc(int stepIndex, int readingIndex, RpcParams rpcParams)
        {
            var step = runtime.Recipe.Steps[stepIndex];

            Debug.Log(readingIndex < 0
                ? $"[Kitchen] {step.DisplayName} 는 알 방법이 없다"
                : $"[Kitchen] {step.DistinctReadings[readingIndex]}");
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void LookResultRpc(int[] counts, RpcParams rpcParams)
        {
            var steps = runtime.Recipe.Steps;

            for (var i = 0; i < counts.Length; i++)
            {
                Debug.Log(counts[i] < 0
                    ? $"[Kitchen] {steps[i].DisplayName} — 보이지 않는다"
                    : $"[Kitchen] {steps[i].DisplayName} — {counts[i]}회");
            }
        }
    }
}
