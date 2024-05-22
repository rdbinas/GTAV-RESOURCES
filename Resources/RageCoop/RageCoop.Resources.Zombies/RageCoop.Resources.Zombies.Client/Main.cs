using GTA;
using RageCoop.Client.Scripting;
using System.Collections.Generic;
using GTA.Native;
using GTA.Math;
using System.Drawing;

namespace RageCoop.Resources.Zombies
{
    public class Main : ClientScript
    {
        private int _level;
        private int _kills;

        private readonly List<Ped> _zombies = new List<Ped>();
        private readonly List<Vehicle> _zombieVehicles = new List<Vehicle>();

        private RelationshipGroup _zombieGroup;

        public override void OnStart()
        {
            API.Events.OnTick += OnTick;

            API.RegisterCustomEventHandler(Events.Start, Start);

            API.QueueAction(() => {
                _zombieGroup = World.AddRelationshipGroup("ZOMBIES_MOD");
            });
        }

        public override void OnStop()
        {
            API.Events.OnTick -= OnTick;

            API.QueueAction(() =>
            {
                foreach (var ped in _zombies)
                {
                    ped.MarkAsNoLongerNeeded();
                    ped.Delete();
                }
                foreach (var veh in _zombieVehicles)
                {
                    veh.MarkAsNoLongerNeeded();
                    veh.Delete();
                }
                _zombies.Clear();
                _zombieVehicles.Clear();
            });
        }

        private void Start(CustomEventReceivedArgs obj)
        {
            _kills = (int)obj.Args[0];
            _level = (int)obj.Args[1];
        }

        private void OnTick()
        {
            Ped player = Game.Player.Character;

            foreach (var ped in World.GetNearbyPeds(player, 400))
            {
                if (ped.PopulationType != EntityPopulationType.RandomAmbient && ped.PopulationType != EntityPopulationType.RandomScenario && !_zombies.Contains(ped))
                    continue;

                if (ped.IsInVehicle())
                {
                    ped.CurrentVehicle.EngineHealth = 0;
                    _zombieVehicles.Add(ped.CurrentVehicle);
                }

                if (ped.IsAlive && ped.IsHuman)
                {
                    if (ped.RelationshipGroup != _zombieGroup)
                        Zombify(ped);
                    else if (ped.Position.DistanceTo(player.Position) < 1 && !player.IsGettingUp && !player.IsRagdoll)
                    {
                        Function.Call(Hash.APPLY_DAMAGE_TO_PED, player, 15);
                        Function.Call(Hash.SET_PED_TO_RAGDOLL, player, 1, 9000, 9000, 1, 1, 1);
                        Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, 1, 100, 100, 1, 1, 1);
                        ped.ApplyForceRelative(new Vector3(0, 1, 2));
                        player.ApplyForceRelative(new Vector3(0, -2, -10));
                        ZombifyPlayer(player); // Zombify the player when attacked by a zombie
                    }
                }

                if (player.RelationshipGroup == _zombieGroup)
                {
                    ped.Task.WanderAround();
                }

                if (ped.IsDead && !ped.IsOnScreen)
                {
                    ped.MarkAsNoLongerNeeded();
                    ped.Delete();
                }
            }
            foreach (var ped in _zombies.ToArray())
            {
                if (!ped.Exists())
                {
                    _zombies.Remove(ped);
                    continue;
                }

                if (ped.IsDead)
                {
                    if (ped.Killer == player)
                    {
                        _kills++;
                        if (_kills >= _level * 10)
                        {
                            _level++;
                            API.SendCustomEvent(Events.LevelUp, _kills, _level);
                        }
                    }
                    _zombies.Remove(ped);
                }

                if (ped.Position.DistanceTo(player.Position) > 400)
                {
                    ped.MarkAsNoLongerNeeded();
                    ped.Delete();
                }
            }

            foreach (var veh in _zombieVehicles.ToArray())
            {
                if (!veh.Exists())
                {
                    _zombieVehicles.Remove(veh);
                    continue;
                }

                if (veh.Position.DistanceTo(player.Position) > 400)
                {
                    veh.MarkAsNoLongerNeeded();
                    veh.Delete();
                }
            }

            if (_zombies.Count < 20)
            {
                var ped = World.CreateRandomPed(player.Position.Around(100));
                if (ped != null)
                    _zombies.Add(ped);
            }
        }

        private void Zombify(Ped ped)
        {
            if (!Function.Call<bool>(Hash.HAS_CLIP_SET_LOADED, "move_m@drunk@verydrunk"))
            {
                Function.Call(Hash.REQUEST_CLIP_SET, "move_m@drunk@verydrunk");
                return;
            }
            Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET, ped.Handle, "move_m@drunk@verydrunk", 1);
            Function.Call(Hash.APPLY_PED_DAMAGE_PACK, ped, "BigHitByVehicle", 0, 9);
            Function.Call(Hash.APPLY_PED_DAMAGE_PACK, ped, "SCR_Dumpster", 0, 9);
            Function.Call(Hash.APPLY_PED_DAMAGE_PACK, ped, "SCR_Torture", 0, 9);
            Function.Call(Hash.STOP_PED_SPEAKING, ped.Handle, true);
            Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, ped.Handle, true);
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped, 1);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped, 0, 0);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 46, 1);

            ped.Task.GoTo(Game.Player.Character);
            ped.AlwaysKeepTask = true;
            ped.IsEnemy = true;
            ped.Health = 3000;
            ped.RelationshipGroup = _zombieGroup;

            if (!_zombies.Contains(ped))
                _zombies.Add(ped);
        }
        private void ZombifyPlayer(Ped player)
        {
            if (!Function.Call<bool>(Hash.HAS_CLIP_SET_LOADED, "move_m@drunk@verydrunk"))
            {
                Function.Call(Hash.REQUEST_CLIP_SET, "move_m@drunk@verydrunk");
                return;
            }
            Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET, player.Handle, "move_m@drunk@verydrunk", 1);
            Function.Call(Hash.APPLY_PED_DAMAGE_PACK, player, "BigHitByVehicle", 0, 9);
            Function.Call(Hash.APPLY_PED_DAMAGE_PACK, player, "SCR_Dumpster", 0, 9);
            Function.Call(Hash.APPLY_PED_DAMAGE_PACK, player, "SCR_Torture", 0, 9);

            player.RelationshipGroup = _zombieGroup;
        }
    }
}
