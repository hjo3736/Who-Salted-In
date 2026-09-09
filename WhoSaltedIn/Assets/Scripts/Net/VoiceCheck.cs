using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Net
{
    public sealed class VoiceCheck : MonoBehaviour
    {
        const string LobbyChannel = "lobby";
        const string KitchenChannel = "kitchen";

        bool inKitchen;
        bool switching;

        public async Task RunAsync()
        {
            await VivoxService.Instance.InitializeAsync();

            VivoxService.Instance.ChannelJoined += name => Log($"joined {name}");
            VivoxService.Instance.ChannelLeft += name => Log($"left {name}");
            VivoxService.Instance.ParticipantAddedToChannel += p =>
                Log($"+{Short(p.PlayerId)}{(p.IsSelf ? "(me)" : "")} in {p.ChannelName}");
            VivoxService.Instance.ParticipantRemovedFromChannel += p =>
                Log($"-{Short(p.PlayerId)} in {p.ChannelName}");

            await VivoxService.Instance.LoginAsync();
            await VivoxService.Instance.JoinGroupChannelAsync(LobbyChannel, ChatCapability.AudioOnly);
        }

        void Update()
        {
            if (switching || Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame)
            {
                return;
            }

            _ = SwitchAsync();
        }

        async Task SwitchAsync()
        {
            switching = true;

            var from = inKitchen ? KitchenChannel : LobbyChannel;
            var to = inKitchen ? LobbyChannel : KitchenChannel;

            try
            {
                await VivoxService.Instance.LeaveChannelAsync(from);
                await VivoxService.Instance.JoinGroupChannelAsync(to, ChatCapability.AudioOnly);
                inKitchen = !inKitchen;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Voice] switch {from}->{to} failed: {e}");
            }
            finally
            {
                switching = false;
            }
        }

        void Log(string message)
        {
            var active = string.Join(" ", VivoxService.Instance.ActiveChannels
                .Select(c => $"{c.Key}({c.Value.Count})"));
            Debug.Log($"[Voice] {message} | active=[{active}]");
        }

        static string Short(string playerId) =>
            string.IsNullOrEmpty(playerId) || playerId.Length <= 6 ? playerId : playerId.Substring(0, 6);

        async void OnDestroy()
        {
            try
            {
                if (VivoxService.Instance.IsLoggedIn)
                {
                    await VivoxService.Instance.LeaveAllChannelsAsync();
                    await VivoxService.Instance.LogoutAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Voice] cleanup failed: {e.Message}");
            }
        }
    }
}
