using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using CoopServer;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;

namespace Race
{
    public class DiscordSettings
    {
        public string Token { get; set; }
        public string Webhook { get; set; }
        public ulong Channel { get; set; }

        public DiscordSettings()
        {
            Token = "token";
            Webhook = "webhook URL";
            Channel = 0;
        }

        public static DiscordSettings ReadSettings(string path)
        {
            var ser = new XmlSerializer(typeof(DiscordSettings));
            var xmlSettings = new XmlWriterSettings()
            {
                Indent = true,
            };
            DiscordSettings settings = null;
            if (File.Exists(path))
                using (var stream = XmlReader.Create(path)) settings = (DiscordSettings)ser.Deserialize(stream);
            else
                using (var stream = XmlWriter.Create(path, xmlSettings)) ser.Serialize(stream, settings = new DiscordSettings());
            return settings;
        }
    }

    public class DiscordBot
    {
        private readonly DiscordSettings Settings;
        private DiscordSocketClient Client;
        private readonly DiscordWebhookClient Webhook;
        private IMessageChannel Channel;
        private readonly bool Enabled = false;

        public DiscordBot()
        {
            Settings = DiscordSettings.ReadSettings($"resources{Path.DirectorySeparatorChar}DiscordSettings.xml");
            if (Settings.Token != "token" && Settings.Webhook != "webhook URL" && Settings.Channel != 0)
            {
                MainAsync().GetAwaiter().GetResult();
                Webhook = new DiscordWebhookClient(Settings.Webhook);
                Enabled = true;
            }
        }

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient();
            Client.Log += LogAsync;
            Client.Ready += ReadyAsync;
            Client.MessageReceived += MessageReceivedAsync;
            await Client.LoginAsync(TokenType.Bot, Settings.Token);
            await Client.StartAsync();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Channel = (IMessageChannel)Client.GetChannel(Settings.Channel);
            Console.WriteLine($"{Client.CurrentUser} is connected");
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == Client.CurrentUser.Id)
                return;

            if (message.Channel == Channel && !message.Author.IsBot && API.GetAllClients().Count > 0)
            {
                string usr = Regex.Replace(message.Author.Username, @"\p{Cs}", "");
                string msg = Regex.Replace(message.Content, @"\p{Cs}", "");
                if (msg.Length > 0)
                    API.SendChatMessageToAll(msg, usr + " [Discord]");
            }
        }

        public async Task SendToDiscord(string message, string name = "Server")
        {
            if (Enabled)
                await Webhook.SendMessageAsync(text: message, username: name);
        }
    }
}