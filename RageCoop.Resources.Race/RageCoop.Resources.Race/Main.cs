﻿using System.Data.SQLite;
using System.Xml.Serialization;
using GTA.Math;
using GTA.Native;
using RageCoop.Server;
using RageCoop.Server.Scripting;
using RageCoop.Resources.Race.Objects;

namespace RageCoop.Resources.Race
{
    public class Main : ServerScript
    {
        private static List<Map> Maps;
        private static Session Session;
        private static readonly List<ServerBlip> Blips = new();

        private readonly XmlSerializer Serializer = new(typeof(Map));
        private static readonly Random Random = new();
        private static SQLiteConnection Connection;

        public override void OnStart()
        {
            API.Events.OnPlayerConnected += (s, c) =>
            {
                if (Session.State != State.Voting && Session.State != State.Preparing)
                    c.SendChatMessage("A race has already started, wait for the next round or use /join to join the race");
                else
                    c.SendChatMessage("The race will start soon, use /vote to vote for a map");

                c.SendNativeCall(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
                c.SendNativeCall(Hash.ON_ENTER_MP);
            };
            API.Events.OnPlayerDisconnected += (s, c) =>
            {
                Leave(c, true);
            };
            API.Events.OnPlayerUpdate += OnPlayerUpdate;

            API.RegisterCommands(this);

            Session.State = State.Voting;
            Session.Votes = new Dictionary<Client, string>();
            Session.Players = new List<Player>();
            Maps = GetMaps()?.Select(map => (Map)Serializer.Deserialize(new StreamReader(map))).ToList();
            InitDB();

            API.Logger.Info("Race resource started");
        }

        public override void OnStop()
        {
            API.Logger.Info($"Race resource stopped");
        }

        private void OnPlayerUpdate(object s, Client c)
        {
            if (Session.State == State.Voting)
            {
                Session.NextEvent = DateTime.Now.AddSeconds(15);
                Session.State = State.Preparing;
                Session.Votes = new Dictionary<Client, string>();
                lock (Session.Players)
                    foreach (var player in Session.Players)
                    {
                        RemoveCheckpoint(player);
                        player.CheckpointsPassed = 0;
                    }
                API.SendChatMessage("Starting in 15 seconds, use /vote to vote for a map");
            }

            if (Session.State == State.Preparing && DateTime.Now > Session.NextEvent)
            {
                Session.State = State.Starting;

                foreach (var prop in API.Entities.GetAllProps())
                    prop.Delete();
                foreach (var vehicle in API.Entities.GetAllVehicles())
                    vehicle.Delete();
                foreach (var blip in API.Entities.GetAllBlips())
                    blip.Delete();
                Blips.Clear();

                var map = Session.Votes.GroupBy(x => x.Value).OrderByDescending(vote => vote.Count()).FirstOrDefault()?.Key ?? GetRandomMap();
                Session.Map = Maps.First(x => x.Name == map);
                API.SendChatMessage("Map: " + map);
                var record = Record(map);
                if (record.Item1 > 0)
                    API.SendChatMessage($"Record: {TimeSpan.FromMilliseconds(record.Item1):m\\:ss\\.ff} by {record.Item2}");
                API.SendChatMessage("Use /respawn to return to the last checkpoint or /leave to leave the race");

                foreach (var prop in Session.Map.DecorativeProps)
                {
                    var p = API.Entities.CreateProp(prop.Hash, prop.Position, prop.Rotation);
                    if (prop.Texture > 0 && prop.Texture < 16)
                        API.SendNativeCall(null, Hash._SET_OBJECT_TEXTURE_VARIATION, p.Handle, prop.Texture);
                }

                foreach (var blip in Session.Map.Checkpoints)
                {
                    var b = API.Entities.CreateBlip(blip, 0);
                    b.Sprite = GTA.BlipSprite.Standard;
                    b.Color = GTA.BlipColor.Yellow2;
                    b.Name = "Checkpoint";
                    b.Scale = 0f;
                    Blips.Add(b);
                }

                Blips[0].Scale = 1f;
                Blips[1].Scale = 0.5f;

                foreach (var client in API.GetAllClients())
                    Join(client.Value);

                int spawnPoint = 0;

                lock (Session.Players)
                    foreach (var player in Session.Players)
                    {
                        player.Client.SendNativeCall(Hash._SET_ISLAND_HOPPER_ENABLED, "HeistIsland", CayoPericoCheck());
                        AddCheckpoint(player, 0);
                        CreateVehicle(player, spawnPoint, true);
                        spawnPoint++;
                    }

                API.SendChatMessage("The race is about to start, get ready");

                var countdown = new Thread((ThreadStart)delegate
                {
                    Thread.Sleep(10000);
                    for (int i = 3; i > 0; i--)
                    {
                        API.SendChatMessage($"{i}");
                        Thread.Sleep(1000);
                    }
                    API.SendChatMessage("Go!");

                    lock (Session.Players)
                        foreach (var player in Session.Players)
                            player.Vehicle.Freeze(false);

                    Session.State = State.Started;
                    Session.RaceStart = Environment.TickCount64;
                });
                countdown.Start();
            }

            if (Session.State == State.Started)
            {
                Player player;
                lock (Session.Players)
                    player = Session.Players.FirstOrDefault(x => x.Client == c);
                if (player != null && Vector3.Distance(player.Client.Player.Position, Session.Map.Checkpoints[player.CheckpointsPassed]) < 15f)
                {
                    player.Client.SendNativeCall(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");

                    RemoveCheckpoint(player);
                    HideBlip(player, player.CheckpointsPassed);

                    if (Session.Map.Checkpoints.Length > player.CheckpointsPassed + 1)
                    {
                        player.CheckpointsPassed++;
                        AddCheckpoint(player, player.CheckpointsPassed);
                        ShowBlips(player, player.CheckpointsPassed);
                    }
                    else
                    {
                        Session.State = State.Voting;

                        var time = Environment.TickCount64 - Session.RaceStart;
                        var msg = $"{player.Client.Username} finished in {TimeSpan.FromMilliseconds(time):m\\:ss\\.ff}";
                        var record = Record(Session.Map.Name);
                        if (record.Item1 > 0 && time < record.Item1)
                            msg += " (new record)";
                        if (Session.Players.Count > 1)
                            msg += $" ({Wins(player.Client.Username) + 1} wins)";
                        API.SendChatMessage(msg);
                        SaveTime(Session.Map.Name, player.Client.Username, time, Session.Players.Count > 1 ? 1 : 0);
                    }
                }
            }
        }

        public static void AddCheckpoint(Player player, int i)
        {
            var next = Session.Map.Checkpoints[i];
            var last = Session.Map.Checkpoints.Length == i + 1;
            var pointTo = Session.Map.Checkpoints[last ? i - 1 : i + 1];
            player.Client.SendNativeCall<int>((o) =>
            {
                player.CurrentCheckpoint = (int)o;
            }, Hash.CREATE_CHECKPOINT, last ? 4 : 0, next.X, next.Y, next.Z, pointTo.X, pointTo.Y, pointTo.Z, 10f, 241, 247, 57, 180, 0);
        }

        public static void RemoveCheckpoint(Player player)
        {
            player.Client.SendNativeCall(Hash.DELETE_CHECKPOINT, player.CurrentCheckpoint);
        }

        public static void ShowBlips(Player player, int i)
        {
            if (Blips.Count > i)
                player.Client.SendNativeCall(Hash.SET_BLIP_SCALE, Blips[i].Handle, 1f);
            if (Blips.Count > i + 1)
                player.Client.SendNativeCall(Hash.SET_BLIP_SCALE, Blips[i + 1].Handle, 0.5f);
        }

        public static void HideBlip(Player player, int i)
        {
            if (Blips.Count > i)
                player.Client.SendNativeCall(Hash.SET_BLIP_SCALE, Blips[i].Handle, 0f);
        }

        public void CreateVehicle(Player player, int spawnPoint, bool freeze)
        {
            var position = Session.Map.SpawnPoints[spawnPoint % Session.Map.SpawnPoints.Length].Position;
            var heading = Session.Map.SpawnPoints[spawnPoint % Session.Map.SpawnPoints.Length].Heading;
            player.Client.Player.Position = position + new Vector3(4, 0, 0);
            player.VehicleHash = (int)Session.Map.AvailableVehicles[Random.Next(Session.Map.AvailableVehicles.Length)];
            player.Vehicle = API.Entities.CreateVehicle(player.Client, player.VehicleHash, position, heading);
            player.Client.SendNativeCall(Hash.SET_PED_INTO_VEHICLE, player.Client.Player.Handle, player.Vehicle.Handle, -1);
            if (freeze)
                player.Vehicle.Freeze(true);
        }

        public void Join(Client client)
        {
            lock (Session.Players)
                if (Session.Players.Any(x => x.Client == client))
                    return;

            var player = new Player(client);

            if (Session.State == State.Started)
            {
                client.SendNativeCall(Hash._SET_ISLAND_HOPPER_ENABLED, "HeistIsland", CayoPericoCheck());
                AddCheckpoint(player, 0);
                ShowBlips(player, 0);
                CreateVehicle(player, Random.Next(Session.Map.SpawnPoints.Length), false);
                API.SendChatMessage($"{client.Username} joined the race");
            }

            lock (Session.Players)
                Session.Players.Add(player);
        }

        public void Leave(Client client, bool disconnected)
        {
            if (Session.Votes.ContainsKey(client))
                Session.Votes.Remove(client);

            Player player;
            lock (Session.Players)
                player = Session.Players.FirstOrDefault(x => x.Client == client);
            if (player != null)
            {
                if (!disconnected)
                {
                    player.Vehicle.Delete();
                    RemoveCheckpoint(player);
                    HideBlip(player, player.CheckpointsPassed);
                    HideBlip(player, player.CheckpointsPassed + 1);
                    API.SendChatMessage($"{client.Username} left the race");
                }

                lock (Session.Players)
                    Session.Players.Remove(player);
            }

            lock (Session.Players)
                if (!Session.Players.Any())
                    Session.State = State.Voting;
        }

        [Command("vote")]
        public void Vote(CommandContext ctx)
        {
            if (Session.State != State.Voting && Session.State != State.Preparing) return;

            if (ctx.Args.Length == 0)
            {
                ctx.Client.SendChatMessage("Use /vote (map), Maps: " + string.Join(", ", Maps.Select(x => x.Name)));
                return;
            }

            if (Session.Votes.ContainsKey(ctx.Client))
            {
                ctx.Client.SendChatMessage("You already voted for this round");
                return;
            }

            var voted = Maps.FirstOrDefault(x => x.Name.ToLower() == string.Join(" ", ctx.Args.ToArray()).ToLower());
            if (voted == null)
            {
                ctx.Client.SendChatMessage("No map with that name exists");
                return;
            }

            Session.Votes.Add(ctx.Client, voted.Name);
            API.SendChatMessage($"{ctx.Client.Username} voted for {voted.Name}");
        }

        [Command("join")]
        public void Join(CommandContext ctx)
        {
            if (Session.State == State.Started)
                Join(ctx.Client);
        }

        [Command("leave")]
        public void Leave(CommandContext ctx)
        {
            if (Session.State == State.Started)
                Leave(ctx.Client, false);
        }

        [Command("respawn")]
        public static void Restart(CommandContext ctx)
        {
            if (Session.State == State.Started)
            {
                Player player;
                lock (Session.Players)
                    player = Session.Players.FirstOrDefault(x => x.Client == ctx.Client);
                if (player != null)
                {
                    var last = player.CheckpointsPassed > 0 ? Session.Map.Checkpoints[player.CheckpointsPassed - 1] :
                        Session.Map.SpawnPoints[Random.Next(Session.Map.SpawnPoints.Length)].Position;
                    var dir = Vector3.Normalize(Session.Map.Checkpoints[player.CheckpointsPassed] - last);
                    var heading = (float)(-Math.Atan2(dir.X, dir.Y) * 180.0 / Math.PI);
                    player.Client.SendNativeCall(Hash.SET_ENTITY_COORDS, player.Vehicle.Handle, last.X, last.Y, last.Z, 0, 0, 0, 1);
                    player.Client.SendNativeCall(Hash.SET_ENTITY_HEADING, player.Vehicle.Handle, heading);
                    player.Client.SendNativeCall(Hash.STOP_ENTITY_FIRE, player.Vehicle.Handle);
                    player.Client.SendNativeCall(Hash.SET_PED_INTO_VEHICLE, player.Client.Player.Handle, player.Vehicle.Handle, -1);
                    player.Client.SendNativeCall(Hash.SET_VEHICLE_FIXED, player.Vehicle.Handle);
                    player.Client.SendNativeCall(Hash.SET_VEHICLE_ENGINE_ON, player.Vehicle.Handle, true, true);
                }
            }
        }

        private static bool CayoPericoCheck()
        {
            return Vector3.Distance(Session.Map.SpawnPoints[0].Position, new Vector3(4700f, -5145f, 0f)) < 2000f;
        }

        private static string[] GetMaps()
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "Resources", "Server", "RageCoop.Resources.Race", "Maps");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Directory.GetFiles(folder, "*.xml");
        }

        private static string GetRandomMap()
        {
            return Maps[Random.Next(Maps.Count)].Name;
        }

        private static void InitDB()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, "Resources", "Server", "RageCoop.Resources.Race", "times.db");

            if (!File.Exists(filename))
                SQLiteConnection.CreateFile(filename);

            var connectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = filename,
                Version = 3
            };

            Connection = new SQLiteConnection(connectionString.ToString());
            Connection.Open();

            new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS `times` (
                    `Id` INTEGER PRIMARY KEY AUTOINCREMENT,
                    `Race` TEXT,
                    `Player` TEXT,
                    `Time` INTEGER,
                    `Win` INTEGER
                );"
            , Connection).ExecuteNonQuery();
        }

        private static void SaveTime(string race, string player, long time, int win)
        {
            var query = new SQLiteCommand(
                "INSERT INTO `times` (`Race`, `Player`, `Time`, `Win`) VALUES (@race, @player, @time, @win);", Connection);
            query.Parameters.AddWithValue("@race", race);
            query.Parameters.AddWithValue("@player", player);
            query.Parameters.AddWithValue("@time", time);
            query.Parameters.AddWithValue("@win", win);
            query.ExecuteNonQuery();
        }

        private static Tuple<long, string> Record(string race)
        {
            var query = new SQLiteCommand("SELECT * FROM `times` WHERE `race` = @race ORDER BY `time` ASC LIMIT 1;", Connection);
            query.Parameters.AddWithValue("@race", race);

            Tuple<long, string> record;
            var reader = query.ExecuteReader();
            if (reader.Read())
                record = new((long)reader["Time"], (string)reader["Player"]);
            else
                record = new(0, "");
            reader.Close();

            return record;
        }

        private static int Wins(string player)
        {
            var query = new SQLiteCommand("SELECT COUNT(*) FROM `times` WHERE `player` = @player AND `win` = 1;", Connection);
            query.Parameters.AddWithValue("@player", player);

            return Convert.ToInt32(query.ExecuteScalar());
        }
    }
}