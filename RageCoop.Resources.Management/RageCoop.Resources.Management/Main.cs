using RageCoop.Server.Scripting;

// Optional
using RageCoop.Server;
using RageCoop.Core.Scripting;
using RageCoop.Core;
using Newtonsoft.Json;
using GTA.Native;
using GTA;

namespace RageCoop.Resources.Management
{
    public class Main :ServerScript
    {
        public ManagementStore ManagementStore { get; set; }
        private object _writeLock=new object();
        public override void OnStart()
        {
            API.RegisterCommands(this);
            API.RegisterCommand("spawn", (ctx) =>
            {
                API.Entities.CreateVehicle(ctx.Client,VehicleHash.Hydra,ctx.Client.Player.Position+GTA.Math.Vector3.WorldUp*5,0).Freeze(true);
                // API.Entities.CreateProp(VehicleHash.Hydra,ctx.Client.Player.Position+new GTA.Math.Vector3(0f, 0f, 10f),new());
            });
            API.RegisterCommand("delete", (ctx) =>
            {
                API.Entities.GetAllProps().ToList().ForEach((x) => { x.Delete(); });
            });
            API.RegisterCommand("blip", (ctx) =>
            {
                var b=API.Entities.CreateBlip(ctx.Client.Player.Position,0);
                b.Sprite=BlipSprite.CCTV;
                b.Scale=3;
                b.Color=BlipColor.Green;
                b.Name="Poop";
            });
            API.RegisterCommand("addblip", (ctx) =>
            {
                foreach(var p in API.Entities.GetAllPeds())
                {
                    var b=p.AddBlip();
                    b.Color=BlipColor.Purple;
                }
            });
            API.RegisterCommand("beee", (ctx) =>
            {
                foreach(var b in API.Entities.GetAllBlips())
                {
                    ctx.Client.SendNativeCall(Hash.SET_BLIP_SCALE, b.Handle,10f);
                }
            });
            API.RegisterCommand("delblip", (ctx) =>
            {
                API.Entities.GetAllBlips().ToList().ForEach((x) => { x.Delete(); });
            });
            API.RegisterCommand("propup", (ctx) =>
            {
                API.Entities.GetAllProps().ToList().ForEach((x) => { x.Position+=new GTA.Math.Vector3(0f, 0f, 1f); });
            });
            API.RegisterCommand("up", (ctx) =>
            {
                ctx.Client.Player.Position+=new GTA.Math.Vector3(0f, 0f, 100f);
                ctx.Client.Player.Rotation=new(50f,50f,50f);
            });
            API.RegisterCommand("vehup", (ctx) =>
            {
                ctx.Client.Player.LastVehicle.Position+=new GTA.Math.Vector3(0f, 0f, 100f);
                ctx.Client.Player.LastVehicle.Rotation=new(50f, 50f, 50f);
            });
            API.RegisterCommand("freeze", (ctx) =>
            {
                ctx.Client.Player.Freeze(true);
            });
            API.RegisterCommand("unfreeze", (ctx) =>
            {
                ctx.Client.Player.Freeze(false);
            });
            try
            {
                ManagementStore= (ManagementStore)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentResource.DataFolder, "ManagementStore.json")),typeof(ManagementStore));
                if (ManagementStore==null) { throw new ArgumentNullException(); }
                API.Logger.Info("Loaded ManagementStore.json");
            }
            catch
            {
                ManagementStore=new ManagementStore();
                Save();
                API.Logger.Info($"ManagementStore.json was written to {CurrentResource.DataFolder}.");
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
                var username = ctx.Args[0];
                var reason = ctx.Args.Length>=2 ? ctx.Args[1] : "EAT POOP!";
                if (ctx.Args.Length<1) { return; }
                else if (username.ToLower()==ctx.Client.Username.ToLower())
                {
                    ctx.Client.SendChatMessage("You cannot ban yourself.");
                    return;
                }
                var sender=ctx.Client;
                Task.Run(() =>
                {
                    try
                    {
                        var c = API.GetClientByUsername(username);
                        if (c!=null)
                        {
                            ManagementStore.Banned.Add(c.EndPoint.Address.ToString());
                            c.Kick(reason);
                            Save();
                            API.SendChatMessage($"{username} was banned:"+reason);
                        }
                        else
                        {
                            sender.SendChatMessage($"Can't find user: {username}");
                        }

                    }
                    catch (Exception ex)
                    {
                        API.Logger.Error(ex);
                    }
                });
            }
            else
            {
                ctx.Client.SendChatMessage("You don't have permission to perform this operation");
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
            Task.Run(() =>
            {
                try
                {
                    ManagementStore.Banned.Remove(API.GetClientByUsername(username).EndPoint.Address.ToString());
                    Save();
                    API.SendChatMessage($"{username} was unbanned.");
                }
                catch (Exception ex)
                {
                    API.Logger.Error(ex);
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
                    ctx.Client.SendChatMessage($"Can't find user:{ctx.Args[0]}.");
                }
            }
            else
            {
                ctx.Client.SendChatMessage("You don't have permission to perform this operation");
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
                e.Sender.SendChatMessage("You do not have permission to execute this command");
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
                    API.Logger.Error(ex);
                    return false;
                }
            }
        }
    }
}