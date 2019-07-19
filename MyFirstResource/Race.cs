using System.Threading.Tasks;
using AlternateLife.RageMP.Net.Interfaces;
using AlternateLife.RageMP.Net.Scripting;
using AlternateLife.RageMP.Net.EventArgs;
using AlternateLife.RageMP.Net.Enums;
using System.Numerics;
using System;

namespace RaceGameMode
{
    public class Race
    {
        private Vector3[] CheckpointList = {new Vector3((float)1666.21, (float)3242.979, (float)39.64759),
                                            new Vector3((float)1084.181, (float)3082.283, (float)38.99031),
                                            new Vector3((float)1191.711, (float)3049.197, (float)39.11037),
                                            new Vector3((float)1540.279, (float)3148.619, (float)39.10761),
                                            new Vector3((float)1666.21, (float)3242.979, (float)39.64759)};

        private IColshape RaceStartColshape;
        private IMarker RaceStartMarker;
        public Race()
        { 
            MP.Events.Add("PLAYER_RACE_CAR_SELECTED", OnPlayerSelectCarForRace);
            MP.Events.Add("PLAYER_PASSED_CHECKPOINT", OnPlayerPassedCheckpoint);

            //Set Marker in which race can be started
            Vector3 pos = new Vector3((float)1683.09, (float)3261.494, (float)39.70709);
            RaceStartColshape = MP.Colshapes.NewCircle(new Vector2(pos.X, pos.Y), 1, 1);
            RaceStartColshape.SetData("id", "RaceStartMarker");
            RaceStartMarker = MP.Markers.New(MarkerType.VerticalCylinder, pos, new Vector3(0, 0, 0), new Vector3(0, 0, 1), 1, System.Drawing.Color.Yellow, true, 1);
            MP.Events.PlayerEnterColshape += OnPlayerEnterRaceStartColshape;
        }

        private async Task OnPlayerEnterRaceStartColshape(object sender, PlayerColshapeEventArgs eventArgs)
        {
            object colshape;
            if (eventArgs.Colshape.TryGetData("id", out colshape))
            {
                if(colshape.ToString() == "RaceStartMarker")
                {
                    await eventArgs.Player.CallAsync("showCarSelectMenu");
                }
            }
        }

        private async Task OnPlayerSelectCarForRace(object sender, PlayerRemoteEventEventArgs eventArgs)
        {
            IPlayer player = eventArgs.Player;
            uint selectedVehicleHash = MP.Utility.Joaat(eventArgs.Arguments[0].ToString());
            IVehicle vehicle = await MP.Vehicles.NewAsync(selectedVehicleHash, new Vector3((float)1679.801, (float)3247.63, (float)40.77756));
            await vehicle.SetDimensionAsync(1);
            await vehicle.SetRotationAsync(new Vector3(0, 0, 100));
            await eventArgs.Player.PutIntoVehicleAsync(vehicle, -1);
            player.ResetData("passedCheckpoints");
            await eventArgs.Player.OutputChatBoxAsync($"Spawned into {eventArgs.Arguments[0].ToString()}");
            await eventArgs.Player.CallAsync("setClientCheckpoint", 4, CheckpointList[0], CheckpointList[1], 0);
        }

        private async Task OnPlayerPassedCheckpoint(object sender, PlayerRemoteEventEventArgs eventArgs)
        {
            IPlayer player = eventArgs.Player;
            int passedCheckpointsFromServer = 0;
            object passedCheckpointsFromServerObj;

            if (player.TryGetData("passedCheckpoints", out passedCheckpointsFromServerObj))
            {
                passedCheckpointsFromServer = Convert.ToInt32(passedCheckpointsFromServerObj);
            }

            await eventArgs.Player.OutputChatBoxAsync($"Checkpoint { passedCheckpointsFromServer } passed");
            int finish = CheckpointList.Length;
            if (passedCheckpointsFromServer == 0)
            {
                await eventArgs.Player.OutputChatBoxAsync("Race started");
                eventArgs.Player.SetData("RaceStarted", DateTime.Now);
            }

            passedCheckpointsFromServer++;

            if (passedCheckpointsFromServer == finish)
            {
                await eventArgs.Player.OutputChatBoxAsync("Race finished");
                await eventArgs.Player.CallAsync("destroyActiveCheckpoint");
                DateTime finishTime = DateTime.Now;
                object startT;
                eventArgs.Player.TryGetData("RaceStarted", out startT);
                DateTime startTime;
                DateTime.TryParse(startT.ToString(), out startTime);
                await eventArgs.Player.OutputChatBoxAsync($"Time: {finishTime.Subtract(startTime).ToString()}");
            }
            else if (passedCheckpointsFromServer == finish - 1)
            {
                await eventArgs.Player.CallAsync("setClientCheckpoint", 4, CheckpointList[passedCheckpointsFromServer], new Vector3());
            }
            else
            {
                await eventArgs.Player.CallAsync("setClientCheckpoint", 1, CheckpointList[passedCheckpointsFromServer], CheckpointList[passedCheckpointsFromServer + 1]);
            }
            player.SetData("passedCheckpoints", passedCheckpointsFromServer);
        }
    }
}
