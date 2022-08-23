using RageCoop.Server.Scripting;

// Optional
using RageCoop.Server;
using RageCoop.Core.Scripting;
using RageCoop.Core;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.Net;

namespace RageCoop.Resources.Management
{
    public class Main : ServerScript
    {
        public ManagementStore ManagementStore { get; set; }
        private object _writeLock = new object();
        public override void OnStart()
        {
            API.RegisterCommands(this);
            ManagementStore=new ManagementStore(Path.Combine(CurrentResource.DataFolder));
            API.Logger.Info("Loaded management databse");
            API.Events.OnPlayerHandshake+=(s, e) =>
            {
                string bannedReason = ManagementStore.IsBanned(e.EndPoint.Address.ToString());
                var m = ManagementStore.GetMember(e.Username);
                if (bannedReason!=null)
                {
                    e.Deny("You're banned: "+bannedReason);
                }
                else if (m!=null)
                {
                    if (e.PasswordHash!=m.PassHash)
                    {
                        e.Deny("Authentication failed!");
                    }
                }
                else if (!ManagementStore.Config.AllowGuest)
                {
                    e.Deny("You're not authorized");
                }
            };
            API.Events.OnCommandReceived+=FilterCommand;
        }

        [Command("ban")]
        public void Ban(CommandContext ctx)
        {
            if (HasPermission(ctx.Client, PermissionFlags.Ban))
            {
                var username = ctx.Args[0];
                if(IPEndPoint.TryParse(username,out var p))
                {
                    ManagementStore.Ban(p.Address.ToString(), username); 
                    API.SendChatMessage($"{username} was banned");
                    Logger.Info(($"{username} was banned"));
                }
                else
                {
                    var reason = ctx.Args.Length >= 2 ? ctx.Args[1] : "EAT POOP!";
                    if (ctx.Args.Length < 1) { return; }
                    else if (username.ToLower() == ctx.Client?.Username?.ToLower())
                    {
                        ctx.Client?.SendChatMessage("You cannot ban yourself.");
                        return;
                    }
                    try
                    {
                        var c = API.GetClientByUsername(username);
                        if (c != null)
                        {
                            ManagementStore.Ban(c.EndPoint.Address.ToString(), username);
                            c.Kick(reason);
                            API.SendChatMessage($"{username} was banned:" + reason);
                        }
                        else
                        {
                            ctx.Client?.SendChatMessage($"Can't find user: {username}");
                        }

                    }
                    catch (Exception ex)
                    {
                        API.Logger.Error(ex);
                    }
                }
            }
            else
            {
                ctx.Client?.SendChatMessage("You don't have permission to perform this operation");
            }
        }

        [Command("unban")]
        public void Unban(CommandContext ctx)
        {
            if (ctx.Args.Length<1) { return; }
            var name = ctx.Args[0];
            if (!HasPermission(ctx.Client, PermissionFlags.Ban))
            {
                ctx.Client?.SendChatMessage("You don't have permission to perform this operation");
                return;
            }
            Task.Run(() =>
            {
                try
                {
                    ManagementStore.Unban(name);
                    API.SendChatMessage($"{name} was unbanned.");
                }
                catch (Exception ex)
                {
                    API.Logger.Error(ex);
                }
            });
        }

        [Command("register")]
        public void Register(CommandContext ctx)
        {
            if (ctx.Args.Length<2) { return; }
            else if (ctx.Client !=null && !HasPermission(ctx.Client, PermissionFlags.Register))
            {
                return;
            }
            var name = ctx.Args[0];
            var pass = ctx.Args[1];
            if (ManagementStore.Config.DefaultRole==null || ManagementStore.Config.DefaultRole.ToLower()=="guest") { return; }
            if (ManagementStore.AddMember(name, pass.GetSHA256Hash().ToHexString(), ManagementStore.Config.DefaultRole))
            {
                ctx.Client?.SendChatMessage("Successfully registered user: "+name);
            }
        }

        [Command("unregister")]
        public void Unregister(CommandContext ctx)
        {
            string name;
            if (ctx.Args.Length==0)
            {
                name=ctx.Client.Username;
            }
            else if (HasPermission(ctx.Client, PermissionFlags.All))
            {
                name= ctx.Args[0];
            }
            else
            {
                return ;
            }
            if (ManagementStore.RemoveMember(name))
            {
                ctx.Client?.SendChatMessage("Succesfully removed member: "+ name);
            }
        }

        [Command("kick")]
        public void Kick(CommandContext ctx)
        {
            if (HasPermission(ctx.Client, PermissionFlags.Kick))
            {
                if (ctx.Args.Length<1) { return; }
                var reason = "EAT POOP!";
                if (ctx.Args.Length>=2) { reason=ctx.Args[1]; }
                var c = API.GetClientByUsername(ctx.Args[0]);
                if (c!=null)
                {
                    c.Kick(reason);
                    API.SendChatMessage($"{c.Username} was kicked");
                }
                else
                {
                    ctx.Client?.SendChatMessage($"Can't find user:{ctx.Args[0]}.");
                }
            }
            else
            {
                ctx.Client?.SendChatMessage("You don't have permission to perform this operation");
            }
        }

        [Command("setrole")]
        public void SetRole(CommandContext ctx)
        {
            if(ctx!=null && !HasPermission(ctx.Client, PermissionFlags.All))
            {
                ctx.Client?.SendChatMessage("You don't have permission to perform this operation");
                return;
            }
            if(ctx.Args.Length<2) { return;}
            var name = ctx.Args[0];
            var role=ctx.Args[1];
            if (ManagementStore.Config.Roles.ContainsKey(role) && ManagementStore.SetRole(name, role))
            {
                ctx.Client?.SendChatMessage("Successfully updated role for member: "+name);
            }

        }
        private Role GetRole(string username)
        {
            var r = ManagementStore.GetMember(username)?.Role;
            if (r==null && ManagementStore.Config.AllowGuest)
            {
                r="Guest";
            }
            if (r!=null && ManagementStore.Config.Roles.TryGetValue(r, out Role role))
            {
                return role;
            }
            return null;
        }
        private void FilterCommand(object sender, OnCommandEventArgs e)
        {

            Member m;
            Role r;
            if (e.Client==null)
            {
                // Sent by server
                return;
            }
            if ((m=ManagementStore.GetMember(e.Client.Username))!=null)
            {
                if (ManagementStore.Config.Roles.TryGetValue(m.Role, out r))
                {
                    if (r.CommandFilteringMode==0)
                    {
                        e.Cancel=!r.WhiteListedCommands.Contains(e.Name);
                    }
                    else
                    {
                        e.Cancel=r.BlackListedCommands.Contains(e.Name);
                    }
                }
            }
            else if (ManagementStore.Config.AllowGuest && ManagementStore.Config.Roles.TryGetValue("Guest", out r))
            {
                if (r.CommandFilteringMode==0)
                {
                    e.Cancel=!r.WhiteListedCommands.Contains(e.Name);
                }
                else
                {
                    e.Cancel=r.BlackListedCommands.Contains(e.Name);
                }
            }
            else
            {
                e.Cancel=true;
            }
            if (e.Cancel)
            {
                e.Client.SendChatMessage("You do not have permission to execute this command");
            }
        }
        private bool HasPermission(Client sender, PermissionFlags permission)
        {
            if (sender==null) { return true; };
            Member m;
            Role r;
            if ((m=ManagementStore.GetMember(sender.Username))!=null)
            {
                if (ManagementStore.Config.Roles.TryGetValue(m.Role.ToString(), out r))
                {
                    return r.Permissions.HasPermissionFlag(permission);
                }
            }
            else if (ManagementStore.Config.AllowGuest && ManagementStore.Config.Roles.TryGetValue("Guest", out r))
            {
                return r.Permissions.HasPermissionFlag(permission);
            }
            return false;
        }

        public override void OnStop()
        {
            ManagementStore.Dispose();
        }
    }
}