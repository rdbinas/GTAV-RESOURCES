using RageCoop.Server;
using RageCoop.Server.Scripting;
using LiteDB;

namespace RageCoop.Resources.Zombies
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
            };
            API.Events.OnPlayerDisconnected += (s, c) =>
            {
            };
            API.Events.OnPlayerUpdate += OnPlayerUpdate;

            API.RegisterCommands(this);
            API.RegisterCustomEventHandler(Events.LevelUp, LevelUp);

            DB = new LiteDatabase(@$"Filename={Path.Combine(CurrentResource.DataFolder, "Records.db")}; Connection=Shared;");
            Records = DB.GetCollection<Record>();

            CurrentResource.Logger.Info("Zombies resource started");
        }

        public override void OnStop()
        {
            DB.Dispose();

            CurrentResource.Logger.Info($"Zombies resource stopped");
        }

        private void OnPlayerUpdate(object s, Client c)
        {
        }

        public void LevelUp(CustomEventReceivedArgs obj)
        {
            var kills = (int)obj.Args[0];
            var level = (int)obj.Args[1];
            API.SendChatMessage($"{obj.Client.Username} killed {kills} zombies, advanced to level {level}");

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
    }

    public class Record
    {
        public int Id { get; set; }
        public string Player { get; set; }
        public int Kills { get; set; }
        public int Level { get; set; }
    }
}
