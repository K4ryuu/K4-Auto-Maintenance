using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace K4ryuuMaintenance
{
	public sealed class PluginConfig : BasePluginConfig
	{
		[JsonPropertyName("Logs")]
		public bool Logs { get; set; } = true;

		[JsonPropertyName("Scripts")]
		public ScriptsClass Scripts { get; set; } = new ScriptsClass();
		public class ScriptsClass
		{
			[JsonPropertyName("BackupRound-Purge")]
			public BackupRoundPurgeClass BackupRoundPurge { get; set; } = new BackupRoundPurgeClass();
			public class BackupRoundPurgeClass
			{
				[JsonPropertyName("Enabled")]
				public bool Enabled { get; set; } = true;

				[JsonPropertyName("HoursToKeep")]
				public int Interval { get; set; } = 4;
			}

			[JsonPropertyName("Demo-Purge")]
			public DemoPurgeClass DemoPurge { get; set; } = new DemoPurgeClass();
			public class DemoPurgeClass
			{
				[JsonPropertyName("Enabled")]
				public bool Enabled { get; set; } = true;

				[JsonPropertyName("HoursToKeep")]
				public int Interval { get; set; } = 2 * 24;
			}

			[JsonPropertyName("CounterStrikeSharp-Junk-Purge")]
			public CounterStrikeSharpJunkPurgeClass CounterStrikeSharpJunkPurge { get; set; } = new CounterStrikeSharpJunkPurgeClass();
			public class CounterStrikeSharpJunkPurgeClass
			{
				[JsonPropertyName("Enabled")]
				public bool Enabled { get; set; } = true;

				[JsonPropertyName("HoursToKeep")]
				public int Interval { get; set; } = 7 * 24;
			}
		}

		[JsonPropertyName("ConfigVersion")]
		public override int Version { get; set; } = 1;
	}

	[MinimumApiVersion(227)]
	public class Plugin : BasePlugin, IPluginConfig<PluginConfig>
	{
		public override string ModuleName => "K4 Auto-Maintenance";
		public override string ModuleVersion => "1.0.0";
		public override string ModuleAuthor => "K4ryuu";

		public required PluginConfig Config { get; set; } = new PluginConfig();

		public void OnConfigParsed(PluginConfig config)
		{
			if (config.Version < Config.Version)
				base.Logger.LogWarning("Configuration version mismatch (Expected: {0} | Current: {1})", this.Config.Version, config.Version);

			this.Config = config;
		}

		public override void Load(bool hotReload)
		{
			Run_All();
			RegisterListener<Listeners.OnMapStart>((mapName) => Run_All());
		}

		public void Run_All()
		{
			Run_RoundBackupPurge();
			Run_CounterStrikeSharpJunkPurge();
			Run_DemoPurge();
		}

		public void Run_RoundBackupPurge()
		{
			if (!Config.Scripts.BackupRoundPurge.Enabled)
				return;

			string GameDirectory = Path.Combine(Server.GameDirectory, "csgo");

			string[] files = Directory.GetFiles(GameDirectory, "*.txt");
			int count = 0;

			foreach (string file in files)
			{
				if (file.Contains("backup_round"))
				{
					if (!IsFileOlderThan(file, Config.Scripts.BackupRoundPurge.Interval))
						continue;

					SafeDelete(file);
					count++;
				}
			}

			if (Config.Logs)
				base.Logger.LogInformation("Purging backup_round files complete. - {0} files removed.", count);
		}

		public void Run_DemoPurge()
		{
			if (!Config.Scripts.DemoPurge.Enabled)
				return;

			string GameDirectory = Path.Combine(Server.GameDirectory, "csgo");

			string[] files = Directory.GetFiles(GameDirectory, "*.dem");
			int count = 0;

			foreach (string file in files)
			{
				if (!IsFileOlderThan(file, Config.Scripts.DemoPurge.Interval))
					continue;

				SafeDelete(file);
				count++;
			}

			if (Config.Logs)
				base.Logger.LogInformation("Purging demo files complete. - {0} files removed.", count);
		}

		public void Run_CounterStrikeSharpJunkPurge()
		{
			if (!Config.Scripts.CounterStrikeSharpJunkPurge.Enabled)
				return;

			string cssDirectory = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "logs");
			if (!Directory.Exists(cssDirectory))
			{
				if (Config.Logs)
					base.Logger.LogInformation("CounterStrikeSharp directory not found at {0}", cssDirectory);
				return;
			}

			string[] files = Directory.GetFiles(cssDirectory, "*.txt");
			int count = 0;

			foreach (string file in files)
			{
				if (!IsFileOlderThan(file, Config.Scripts.CounterStrikeSharpJunkPurge.Interval))
					continue;

				SafeDelete(file);
				count++;
			}

			if (Config.Logs)
				base.Logger.LogInformation("Purging CounterStrikeSharp junk files complete. - {0} files removed.", count);
		}

		public static bool IsFileOlderThan(string filePath, int hours)
		{
			return (DateTime.Now - File.GetCreationTime(filePath)).TotalHours > hours;
		}

		public void SafeDelete(string filePath)
		{
			try { File.Delete(filePath); }
			catch (Exception ex) { base.Logger.LogInformation("failed to remove > {0} : {1}", filePath, ex.Message); }
		}
	}
}
