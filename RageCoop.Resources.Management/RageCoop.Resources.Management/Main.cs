﻿using RageCoop.Server.Scripting;
using RageCoop.Server;
using Newtonsoft.Json;
using GTA.Native;

namespace RageCoop.Resources.Management
{
    public class Main :ServerScript
    {
        public ManagementStore ManagementStore { get; set; }
        private object _writeLock=new object();
        public override void OnStart()
        {
            API.RegisterCommands(this);
            try
            {
                ManagementStore= (ManagementStore)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentResource.DataFolder, "ManagementStore.json")),typeof(ManagementStore));
                if (ManagementStore==null) { throw new ArgumentNullException(); }
                API.GetLogger().Info("Loaded ManagementStore.json");
            }
            catch
            {
                ManagementStore=new ManagementStore();
                Save();
                API.GetLogger().Info($"ManagementStore.json was written to {CurrentResource.DataFolder}.");
            }
            API.Events.OnPlayerHandshake+=(s, e) =>
            {
                Member m;
                if (ManagementStore.Banned.Contains(e.EndPoint.Address.ToString()))
                {
                    e.Deny("You're banned.");
                }
                else if (ManagementStore.Members.TryGetValue(e.Username,out m))
                {
                    if (e.PasswordHash!=m.PassHash)
                    {
                        e.Deny("Authentication failed!");
                    }
                }
                else if(!ManagementStore.AllowGuest){
                    e.Deny("You're not authorized");
                }
            };
            API.Events.OnCommandReceived+=FilterCommand;
            API.Events.OnPlayerReady+=(s,c) =>
            {
                c.Config.EnableAutoRespawn=false;
            };
        }
        private void Mute(string username)
        {

        }
        private void UnMute(string username)
        {

        }

        [Command("ban")]
        public void Ban(CommandContext ctx)
        {
            if (HasPermission(ctx.Client.Username, PermissionFlags.Ban))
            {
                if (ctx.Args.Length<1) { return; }
                var username=ctx.Args[0];
                var reason = ctx.Args.Length>=2 ? ctx.Args[1] : "EAT POOP!";
                var sender=ctx.Client;
                Task.Run(() =>
                {
                    try
                    {
                        var c = API.GetClientByUsername(username);
                        if (c!=null)
                        {
                            ManagementStore.Banned.Add(c.Connection.RemoteEndPoint.Address.ToString());
                            c.Kick(reason);
                            Save();
                            API.SendChatMessage($"{username} was banned:"+reason);
                        }
                        else
                        {
                            API.SendChatMessage($"Can't find user:{username}.", sender);
                        }

                    }
                    catch (Exception ex)
                    {
                        API.GetLogger().Error(ex);
                    }
                });
            }
            else
            {
                API.SendChatMessage("You don't have permission to perform this operation", ctx.Client);
            }
        }

        [Command("unban")]
        public void Unban(CommandContext ctx)
        {
            if (ctx.Args.Length<1) { return; }
            var username=ctx.Args[0];
            if (!HasPermission(ctx.Client.Username, PermissionFlags.Ban))
            {
                ctx.Client.SendChatMessage("You don't have permission to perform this operation");
                return;
            }
            else if (username==ctx.Client.Username)
            {
                ctx.Client.SendChatMessage("You cannot ban yourself.");
            }
            Task.Run(() =>
            {
                try
                {
                    ManagementStore.Banned.Remove(API.GetClientByUsername(username).Connection.RemoteEndPoint.Address.ToString());
                    Save();
                    API.SendChatMessage($"{username} was unbanned.");
                }
                catch (Exception ex)
                {
                    API.GetLogger().Error(ex);
                }
            });
        }

        [Command("kick")]
        public void Kick(CommandContext ctx)
        {
            if (HasPermission(ctx.Client.Username, PermissionFlags.Kick))
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
                    API.SendChatMessage($"Can't find user:{ctx.Args[0]}.", ctx.Client);
                }
            }
            else
            {
                API.SendChatMessage("You don't have permission to perform this operation", ctx.Client);
            }
        }
        private void FilterCommand(object sender, OnCommandEventArgs e)
        {

            Member m;
            Role r;
            if (ManagementStore.Members.TryGetValue(e.Sender.Username, out m))
            {
                if (ManagementStore.Roles.TryGetValue(m.Role, out r))
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
            else if (ManagementStore.AllowGuest && ManagementStore.Roles.TryGetValue("Guest", out r))
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
                API.SendChatMessage("You do not have permission to execute this command", e.Sender);
            }
        }
        private bool HasPermission(string username,PermissionFlags permission)
        {
            Member m;
            Role r;
            if(ManagementStore.Members.TryGetValue(username, out m))
            {
                if(ManagementStore.Roles.TryGetValue(m.Role.ToString(), out r))
                {
                    return r.Permissions.HasPermissionFlag(permission);
                }
            }
            else if (ManagementStore.AllowGuest && ManagementStore.Roles.TryGetValue("Guest", out r))
            {
                return r.Permissions.HasPermissionFlag(permission);
            }
            return false;
        }

        public override void OnStop()
        {
            
        }
        private bool Save()
        {
            lock (_writeLock)
            {
                try
                {
                    File.WriteAllText(Path.Combine(CurrentResource.DataFolder, "ManagementStore.json"), JsonConvert.SerializeObject(ManagementStore, Newtonsoft.Json.Formatting.Indented));
                    return true;
                }
                catch (Exception ex)
                {
                    API.GetLogger().Error(ex);
                    return false;
                }
            }
        }
    }
}