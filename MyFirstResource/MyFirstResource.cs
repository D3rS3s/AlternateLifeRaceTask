using System.Threading.Tasks;
using AlternateLife.RageMP.Net.Interfaces;
using AlternateLife.RageMP.Net.Scripting;
using AlternateLife.RageMP.Net.EventArgs;
using AlternateLife.RageMP.Net.Enums;
using System.Numerics;
using System;

namespace MyFirstResource
{
    public class Race : IResource
    {
        private Vector3[] CheckpointList = {new Vector3((float)1666.21, (float)3242.979, (float)39.64759),
                                            new Vector3((float)1084.181, (float)3082.283, (float)38.99031),
                                            new Vector3((float)1191.711, (float)3049.197, (float)39.11037),
                                            new Vector3((float)1540.279, (float)3148.619, (float)39.10761),
                                            new Vector3((float)1666.21, (float)3242.979, (float)39.64759)};

        public Race()
        {
            MP.Events.PlayerJoin += OnPlayerJoin;
            MP.Events.PlayerReady += OnPlayerReady;
            MP.Events.PlayerEnterColshape += OnPlayerEnterColshape;
            MP.Events.PlayerEnterCheckpoint += OnPlayerEnterCheckpoint;

            MP.Events.Add("PLAYER_RACE_CAR_SELECTED", OnPlayerSelectCarForRace);
            
            MP.Commands.RegisterHandler(new CommandHandler());
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

            Vector3 pos = new Vector3((float)1683.09, (float)3261.494, (float)39.70709);
            await MP.Markers.NewAsync(MarkerType.VerticalCylinder, pos, new Vector3(0, 0, 0), new Vector3(0, 0, 1), 1, System.Drawing.Color.Yellow, true, 1);
            await MP.Colshapes.NewCircleAsync(new Vector2(pos.X, pos.Y), 1, 1);

            await MP.Players.BroadcastAsync($"Player {player.GetName()} connected!");
        }

        private async Task OnPlayerEnterColshape(object sender, PlayerColshapeEventArgs eventArgs)
        {
            await eventArgs.Player.CallAsync("showCarSelectMenu");
        }


        private async Task OnPlayerSelectCarForRace(object sender, PlayerRemoteEventEventArgs eventArgs)
        {
            uint selectedVehicleHash = MP.Utility.Joaat(eventArgs.Arguments[0].ToString());
            IVehicle vehicle = await MP.Vehicles.NewAsync(selectedVehicleHash, new Vector3((float)1679.801, (float)3247.63, (float)40.77756));
            await vehicle.SetDimensionAsync(1);
            await vehicle.SetRotationAsync(new Vector3(0, 0, 100));
            await eventArgs.Player.PutIntoVehicleAsync(vehicle, -1);
            await eventArgs.Player.OutputChatBoxAsync($"Spawned into {eventArgs.Arguments[0].ToString()}");

            await MP.Checkpoints.NewAsync(4, CheckpointList[0], CheckpointList[1], 10, System.Drawing.Color.Yellow, false, 1);
        }

        private async Task OnPlayerEnterCheckpoint(object sender, PlayerCheckpointEventArgs eventArgs)
        {
            object AlreadyPassedCheckpoints;
            int passedCheckpoints;
            if (eventArgs.Player.TryGetData("passedCheckpoints", out AlreadyPassedCheckpoints))
            {
                passedCheckpoints = Convert.ToInt32(AlreadyPassedCheckpoints);
                passedCheckpoints++;
            }
            else
            {
                passedCheckpoints = 1;
            }

            int finish = CheckpointList.Length;
            if (passedCheckpoints == 1)
            {
                await eventArgs.Player.OutputChatBoxAsync("Race started");
                eventArgs.Player.SetData("RaceStarted", DateTime.Now);
            }

            await eventArgs.Player.OutputChatBoxAsync($"Checkpoint { passedCheckpoints }");

            if(passedCheckpoints == finish)
            {
                await eventArgs.Player.OutputChatBoxAsync("Race finished");
                DateTime finishTime = DateTime.Now;
                object startT;
                eventArgs.Player.TryGetData("RaceStarted", out startT);
                DateTime startTime;
                DateTime.TryParse(startT.ToString(), out startTime);
                await eventArgs.Player.OutputChatBoxAsync($"Time: {finishTime.Subtract(startTime).ToString()}");
                passedCheckpoints = 0;
            }
            else if(passedCheckpoints == finish-1)
            {
                await MP.Checkpoints.NewAsync(4, CheckpointList[passedCheckpoints], new Vector3(), 10, System.Drawing.Color.Yellow, true, 1);
            }
            else
            {
                await MP.Checkpoints.NewAsync(1, CheckpointList[passedCheckpoints], CheckpointList[passedCheckpoints+1], 10, System.Drawing.Color.Yellow, true, 1);
            }
            eventArgs.Player.SetData("passedCheckpoints", passedCheckpoints);
            await eventArgs.Checkpoint.DestroyAsync();
        }
    }
}