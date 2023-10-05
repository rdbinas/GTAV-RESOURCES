using GTA.UI;
using GTA;
using RageCoop.Client.Scripting;
using System.Collections.Generic;
using System;
using GTA.Native;
using GTA.Math;

namespace RageCoop.Resources.GangWars
{
    public class Main : ClientScript
    {
        private int _level;
        private int _kills;

        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly List<Vehicle> _enemyVehicles = new List<Vehicle>();

        private Vehicle _hostVehicle;

        private RelationshipGroup _enemyGroup;

        private readonly Random _rndGet = new Random();

        private readonly WeaponHash[] _weaponList = {
            WeaponHash.Pistol,
            WeaponHash.CombatPistol,
            WeaponHash.APPistol,
            WeaponHash.BullpupShotgun,
            WeaponHash.SawnOffShotgun,
            WeaponHash.MicroSMG,
            WeaponHash.SMG,
            WeaponHash.AssaultRifle,
            WeaponHash.CarbineRifle
        };

        private readonly Model[] _vehicleList = {
            new Model(VehicleHash.Oracle),
            new Model(VehicleHash.Buffalo),
            new Model(VehicleHash.Exemplar),
            new Model(VehicleHash.Sultan),
            new Model(VehicleHash.Tailgater)
        };

        public override void OnStart()
        {
            API.RegisterCustomEventHandler(Events.Start, Start);
            API.RegisterCustomEventHandler(Events.Stop, Stop);
            API.Events.OnTick+=OnTick;

            API.QueueAction(() => {
                _enemyGroup = World.AddRelationshipGroup("GANGS_MOD");
            });
        }

        public override void OnStop()
        {
            API.QueueAction(() =>
            {
                StopMissions();
            });
        }

        private void OnTick()
        {
            Ped player = Game.Player.Character;

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].ped.IsDead)
                {
                    _kills++;
                    Game.Player.Money += 20 * _level;
                    _enemies[i].ped.MarkAsNoLongerNeeded();
                    _enemies[i].blip.Delete();
                    _enemies.RemoveAt(i);
                    if (_enemies.Count == 0)
                    {
                        foreach (var item in _enemyVehicles)
                            item.MarkAsNoLongerNeeded();
                        _enemyVehicles.Clear();
                        _level++;
                        API.SendCustomEvent(Events.MissionAccomplished, _kills, _level);
                        StartMissions(_kills, _level);
                    }
                }
                else
                {
                    if (_enemies[i].ped.IsInVehicle())
                    {
                        if ((player.Position - _enemies[i].ped.Position).Length() < 40.0f && !_enemies[i].spooked)
                        {
                            _enemies[i].spooked = true;
                            Function.Call(Hash.SET_DRIVE_TASK_CRUISE_SPEED, _enemies[i].ped.Handle, 60.0f);
                        }

                        if ((player.Position - _enemies[i].ped.Position).Length() < 50.0f && _enemies[i].ped.CurrentVehicle.Speed < 1.0f && _enemies[i].spooked && !_enemies[i].fighting)
                        {
                            _enemies[i].fighting = true;
                            TaskSequence tasks = new TaskSequence();
                            tasks.AddTask.LeaveVehicle();
                            tasks.AddTask.FightAgainst(player);
                            tasks.Close();
                            _enemies[i].ped.Task.PerformSequence(tasks);
                        }
                    }
                    else
                    {
                        if ((player.Position - _enemies[i].ped.Position).Length() < 60.0f)
                        {
                            if (!_enemies[i].fighting)
                            {
                                _enemies[i].fighting = true;
                                _enemies[i].ped.Task.FightAgainst(player);
                            }
                        }
                        else if (_enemies[i].ped.LastVehicle.IsAlive)
                        {
                            _enemies[i].fighting = false;
                            TaskSequence tasks = new TaskSequence();
                            tasks.AddTask.EnterVehicle(_enemies[i].ped.LastVehicle, VehicleSeat.Driver);
                            tasks.AddTask.CruiseWithVehicle(_enemies[i].ped.LastVehicle, 60.0f, DrivingStyle.AvoidTrafficExtremely);
                            tasks.Close();
                            _enemies[i].ped.Task.PerformSequence(tasks);
                        }
                    }
                }
            }
        }

        private void StartMissions(int kills, int level)
        {
            _kills = kills;
            _level = level;
            Notification.Show("Eliminate the ~r~enemies~w~.");
            Ped player = Game.Player.Character;

            for (int i = 1; i <= Math.Ceiling((decimal)_level / 4); i++)
            {
                Model vehModel = _vehicleList[_rndGet.Next(0, _vehicleList.Length)];
                if (vehModel.Request(1000))
                {
                    Vector3 pedSpawnPoint;
                    if (i == 1)
                    {
                        Vector3 playerpos = player.Position;

                        Vector3 v = new Vector3
                        {
                            X = (float)(_rndGet.NextDouble() - 0.5),
                            Y = (float)(_rndGet.NextDouble() - 0.5),
                            Z = 0.0f
                        };
                        v.Normalize();
                        playerpos += v * 500.0f;

                        pedSpawnPoint = World.GetNextPositionOnStreet(playerpos, true);
                    }
                    else
                    {
                        Vector3 playerpos = _hostVehicle.Position;

                        Vector3 v = new Vector3
                        {
                            X = (float)(_rndGet.NextDouble() - 0.5),
                            Y = (float)(_rndGet.NextDouble() - 0.5),
                            Z = 0.0f
                        };
                        v.Normalize();
                        playerpos += v * 200.0f;

                        pedSpawnPoint = World.GetNextPositionOnStreet(playerpos, true);
                    }
                    Vehicle tmpVeh = World.CreateVehicle(vehModel, pedSpawnPoint);
                    tmpVeh.PlaceOnGround();
                    tmpVeh.IsPersistent = true;
                    if (i == 1)
                        _hostVehicle = tmpVeh;

                    int maxPasseng;

                    if (i == Math.Ceiling((decimal)_level / 4))
                    {
                        maxPasseng = _level % 4;
                        if (maxPasseng == 0)
                            maxPasseng = 4;
                    }
                    else
                        maxPasseng = 4;

                    for (int d = 0; d < maxPasseng; d++)
                    {
                        Ped tmpPed = World.CreateRandomPed(pedSpawnPoint);
                        var gunid = _level > _weaponList.Length ? _weaponList[_rndGet.Next(0, _weaponList.Length)] : _weaponList[_rndGet.Next(0, _level)];
                        tmpPed.Weapons.Give(gunid, 999, true, true);
                        if (d == 0)
                        {
                            tmpPed.SetIntoVehicle(tmpVeh, VehicleSeat.Driver);
                            if (i == 1)
                                tmpPed.Task.CruiseWithVehicle(tmpPed.CurrentVehicle, 15.0f, DrivingStyle.AvoidTrafficExtremely);
                            else
                                Function.Call(Hash.TASK_VEHICLE_FOLLOW, tmpPed.Handle, tmpVeh, _hostVehicle, 15.0f, (int)DrivingStyle.AvoidTrafficExtremely);
                        }
                        else
                            tmpPed.SetIntoVehicle(tmpVeh, VehicleSeat.Any);

                        tmpPed.IsPersistent = true;
                        tmpPed.RelationshipGroup = _enemyGroup;
                        tmpPed.IsEnemy = true;
                        tmpPed.CanSwitchWeapons = true;

                        Blip tmpBlip = tmpPed.AddBlip();
                        tmpBlip.Color = BlipColor.Red;

                        _enemies.Add(new Enemy(tmpPed, tmpBlip));
                    }
                    _enemyVehicles.Add(tmpVeh);
                }
                else
                    Notification.Show("Error loading vehicle.");
            }
        }

        private void Start(CustomEventReceivedArgs obj)
        {
            API.QueueAction(() =>
            {
                StartMissions((int)obj.Args[0], (int)obj.Args[1]);
            });
        }

        private void StopMissions()
        {
            foreach (var item in _enemies)
            {
                item.ped.MarkAsNoLongerNeeded();
                item.blip.Delete();
            }
            foreach (var item in _enemyVehicles)
                item.MarkAsNoLongerNeeded();
            _enemies.Clear();
            _enemyVehicles.Clear();
        }

        private void Stop(CustomEventReceivedArgs obj)
        {
            API.QueueAction(() =>
            {
                StopMissions();
            });
        }
    }

    public class Enemy
    {
        public Ped ped;
        public Blip blip;
        public bool spooked;
        public bool fighting;

        public Enemy(Ped ped, Blip blip)
        {
            this.ped = ped;
            this.blip = blip;
            spooked = false;
            fighting = false;
        }
    }
}
