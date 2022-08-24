using System;
using System.Net;
using System.Data.SQLite;
using Newtonsoft.Json;
using RageCoop.Core;

namespace RageCoop.Resources.Management
{
	public class ManagementStore : IDisposable
	{
		private SQLiteConnection _con;
		public Config Config { get; set; }
		public ManagementStore(string dataFolder,Logger logger)
		{
			var configPath = Path.Combine(dataFolder, "Config.json");
            if (!File.Exists(configPath))
            {
				Config = new Config();
				File.WriteAllText(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
			}
            else
            {
				try
				{
					Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
				}
				catch(Exception ex)
				{
					logger.Warning("Failed to parse Config.json, overwritting with default values");
					logger.Error(ex);
					Config = new Config();
					File.WriteAllText(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
				}
			}
			InitDB(Path.Combine(dataFolder, "Members.db"));
			var check = new SQLiteCommand("SELECT * FROM Members;", _con);
			if (!check.ExecuteReader().Read())
			{
				var query = new SQLiteCommand(
				"INSERT INTO `Members` (Username, PassHash, Role) VALUES (\"sausage\",\"FB6F796DFD477E6361A19057EE0235E88820060EC786343229B4D839C01B47C3\" ,\"Admin\" );", _con);
				query.ExecuteNonQuery();
			}
		}
		private void InitDB(string path)
		{

			if (!File.Exists(path))
				SQLiteConnection.CreateFile(path);

			var connectionString = new SQLiteConnectionStringBuilder()
			{
				DataSource = path,
				Version = 3
			};

			_con = new SQLiteConnection(connectionString.ToString());
			_con.Open();

			new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS `Members` (
                    `Username` TEXT PRIMARY KEY,
                    `PassHash` TEXT,
                    `Role` TEXT
                );"
			, _con).ExecuteNonQuery();

			new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS `Banned` (
                    `IP` TEXT PRIMARY KEY,
                    `Username` TEXT,
                    `Reason` TEXT
                );"
			, _con).ExecuteNonQuery();
		}
		public string IsBanned(string ip)
		{
			var query = new SQLiteCommand("SELECT * FROM `Banned` WHERE `IP` = @ip ;", _con);
			query.Parameters.AddWithValue("@ip", ip);
			var reader = query.ExecuteReader();
			if (reader.Read())
			{
				return (string)reader["Reason"];
			}
			else
			{
				return null;
			}
		}
		public Member GetMember(string name)
		{
			var query = new SQLiteCommand($"SELECT * FROM `Members` WHERE `Username` = \"{name.ToLower()}\" ;", _con);
			var reader = query.ExecuteReader();
			if (reader.Read())
			{
				return new Member { PassHash=(string)reader["PassHash"], Role=(string)reader["Role"] };
			}
			else
			{
				return null;
			}
		}
		public bool AddMember(string username, string passHash, string role)
		{
			var check = new SQLiteCommand($"SELECT * FROM `Members` WHERE `Username` = \"{username.ToLower()}\";", _con);
			if (!check.ExecuteReader().Read())
			{
				var query = new SQLiteCommand(
				$"INSERT INTO `Members` (Username, PassHash, Role) " +
				$"VALUES (\"{username.ToLower()}\",\"{passHash}\" ,\"{role}\" );", _con);
				query.ExecuteNonQuery();
				return true;
			}
			else
			{
				return false;
			}
		}
		public bool SetRole(string username, string role)
        {
			username=username.ToLower();
			SQLiteCommand query;
			query = new SQLiteCommand($"UPDATE Members SET Role=\"{role}\" WHERE Username = \"{username.ToLower()}\";", _con);
			return query.ExecuteNonQuery()!=0;
		}
		public bool RemoveMember(string username)
        {
			SQLiteCommand query;
			query = new SQLiteCommand($"DELETE FROM Members WHERE Username = \"{username.ToLower()}\";", _con);
			return query.ExecuteNonQuery()!=0;
		}
		public void Ban(string ip, string username, string reason = "Unspecified")
		{
			var query = new SQLiteCommand(
				$"INSERT OR IGNORE INTO Banned(IP, Username, Reason) " +
				$"VALUES(\"{ip}\", \"{username.ToLower()}\", \"{reason}\");", _con);
			query.ExecuteNonQuery();
		}
		public void Unban(string ipOrUserName)
		{
			SQLiteCommand query;
			if (IPEndPoint.TryParse(ipOrUserName, out _))
			{
				query = new SQLiteCommand($"DELETE FROM Banned WHERE IP = \"{ipOrUserName}\";", _con);
			}
			else
			{
				query = new SQLiteCommand($"DELETE FROM Banned WHERE Username = \"{ipOrUserName.ToLower()}\";", _con);
			}
			query.ExecuteNonQuery();
		}

		public void Dispose()
		{
			_con.Close();
		}
	}
	public class Member
	{
		public string PassHash { get; set; }
		public string Role { get; set; }
	}

	[Flags]
	public enum PermissionFlags : ulong
	{
		None = 0,
		Mute = 1 << 0,
		Kick = 1 << 1,
		Ban = 1 << 2,
		Register = 1 << 3,
		All = ~0u
	}
	public class Config
	{
		public bool AllowGuest = true;
		/// <summary>
		/// The role to assign to a newly registered user
		/// </summary>
		public string DefaultRole = "User";
		public readonly Dictionary<string, Role> Roles = new()
		{
			{ "Admin", new Role() { Permissions=PermissionFlags.All, CommandFilteringMode=1 } },
			{ "User", new Role() { Permissions=PermissionFlags.Register, CommandFilteringMode=1 } },
			{ "Guest", new Role() { Permissions=PermissionFlags.Register, CommandFilteringMode=0, WhiteListedCommands=new(){"register"} } }
		};
	}
	public class Role
	{
		[JsonConverter(typeof(PermissionConverter))]
		public PermissionFlags Permissions { get; set; } = PermissionFlags.None;
		/// <summary>
		/// 0:whitelist (block all by default), 1:blacklist (allow all by default).
		/// </summary>
		public byte CommandFilteringMode = 1;
		public HashSet<string> WhiteListedCommands { get; set; } = new HashSet<string>();
		public HashSet<string> BlackListedCommands { get; set; } = new HashSet<string>();

	}
	public class PermissionConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(((PermissionFlags)value).ToString());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return Enum.Parse(typeof(PermissionFlags), reader.ReadAsString());
		}

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(PermissionFlags);
		}
	}
}