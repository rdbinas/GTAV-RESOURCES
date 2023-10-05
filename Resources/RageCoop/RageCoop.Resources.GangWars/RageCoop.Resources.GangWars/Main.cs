using RageCoop.Server;
using RageCoop.Server.Scripting;
using LiteDB;

namespace RageCoop.Resources.GangWars
{
    public class Main : ServerScript
    {
        private static LiteDatabase DB;
        private static ILiteCollection<Record> Records;

        public override void OnStart()
        {
            API.Events.OnPlayerReady += (s, c) =>
            {
                int kills = 0;
                int level = 1;
                var player = Records.Query().Where(x => x.Player == c.Username.ToLower()).FirstOrDefault();
                if (player != null)
                {
                    kills = player.Kills;
                    level = player.Level;
                }
                c.SendCustomEvent(Events.Start, kills, level);
                if (level > 1)
                    c.SendChatMessage($"You are on level {level}, type /restart to start over from level 1");
            };
            API.Events.OnPlayerDisconnected += (s, c) =>
            {
            };
            API.Events.OnPlayerUpdate += OnPlayerUpdate;

            API.RegisterCommands(this);
            API.RegisterCustomEventHandler(Events.MissionAccomplished, MissionAccomplished);

            DB = new LiteDatabase(@$"Filename={Path.Combine(CurrentResource.DataFolder, "Records.db")}; Connection=Shared;");
            Records = DB.GetCollection<Record>();

            CurrentResource.Logger.Info("GangWars resource started");
        }

        public override void OnStop()
        {
            DB.Dispose();

            CurrentResource.Logger.Info($"GangWars resource stopped");
        }

        private void OnPlayerUpdate(object s, Client c)
        {
        }

        public void MissionAccomplished(CustomEventReceivedArgs obj)
        {
            var kills = (int)obj.Args[0];
            var level = (int)obj.Args[1];
            API.SendChatMessage($"{obj.Client.Username} killed {kills} enem{(kills > 1 ? "ies" : "y")}, advanced to level {level}");

            var player = Records.Query().Where(x => x.Player == obj.Client.Username.ToLower()).FirstOrDefault();
            if (player != null)
            {
                player.Kills = kills;
                player.Level = level;
                Records.Update(player);
            }
            else
                Records.Insert(new Record()
                {
                    Player = obj.Client.Username.ToLower(),
                    Kills = kills,
                    Level = level
                });
        }

        [Command("restart")]
        public static void Restart(CommandContext ctx)
        {
            Records.DeleteMany(x => x.Player == ctx.Client.Username.ToLower());
            ctx.Client.SendCustomEvent(Events.Stop);
            ctx.Client.SendCustomEvent(Events.Start, 0, 1);
            ctx.Client.SendChatMessage("Restarted from level 1");
        }
    }

    public class Record
    {
        public int Id { get; set; }
        public string Player { get; set; }
        public int Kills { get; set; }
        public int Level { get; set; }
    }
}
