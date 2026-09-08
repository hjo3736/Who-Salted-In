using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Game.Net
{
    public sealed class ServicesCheck : MonoBehaviour
    {
        async void Start()
        {
            await RunAsync();
        }

        static async Task RunAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log($"[UGS] initialized: {UnityServices.State}");

                var auth = AuthenticationService.Instance;

                if (!auth.IsSignedIn)
                {
                    await auth.SignInAnonymouslyAsync();
                }

                Debug.Log($"[UGS] signedIn={auth.IsSignedIn} playerId={auth.PlayerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGS] failed: {e}");
            }
        }
    }
}
