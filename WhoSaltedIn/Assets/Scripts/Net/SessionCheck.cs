using System;
using System.Collections.Generic;
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
        [SerializeField] VoiceCheck voice;

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

                session = await JoinWithRetryAsync(options);

                session.PlayerJoined += id => Log($"player joined {id}");
                session.PlayerHasLeft += id => Log($"player left {id}");
                session.RemovedFromSession += () => Log("removed from session");

                Log($"connected isHost={session.IsHost} code={session.Code}");

                if (voice != null)
                {
                    await voice.RunAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Session] failed: {e}");
            }
        }

        async Task<ISession> JoinWithRetryAsync(SessionOptions options)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);
                }
                catch (SessionException e) when (e.Error == SessionError.SessionConflict && attempt < 4)
                {
                    Log($"이전 세션이 남아 있다, 정리 후 재시도 {attempt}");
                    await LeaveStaleSessionsAsync();
                    await Task.Delay(1000);
                }
            }
        }

        static async Task LeaveStaleSessionsAsync()
        {
            List<string> joined;

            try
            {
                joined = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] 참가 목록 조회 실패: {e.Message}");
                return;
            }

            foreach (var id in joined)
            {
                try
                {
                    var stale = await MultiplayerService.Instance.ReconnectToSessionAsync(id);
                    await stale.LeaveAsync();
                    Debug.Log($"[Session] 남아 있던 세션 {id} 에서 나감");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Session] {id} 정리 실패: {e.Message}");
                }
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
