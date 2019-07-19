using System.Threading.Tasks;
using AlternateLife.RageMP.Net.Interfaces;
using AlternateLife.RageMP.Net.Scripting;
using AlternateLife.RageMP.Net.EventArgs;
using AlternateLife.RageMP.Net.Enums;
using System.Numerics;
using System;

namespace RaceGameMode
{
    public class Core : IResource
    {
        public Core()
        {
            MP.Events.PlayerJoin += OnPlayerJoin;
            MP.Events.PlayerReady += OnPlayerReady;
            MP.Commands.RegisterHandler(new CommandHandler());

            new Race();
        }

        public Task OnStartAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnStopAsync()
        {
            return Task.CompletedTask;
        }

        private Task OnPlayerJoin(object sender, PlayerEventArgs eventArgs)
        {
            IPlayer player = eventArgs.Player;
            MP.Logger.Info($"Player {player.GetSocialClubName()} ({player.GetIp()}) joined");

            return Task.CompletedTask;
        }

        public async Task OnPlayerReady(object sender, PlayerEventArgs eventArgs)
        {
            IPlayer player = eventArgs.Player;
            await player.SetPositionAsync(new Vector3((float)1688.415, (float)3244.708, (float)40.8459));
            await player.SetDimensionAsync(1);

            await MP.Players.BroadcastAsync($"Player {player.GetName()} connected!");
        }
    }
}