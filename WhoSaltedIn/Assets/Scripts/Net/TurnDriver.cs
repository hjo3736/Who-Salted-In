using System.Linq;
using Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Game.Net
{
    public sealed class TurnDriver : NetworkBehaviour
    {
        [SerializeField] float turnSeconds = 60f;
        [SerializeField] int rounds = 2;
        [SerializeField] int minPlayers = 3;
        [SerializeField] VoiceCheck voice;

        readonly MatchState state = new MatchState();

        float remaining;
        bool running;

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
            var seed = Random.Range(int.MinValue, int.MaxValue);
            var order = TurnOrder.Build(players, rounds, seed);

            running = true;
            remaining = turnSeconds;

            StartMatchRpc(order.ToArray(), seed);
            AdvanceTurnRpc(0);
        }

        void Update()
        {
            if (!IsServer || !running || state.IsFinished)
            {
                return;
            }

            remaining -= Time.deltaTime;

            if (remaining > 0f)
            {
                return;
            }

            remaining = turnSeconds;
            AdvanceTurnRpc(state.TurnIndex + 1);
        }

        [Rpc(SendTo.Everyone)]
        void StartMatchRpc(ulong[] order, int seed)
        {
            state.Apply(new MatchStarted(order));
            Debug.Log($"[Turn] match started seed={seed} order=[{string.Join(" ", order)}]");
        }

        [Rpc(SendTo.Everyone)]
        void AdvanceTurnRpc(int turnIndex)
        {
            state.Apply(new TurnAdvanced(turnIndex));

            var me = NetworkManager.LocalClientId;
            var mine = state.IsCurrentPlayer(me);

            if (state.IsFinished)
            {
                running = false;
                Debug.Log($"[Turn] match finished after {state.Order.Count} turns");
            }
            else
            {
                Debug.Log($"[Turn] {turnIndex + 1}/{state.Order.Count} cook={state.CurrentPlayer} me={me} mine={mine}");
            }

            if (voice != null)
            {
                _ = voice.ApplyTurnAsync(mine);
            }
        }
    }
}
