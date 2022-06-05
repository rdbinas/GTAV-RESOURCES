using CoopServer;
using Race.Objects;

namespace Race
{
    internal class Commands
    {
        [Command("vote")]
        public static void Vote(CommandContext ctx)
        {
            if (Race.Session.State != State.Voting && Race.Session.State != State.Preparing) return;

            if (ctx.Args.Length == 0)
            {
                ctx.Client.SendChatMessage("Use /vote (map), Maps: " + string.Join(", ", Race.Maps.Select(x => x.Name)));
                return;
            }

            if (Race.Session.Votes.ContainsKey(ctx.Client))
            {
                ctx.Client.SendChatMessage("You already voted for this round");
                return;
            }

            var voted = Race.Maps.FirstOrDefault(x => x.Name.ToLower() == string.Join(" ", ctx.Args.ToArray()).ToLower());
            if (voted == default)
            {
                ctx.Client.SendChatMessage("No map with that name exists");
                return;
            }

            Race.Session.Votes.Add(ctx.Client, voted.Name);
            API.SendChatMessageToAll($"{ctx.Client.Player.Username} voted for {voted.Name}");
        }

        [Command("join")]
        public static void Join(CommandContext ctx)
        {
            if (Race.Session.State == State.Started)
                Race.Join(ctx.Client);
        }

        [Command("leave")]
        public static void Leave(CommandContext ctx)
        {
            if (Race.Session.State == State.Started)
                Race.Leave(ctx.Client, false);
        }

        [Command("respawn")]
        public static void Restart(CommandContext ctx)
        {
            if (Race.Session.State == State.Started)
                Race.Respawn(ctx.Client);
        }
    }
}
