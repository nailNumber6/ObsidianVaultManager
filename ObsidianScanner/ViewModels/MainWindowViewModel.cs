using Avalonia.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ObsidianScanner.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public AvaloniaList<ObsidianVaultViewModel> VaultVms { get; init; }

		public MainWindowViewModel(IObsidianVaultProvider vaultProvider)
		{
			VaultVms = [.. vaultProvider.GetVaults()];
		}
	}

	public class ObsidianVaultViewModel(string name, string path)
	{
		public string Name { get; init; } = name;

		public string Path { get; init; } = path;

		public AvaloniaList<ObsidianCommunityPlugin> CommunityPlugins { get; init; }
	}

	public sealed record ObsidianCommunityPlugin(
		string Id,
		string Name,
		string Version,
		string Description,
		string Author,
		string AuthorUrl,
		bool IsDesktopOnly);

	public sealed record ObsidianCorePlugin(
		string Id,
		bool IsEnabled);

	public interface IObsidianVaultProvider
	{
		IEnumerable<ObsidianVaultViewModel> GetVaults();
	}

	public class ObsidianVaultProvider : IObsidianVaultProvider
	{
		const string ObsidianJsonFileName = "obsidian.json";

		readonly JsonSerializer _jsonSerializer;

		readonly string _obsidianConfigDirectory;

		public ObsidianVaultProvider()
		{
			_jsonSerializer = new JsonSerializer();

			_obsidianConfigDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"Obsidian");
		}

		public IEnumerable<ObsidianVaultViewModel> GetVaults()
		{
			if (Directory.Exists(_obsidianConfigDirectory))
			{
				if(File.Exists(Path.Combine(_obsidianConfigDirectory, ObsidianJsonFileName)))
				{
					string vaultsJson = File.ReadAllText(Path.Combine(_obsidianConfigDirectory, ObsidianJsonFileName));

					using var streamReader = new StreamReader(vaultsJson);
					using var reader = new JsonTextReader(streamReader);
					
					var obsidianConfig = _jsonSerializer.Deserialize<ObsidianConfig>(reader);

					if (obsidianConfig?.Vaults.Count < 1)
					{
						return Array.Empty<ObsidianVaultViewModel>();
					}
					List<ObsidianVaultViewModel> vaults = new List<ObsidianVaultViewModel>();
					foreach (var vault in obsidianConfig!.Vaults)
					{
						// TODO: Reading manifest.json for each vault to get the plugins
					}
				}
			}
			return Array.Empty<ObsidianVaultViewModel>();
		}
	}

	public class ObsidianConfig
	{
		[JsonProperty("vaults")]
		public Dictionary<string, ObsidianVault> Vaults { get; set; } = [];
	}

	public class ObsidianVault
	{
		[JsonProperty("path")]
		public string Path { get; set; } = null!;

		[JsonProperty("ts")]
		public long Timestamp { get; set; }
	}
}
