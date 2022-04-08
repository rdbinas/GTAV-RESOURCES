using System.ComponentModel;

using System.Data.SQLite;

using CoopServer;

namespace FirstResource
{
    public class Main : ServerScript
    {
        internal enum Groups
        {
            Normal = 0,
            Admin = 1
        }

        public static SQLiteConnection con;

        public Main()
        {
            try
            {
                var filename = Path.Combine(AppContext.BaseDirectory, "users.db");

                if (!File.Exists(filename))
                {
                    SQLiteConnection.CreateFile(filename);
                }

                var connectionString = new SQLiteConnectionStringBuilder()
                {
                    DataSource = filename,
                    Version = 3
                };

                con = new SQLiteConnection(connectionString.ToString());
                con.Open();

                new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS `users` (
	                `id` INTEGER PRIMARY KEY AUTOINCREMENT,
	                `username` VARCHAR(64),
	                `password` VARCHAR(255),
	                `rank` INTEGER DEFAULT 0
                );", con).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logging.Error(ex.ToString());
            }

            API.OnStart += () =>
            {
                Logging.Info("Resource started successfully!");
            };
            API.OnStop += () =>
            {
                Logging.Info($"Resource is stopped! Clients: {API.GetAllClients().Count}");
            };
            API.OnPlayerConnected += (Client client) =>
            {
                Logging.Info($"Player [{client.Player.Username}] connected!");
                API.SendChatMessageToAll($"Player {client.Player.Username} connected!");

                client.SendChatMessage("Use \"/login password\" to log in or \"/register password\" to register your username!");
            };
            API.OnPlayerDisconnected += (Client client) =>
            {
                API.SendChatMessageToAll($"Player {client.Player.Username} disconnected!");
                Logging.Info($"Player [{client.Player.Username}] disconnected!");
            };
            API.OnChatMessage += ChatMessage;

            API.RegisterCommands<Commands>();
            API.RegisterCommand("online", (CommandContext ctx) =>
            {
                ctx.Client.SendChatMessage($"Online: {API.GetAllClients().Count}");
            });
            API.RegisterCommand("info", "please use \"/info username\"", 1, (CommandContext ctx) =>
            {
                Client target = API.GetClientByUsername(ctx.Args[0]);
                if (target == null)
                {
                    ctx.Client.SendChatMessage("Player not online!");
                    return;
                }

                ctx.Client.SendChatMessage($"{ctx.Args[0]}: [Health: {target.Player.Health} | X: {target.Player.Position.X} | Y: {target.Player.Position.Y} | Z: {target.Player.Position.Z}]");
            });
        }

        public static void ChatMessage(string username, string message, CancelEventArgs args)
        {
            if (!message.Equals("EASTEREGG"))
            {
                return;
            }

            args.Cancel = true;

            Client client = API.GetClientByUsername(username);
            if (client == null)
            {
                return;
            }

            client.SendChatMessage("You have found the EASTEREGG! *-*");
        }
    }
}