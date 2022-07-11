using System.Data.SQLite;
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
        private readonly List<object> Checkpoints = new();

        private readonly XmlSerializer Serializer = new(typeof(Map));
        private static readonly Random Random = new();
        private static SQLiteConnection Connection;

        public override void OnStart()
        {
            API.Events.OnPlayerReady += (s, c) =>
            {
                if (Session.State != State.Voting && Session.State != State.Preparing)
                    c.SendChatMessage("A race has already started, wait for the next round or use /join to join the race");
                else
                    c.SendChatMessage("The race will start soon, use /vote to vote for a map");
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
                        player.CheckpointsPassed = 0;
                API.SendChatMessage("Starting in 15 seconds, use /vote to vote for a map");
            }

            if (Session.State == State.Preparing && DateTime.Now > Session.NextEvent)
            {
                Session.State = State.Starting;

                foreach (var prop in API.Entities.GetAllProps())
                    prop.Delete();
                foreach (var vehicle in API.Entities.GetAllVehicles())
                    vehicle.Delete();

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

                Checkpoints.Clear();
                foreach (var checkpoint in Session.Map.Checkpoints)
                    Checkpoints.Add(checkpoint);

                int spawnPoint = 0;
                foreach (var client in API.GetAllClients())
                {
                    Join(client.Value, spawnPoint);
                    spawnPoint++;
                }

                API.SendChatMessage("The race is about to start, get ready");
                API.SendChatMessage("Press Y to respawn at last checkpoint");
                var countdown = new Thread((ThreadStart)delegate
                {
                    Thread.Sleep(10000);

                    API.SendCustomEvent(null, Events.CountDown);
                    Thread.Sleep(3000);

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
                    if (Session.Map.Checkpoints.Length > player.CheckpointsPassed + 1)
                        player.CheckpointsPassed++;
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

        public void Join(Client client, int spawnPoint)
        {
            Player player;
            lock (Session.Players)
            {
                player = Session.Players.FirstOrDefault(x => x.Client == client);
                if (player == null)
                {
                    player = new Player(client);
                    Session.Players.Add(player);
                }
            }

            var setupPlayer = new Thread((ThreadStart)delegate
            {
                client.SendNativeCall(Hash._SET_ISLAND_HOPPER_ENABLED, "HeistIsland",
                    Session.Map.SpawnPoints[0].Position.DistanceTo2D(new Vector2(4700f, -5145f)) < 2000f);
                var position = Session.Map.SpawnPoints[spawnPoint % Session.Map.SpawnPoints.Length].Position;
                var heading = Session.Map.SpawnPoints[spawnPoint % Session.Map.SpawnPoints.Length].Heading;
                client.Player.Position = position + new Vector3(4, 0, 1);
                player.VehicleHash = (int)Session.Map.AvailableVehicles[Random.Next(Session.Map.AvailableVehicles.Length)];
                player.Vehicle = API.Entities.CreateVehicle(client, player.VehicleHash, position, heading);
                Thread.Sleep(1000);
                client.SendNativeCall(Hash.SET_PED_INTO_VEHICLE, client.Player.Handle, player.Vehicle.Handle, -1);
                client.SendCustomEvent(Events.StartCheckpointSequence, Checkpoints.ToArray());
                if (Session.State == State.Started)
                    API.SendChatMessage($"{client.Username} joined the race");
                else
                    player.Vehicle.Freeze(true);
            });
            setupPlayer.Start();
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
                    client.SendCustomEvent(Events.LeaveRace);
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
                Join(ctx.Client, Random.Next(Session.Map.SpawnPoints.Length));
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

        private string[] GetMaps()
        {
            var folder = Path.Combine(AppContext.BaseDirectory, CurrentResource.DataFolder, "Maps");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Directory.GetFiles(folder, "*.xml");
        }

        private static string GetRandomMap()
        {
            return Maps[Random.Next(Maps.Count)].Name;
        }

        private void InitDB()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, CurrentResource.DataFolder, "times.db");

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
