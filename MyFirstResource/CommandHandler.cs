using System.Threading.Tasks;
using AlternateLife.RageMP.Net.Interfaces;
using AlternateLife.RageMP.Net.EventArgs;
using AlternateLife.RageMP.Net.Scripting;
using AlternateLife.RageMP.Net.Attributes;
using AlternateLife.RageMP.Net.Enums;
using System;

namespace RaceGameMode
{
    public class CommandHandler : ICommandHandler
    {
        [Command("vehicle")]
        public async Task Vehicle(IPlayer player, VehicleHash vehicleName)
        {
            IVehicle vehicle = await MP.Vehicles.NewAsync(vehicleName, player.GetPosition());
            vehicle.SetDimension(1);
            await player.PutIntoVehicleAsync(vehicle, -1);
            await player.OutputChatBoxAsync("Vehicle created");
            return;
        }

        [Command("position")]
        public async Task Position(IPlayer player)
        {
            MP.Logger.Info($"Player at {player.GetPosition()}");
            await player.OutputChatBoxAsync($"Player at position {player.GetPosition()}");
        }

        [Command("rotation")]
        public async Task Rotation(IPlayer player)
        {
            MP.Logger.Info($"Player at {player.GetRotation()}");
            await player.OutputChatBoxAsync($"Player at position {player.GetRotation()}");
        }
    }
}
