using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Game.Net
{
    public sealed class SessionCheck : MonoBehaviour
    {
        [SerializeField] string sessionId = "who-salted-in-dev";
        [SerializeField] int maxPlayers = 4;

        ISession session;

        async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync();

                var auth = AuthenticationService.Instance;
                if (!auth.IsSignedIn)
                {
                    await auth.SignInAnonymouslyAsync();
                }

                var options = new SessionOptions
                {
                    Name = sessionId,
                    MaxPlayers = maxPlayers,
                    IsPrivate = true
                }.WithRelayNetwork();

                session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);

                session.PlayerJoined += id => Log($"player joined {id}");
                session.PlayerHasLeft += id => Log($"player left {id}");
                session.RemovedFromSession += () => Log("removed from session");

                Log($"connected isHost={session.IsHost} code={session.Code}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Session] failed: {e}");
            }
        }

        void Log(string message)
        {
            var count = session == null ? 0 : session.PlayerCount;
            Debug.Log($"[Session] {message} players={count}/{maxPlayers}");
        }

        async void OnDestroy()
        {
            if (session == null)
            {
                return;
            }

            try
            {
                await session.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] leave failed: {e.Message}");
            }
        }
    }
}
