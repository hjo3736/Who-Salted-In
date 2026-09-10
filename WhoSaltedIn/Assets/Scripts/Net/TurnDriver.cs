using System.Linq;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Net
{
    public sealed class TurnDriver : NetworkBehaviour
    {
        [SerializeField] int averageTurnSeconds = 45;
        [SerializeField] int minTurnSeconds = 20;
        [SerializeField] int warnSeconds = 10;
        [SerializeField] int minPlayers = 3;
        [SerializeField] VoiceCheck voice;
        [SerializeField] MatchRuntime runtime;

        MatchState state => runtime.State;

        float remaining;
        bool running;
        bool warned;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            TryStartMatch();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        void OnClientConnected(ulong clientId) => TryStartMatch();

        void TryStartMatch()
        {
            if (running || NetworkManager.ConnectedClientsIds.Count < minPlayers)
            {
                return;
            }

            var players = NetworkManager.ConnectedClientsIds.ToList();
            var pacing = MatchPacing.ForPlayers(players.Count, averageTurnSeconds, minTurnSeconds);
            var seed = Random.Range(int.MinValue, int.MaxValue);

            var schedule = TurnSchedule.Build(
                players,
                pacing.Rounds,
                pacing.SecondsPerPlayer,
                pacing.MinSeconds,
                pacing.MaxSeconds,
                seed);

            running = true;

            Debug.Log($"[Turn] pacing players={pacing.PlayerCount} rounds={pacing.Rounds} " +
                      $"perPlayer={pacing.SecondsPerPlayer}s range={pacing.MinSeconds}-{pacing.MaxSeconds}s " +
                      $"match~{pacing.EstimatedMatchSeconds}s wait~{pacing.EstimatedWaitSeconds}s");

            StartMatchRpc(schedule.Order.ToArray(), schedule.Seconds.ToArray());
            AdvanceTurnRpc(0);
        }

        void Update()
        {
            if (!running || !state.IsRunning)
            {
                return;
            }

            remaining -= Time.deltaTime;

            if (!warned && remaining <= warnSeconds)
            {
                warned = true;

                if (state.IsCurrentPlayer(NetworkManager.LocalClientId))
                {
                    Debug.Log($"[Turn] {warnSeconds}s left");
                }
            }

            if (IsServer && remaining <= 0f)
            {
                AdvanceTurnRpc(state.TurnIndex + 1);
            }
        }

        [Rpc(SendTo.Everyone)]
        void StartMatchRpc(ulong[] order, int[] seconds)
        {
            state.Apply(new MatchStarted(order, seconds));
        }

        [Rpc(SendTo.Everyone)]
        void AdvanceTurnRpc(int turnIndex)
        {
            state.Apply(new TurnAdvanced(turnIndex));

            var me = NetworkManager.LocalClientId;
            var mine = state.IsCurrentPlayer(me);

            warned = false;
            remaining = state.CurrentTurnSeconds ?? 0f;

            if (state.IsFinished)
            {
                running = false;
                Debug.Log($"[Turn] match finished after {state.Order.Count} turns");

                if (IsServer)
                {
                    ReportMatch();
                }
            }
            else if (mine)
            {
                Debug.Log($"[Turn] {turnIndex + 1}/{state.Order.Count} my turn");
            }
            else
            {
                Debug.Log($"[Turn] {turnIndex + 1}/{state.Order.Count} cook={state.CurrentPlayer} waiting");
            }

            runtime.SetMyTurn(mine && !state.IsFinished);

            if (voice != null)
            {
                _ = voice.ApplyTurnAsync(mine);
            }
        }

        void ReportMatch()
        {
            var report = KitchenReport.Of(runtime.Recipe, state.Kitchen);

            Debug.Log(report.IsClean
                ? "[Result] 요리 성공"
                : $"[Result] 중복=[{string.Join(" ", report.Duplicated)}] 누락=[{string.Join(" ", report.Missing)}]");

            foreach (var step in runtime.Recipe.Steps)
            {
                Debug.Log($"[Result] {step.Id}: {state.Kitchen.CountOf(step.Id)}회");
            }

            foreach (var entry in state.Log)
            {
                switch (entry)
                {
                    case StepPerformed performed:
                        Debug.Log($"[Log] {performed.Player} → {performed.StepId}");
                        break;
                    case Observed observed:
                        Debug.Log($"[Log] {observed.Player} 관측 {observed.StepId} → \"{observed.Reading}\"");
                        break;
                    case TurnAdvanced advanced:
                        Debug.Log($"[Log] --- turn {advanced.TurnIndex + 1} ---");
                        break;
                }
            }
        }
    }
}
