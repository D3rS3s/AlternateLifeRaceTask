using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using AlternateLife.RageMP.Net.Interfaces;
using AlternateLife.RageMP.Net.Scripting;
using AlternateLife.RageMP.Net.EventArgs;
using AlternateLife.RageMP.Net.Enums;
using System.Linq;

namespace RaceGameMode
{
    public class Race
    {
        private Vector3[] CheckpointList = {new Vector3((float)1666.21, (float)3242.979, (float)39.64759),
                                            new Vector3((float)1084.181, (float)3082.283, (float)38.99031),
                                            new Vector3((float)1191.711, (float)3049.197, (float)39.11037),
                                            new Vector3((float)1540.279, (float)3148.619, (float)39.10761),
                                            new Vector3((float)1666.21, (float)3242.979, (float)39.64759)};

        private Vector3[] StartPositions = {new Vector3((float)1679.801, (float)3247.63, (float)40.77756),
                                            new Vector3((float)1684.105, (float)3243.514, (float)40.38148),
                                            new Vector3((float)1694.224, (float)3246.158, (float)40.47741),
                                            new Vector3((float)1694.163, (float)3250.678, (float)40.48647)};

        private IColshape RaceStartColshape;
        private IMarker RaceStartMarker;

        private uint[] CarsAvaiable = { (uint)VehicleHash.T20, (uint)VehicleHash.Issi6, (uint)VehicleHash.Bf400 };

        private List<IPlayer> PlayerList = new List<IPlayer>();
        private uint PlayerReady = 0;
        private uint PlayerFinished = 0;
        private Boolean RaceActive = false;
        private const uint MAX_PLAYERS = 4;
        private System.Timers.Timer countdown;
        private uint startTimerCount = 4;

        public Race()
        { 
            MP.Events.Add("PLAYER_RACE_CAR_SELECTED", OnPlayerSelectCarForRace);
            MP.Events.Add("PLAYER_PASSED_CHECKPOINT", OnPlayerPassedCheckpoint);
            MP.Events.Add("PLAYER_CAR_SELECT_CANCELED", OnPlayerCarSelectCanceled);

            //Set Marker in which race can be started
            Vector3 pos = new Vector3((float)1683.09, (float)3261.494, (float)39.70709);
            RaceStartColshape = MP.Colshapes.NewCircle(new Vector2(pos.X, pos.Y), 1, 1);
            RaceStartColshape.SetData("id", "RaceStartMarker");
            RaceStartMarker = MP.Markers.New(MarkerType.VerticalCylinder, pos, new Vector3(0, 0, 0), new Vector3(0, 0, 1), 1, System.Drawing.Color.Yellow, true, 1);
            MP.Events.PlayerEnterColshape += OnPlayerEnterRaceStartColshape;
            MP.Events.PlayerStartExitVehicle += OnPlayerExitRaceVehicle;
        }

        private async Task OnPlayerEnterRaceStartColshape(object sender, PlayerColshapeEventArgs eventArgs)
        {
            if(!RaceActive)
            {
                IPlayer player = eventArgs.Player;
                object colshape;
                if (eventArgs.Colshape.TryGetData("id", out colshape))
                {
                    if (colshape.ToString() == "RaceStartMarker")
                    {
                        //first add player that started the race to playerList and then get all players in range and add them
                        await player.CallAsync("destroyActiveCheckpoint");
                        if (PlayerList.Count() > 0)
                        {
                            PlayerList = new List<IPlayer>();
                        }
                        PlayerList.Add(player);

                        IReadOnlyCollection<IPlayer> playersInRange = await MP.Players.GetInRangeAsync(await player.GetPositionAsync(), 50, 1);
                        if (playersInRange != null)
                        {
                            foreach (IPlayer playerInRange in playersInRange)
                            {
                                //only allow up to MAX_PLAYERS player to join race
                                if(PlayerList.Count() < MAX_PLAYERS)
                                {
                                    PlayerList.Add(playerInRange);
                                }
                                else
                                {
                                    await playerInRange.OutputChatBoxAsync($"Sorry, race full");
                                }
                            }
                        }

                        //tell every client to show the car select menu
                        PlayerList.ForEach(async delegate (IPlayer racePlayer)
                        {
                            racePlayer.SetData("passedCheckpoints", 0);
                            await racePlayer.CallAsync("showCarSelectMenu");

                        });
                    }
                }
            }
        }

        private async Task OnPlayerCarSelectCanceled(object sender, PlayerRemoteEventEventArgs eventArgs)
        {
            if(RaceActive)
            {
                IPlayer player = eventArgs.Player;
                object hasPlayerSelectedCar;
                player.TryGetData("hasPlayerSelectedCar", out hasPlayerSelectedCar);
                await player.OutputChatBoxAsync($"hasPlayerSelectedCar: { (bool)hasPlayerSelectedCar }");
                if (!(bool)hasPlayerSelectedCar)
                {
                    await player.OutputChatBoxAsync("Car select canceled, removed from race");
                    if(PlayerList.Contains(player))
                    {
                        PlayerList.Remove(player);
                    }
                    await checkStartRace();
                }
            }
        }

        private async Task OnPlayerSelectCarForRace(object sender, PlayerRemoteEventEventArgs eventArgs)
        {
            RaceActive = true;
            //First check if player is in the race and validate if selected car is allowed
            IPlayer player = eventArgs.Player;
            await player.OutputChatBoxAsync($"car selected: {PlayerList.Contains(player)}");
            uint selectedVehicleHash = MP.Utility.Joaat(eventArgs.Arguments[0].ToString());
            if (PlayerList.Contains(player) && CarsAvaiable.Contains(selectedVehicleHash))
            {
                //TODO: freeze vehicle till countdown finished
                IVehicle vehicle = await MP.Vehicles.NewAsync(selectedVehicleHash, StartPositions[PlayerReady]);
                await vehicle.SetDimensionAsync(1);
                await vehicle.SetRotationAsync(new Vector3(0, 0, 100));
                await player.PutIntoVehicleAsync(vehicle, -1);
                await player.OutputChatBoxAsync($"Spawned into Vehicle, waiting for other players to ready up");
                await player.CallAsync("setClientCheckpoint", 4, CheckpointList[0], CheckpointList[1], 0);
                PlayerReady++;
            }
            await checkStartRace();
        }

        private async Task OnPlayerPassedCheckpoint(object sender, PlayerRemoteEventEventArgs eventArgs)
        {
            IPlayer player = eventArgs.Player;
            //validate that player is in the race
            if(PlayerList.Contains(player))
            {
                //get how many checkpoints the player already passed -> saved in playerData
                int passedCheckpointsFromServer = 0;
                object passedCheckpointsFromServerObj;
                if (player.TryGetData("passedCheckpoints", out passedCheckpointsFromServerObj))
                {
                    passedCheckpointsFromServer = Convert.ToInt32(passedCheckpointsFromServerObj);
                }

                //validate if checkpoint from client is at the same position that server expects
                if (validatePassedCheckpoint(passedCheckpointsFromServer, new Vector3((float) eventArgs.Arguments[0], (float)eventArgs.Arguments[1], (float)eventArgs.Arguments[2])))
                {
                    await player.OutputChatBoxAsync($"Checkpoint { passedCheckpointsFromServer } passed and validated");
                           
                    int finish = CheckpointList.Length;
                    passedCheckpointsFromServer++;

                    //Check if passed checkpoint is last one -> racefinished for particular player
                    if (passedCheckpointsFromServer == finish)
                    {
                        await eventArgs.Player.CallAsync("destroyActiveCheckpoint");
                        DateTime finishTime = DateTime.Now;
                        object startT;
                        eventArgs.Player.TryGetData("RaceStarted", out startT);
                        DateTime startTime;
                        DateTime.TryParse(startT.ToString(), out startTime);
                        await eventArgs.Player.OutputChatBoxAsync($"Your finish position: {++PlayerFinished}");
                        await eventArgs.Player.OutputChatBoxAsync($"Your time: {finishTime.Subtract(startTime).ToString()}");
                        if(PlayerFinished == PlayerList.Count())
                        {
                            await endRace();
                        }
                    }
                    // check if next checkpoint is last one -> display finishline
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
                //if checkpoint differs from expected checkpoint -> end the race
                else
                {
                    await player.OutputChatBoxAsync($"Checkpoint position differs from expected position -> race aborting");
                    await endRace();
                }
            }

        }

        private Boolean validatePassedCheckpoint(int playerPassedCheckpoints, Vector3 checkpointPosition)
        {
            if(playerPassedCheckpoints <= CheckpointList.Length)
            {
                if(CheckpointList[playerPassedCheckpoints] == checkpointPosition)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private Task checkStartRace()
        {
            if (PlayerReady == PlayerList.Count())
            {
                //Start Countdowntimer
                countdown = new System.Timers.Timer(1000);
                countdown.Elapsed += startCountdownEvent;
                countdown.AutoReset = true;
                countdown.Enabled = true;
            }
            return Task.CompletedTask;
        }

        private Task endRace()
        {
            PlayerList.ForEach(async delegate (IPlayer racePlayer)
            {
                await racePlayer.CallAsync("destroyActiveCheckpoint");
                await racePlayer.OutputChatBoxAsync("Race finished");
                IVehicle vehicle = await racePlayer.GetVehicleAsync();
                await vehicle.DestroyAsync();
                racePlayer.SetData("passedCheckpoints", 0);
            });
            PlayerReady = 0;
            PlayerFinished = 0;
            PlayerList = new List<IPlayer>();
            RaceActive = false;
            return Task.CompletedTask;
        }

        private async Task OnPlayerExitRaceVehicle(object sender, PlayerVehicleEventArgs eventArgs)
        {
            if(RaceActive)
            {
                IPlayer player = eventArgs.Player;
                IVehicle vehicle = eventArgs.Vehicle;
                await player.OutputChatBoxAsync($"Player exited Vehicle, removed from race");
                await vehicle.DestroyAsync();
                await player.CallAsync("destroyActiveCheckpoint");
                if (PlayerList.Contains(player))
                {
                    PlayerList.Remove(player);
                }
                if (PlayerList.Count() == 0)
                {
                    await endRace();
                }
            }
        }

        private void startCountdownEvent(object sender, EventArgs e)
        {
            startTimerCount--;
            PlayerList.ForEach(async delegate (IPlayer racePlayer)
            {
                if(startTimerCount > 0)
                {
                    await racePlayer.OutputChatBoxAsync($"Start in {startTimerCount}");
                }
                else
                {
                    //TODO: unfreeze vehicles
                    await racePlayer.OutputChatBoxAsync($"GO!");
                    racePlayer.SetData("RaceStarted", DateTime.Now);
                }
            });
            if(startTimerCount == 0)
            {
                startTimerCount = 4;
                countdown.Stop();
                countdown.Dispose();
            }
        }
    }
}