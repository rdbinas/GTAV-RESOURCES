using CoopServer;

using System.Data.SQLite;

namespace FirstResource
{
    internal class Commands
    {
        [Command("talk")]
        public static void talk_CMD(CommandContext ctx)
        {
            if (ctx.Args.Length == 0)
            {
                ctx.Client.SendChatMessage("please use \"/talk text\"");
                return;
            }

            string message = string.Join(" ", ctx.Args);
            API.SendChatMessageToAll(message);
        }

        [Command("gettime", Usage = "please use \"/gettime\"", ArgsLength = 0)]
        public static void gettime_CMD(CommandContext ctx)
        {
            ctx.Client.SendNativeResponse((object o) =>
            {
                ctx.Client.SendChatMessage($"Hours: {(int)o}");
            }, 0x25223CA6B4D20B7F, typeof(int));
        }

        [Command("register", Usage = "please use \"/register PASSWORD\"", ArgsLength = 1)]
        public static void register_CMD(CommandContext ctx)
        {
            SQLiteCommand cmd = new($"SELECT * FROM users WHERE username = '{ctx.Client.Player.Username}'", Main.con);
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                ctx.Client.SendChatMessage("This username is already registered!");
                reader.Close();
                return;
            }
            reader.Close();

            new SQLiteCommand($"INSERT INTO users (username, password) VALUES ('{ctx.Client.Player.Username}', '{ctx.Args[0]}')", Main.con).ExecuteNonQuery();
            ctx.Client.SendChatMessage("You are now registered! Use \"/login password\" to log in!");
        }

        [Command("login", Usage = "please use \"/login PASSWORD\"", ArgsLength = 1)]
        public static void login_CMD(CommandContext ctx)
        {
            if (ctx.Client.HasData("loggedin"))
            {
                ctx.Client.SendChatMessage("You are already logged in!");
                return;
            }

            SQLiteCommand cmd = new($"SELECT * FROM users WHERE username = '{ctx.Client.Player.Username}'", Main.con);
            SQLiteDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                ctx.Client.SendChatMessage("This username is not yet registered!");
            }
            else
            {
                while (reader.Read())
                {
                    if (!reader.GetString(2).Equals(ctx.Args[0]))
                    {
                        ctx.Client.SendChatMessage("The password is wrong!");
                        return;
                    }
                    ctx.Client.SetData("loggedin", true);
                    ctx.Client.SetData("group", reader.GetInt32(3));
                    ctx.Client.SendChatMessage("You are now logged in!");
                }
            }
            reader.Close();
        }

        [Command("setgroup", Usage = "please use \"/setgroup SECRET USERNAME VALUE(0-1)\"", ArgsLength = 3)]
        public static void setgroup_CMD(CommandContext ctx)
        {
            if (!ctx.Client.HasData("loggedin"))
            {
                ctx.Client.SendChatMessage("You are not logged in!");
                return;
            }

            // Change the SECRET to your password
            if (!ctx.Args[0].Equals("SECRET"))
            {
                ctx.Client.SendChatMessage("Wrong secret!");
                return;
            }

            if (!int.TryParse(ctx.Args[2], out int newGroup))
            {
                ctx.Client.SendChatMessage("VALUE must be a number!");
                return;
            }

            if (newGroup > 1 || newGroup < 0)
            {
                ctx.Client.SendChatMessage("VALUE must be 0 or 1!");
                return;
            }

            SQLiteCommand cmd = new($"SELECT * FROM users WHERE username = '{ctx.Args[1]}'", Main.con);
            cmd.CommandType = System.Data.CommandType.Text;

            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            {
                ctx.Client.SendChatMessage("This username is not yet registered!");
                return;
            }

            new SQLiteCommand($"UPDATE users SET rank = {newGroup} WHERE username = '{ctx.Args[1]}'", Main.con).ExecuteNonQuery();

            Client target = API.GetClientByUsername(ctx.Args[1]);
            if (target != null && target.HasData("loggedin"))
            {
                target.SetData("group", newGroup);
                target.SendChatMessage($"New group received: \"{Enum.GetName(typeof(Main.Groups), newGroup)}\"!");
            }

            ctx.Client.SendChatMessage($"This player is now \"{Enum.GetName(typeof(Main.Groups), newGroup)}\"!");
        }

        [Command("kick", Usage = "please use \"/kick USERNAME\"", ArgsLength = 1)]
        public static void kick_CMD(CommandContext ctx)
        {
            if (!ctx.Client.HasData("loggedin"))
            {
                ctx.Client.SendChatMessage("You are not logged in!");
                return;
            }

            if (ctx.Client.GetData<int>("group") == 0)
            {
                ctx.Client.SendChatMessage("You cannot execute this command!");
                return;
            }

            Client target = API.GetClientByUsername(ctx.Args[0]);
            if (target == null)
            {
                ctx.Client.SendChatMessage($"Username \"{ctx.Args[0]}\" not found!");
                return;
            }

            target.Kick("You got kicked!");
            ctx.Client.SendChatMessage($"{ctx.Args[0]} was kicked!");
        }
    }
}