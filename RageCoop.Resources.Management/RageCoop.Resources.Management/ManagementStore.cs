using System;
using System.Net;
namespace RageCoop.Resources.Management{
	public class ManagementStore
	{
		public bool AllowGuest { get; set; } = true;
		public HashSet<string> Banned { get; set; }=new();
		public Dictionary<string, Role> Roles { get; set; } = new()
		{
			{ "Admin",new Role() { Permissions=PermissionFlags.All, CommandFilteringMode=1 } },
			{ "User", new Role() { Permissions=PermissionFlags.None,CommandFilteringMode=1 } },
			{ "Guest", new Role() { Permissions=PermissionFlags.None, CommandFilteringMode=0} }
		};
		public Dictionary<string,Member> Members { get; set; } = new()
		{
			{ "Sausage",new() {Role="Admin",PassHash="iLoveSausage" } }
		};
	}
	public class Member{
		public string PassHash { get; set; }
		public string Role { get; set; }
	}
	public enum PermissionFlags:ulong
    {
		None = 0,
		Mute = 1 << 0,
		Kick = 1 << 1,
		Ban = 1 << 2,
		All = ~0u
	}
	public class Role
    {
		public PermissionFlags Permissions { get; set; }=PermissionFlags.None;
		/// <summary>
		/// 0:whitelist (block all by default), 1:blacklist (allow all by default).
		/// </summary>
		public byte CommandFilteringMode=1;
		public HashSet<string> WhiteListedCommands { get; set; } = new HashSet<string>();
		public HashSet<string> BlackListedCommands { get; set; } = new HashSet<string>();

    }

}