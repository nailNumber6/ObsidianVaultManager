using ObsidianScanner.Contracts;
using ObsidianScanner.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace ObsidianScanner.Services
{
	public class ObsidianVaultProvider : IObsidianVaultProvider
	{
		#region Constants

		const string ObsidianJsonFileName = "obsidian.json";

		const string ManifestJsonFileName = "manifest.json";

		const string ObsidianFolderName = ".obsidian";

		const string CommunityPluginsFileName = "community-plugins.json";

		const string CommunityPluginFolderName = "plugins";

		#endregion

		#region Fields

		readonly IFileDeserializer _fileDeserializer;

		readonly string _obsidianConfigDirectory;

		#endregion

		public ObsidianVaultProvider(IFileDeserializer fileDeserializer)
		{
			_fileDeserializer = fileDeserializer;
			_obsidianConfigDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"Obsidian");
		}

		#region Methods

		public IEnumerable<ObsidianVaultViewModel> GetVaults()
		{
			if (Directory.Exists(_obsidianConfigDirectory))
			{
				if (File.Exists(
					Path.Combine(_obsidianConfigDirectory, ObsidianJsonFileName)))
				{
					var obsidianConfig = LoadObsidianConfig();

					if (obsidianConfig?.Vaults.Count < 1)
					{
						return [];
					}
					List<ObsidianVaultViewModel> vaultVms = [];
					foreach (var vault in obsidianConfig!.Vaults)
					{
						vaultVms.Add(CreateVaultViewModel(vault.Key, vault.Value));
					}
					return vaultVms;
				}
			}
			return [];
		}

		ObsidianConfig LoadObsidianConfig()
		{
			string configPath = Path.Combine(_obsidianConfigDirectory, ObsidianJsonFileName);

			//TODO: Perhaps creating obsidian config file if it doesn't exist, or at least prompting the user to create one.
			return _fileDeserializer.Deserialize<ObsidianConfig>(configPath) 
				?? throw new InvalidDataException($"Failed to deserialize Obsidian configuration from file at path {configPath}");
		}

		/// <summary>
		/// Creates and returns a view model for the specified Obsidian vault.
		/// </summary>
		/// <remarks>The method verifies the existence of the manifest file in the vault's directory before creating.</remarks>
		/// <param name="vaultId">The unique identifier of the vault.</param>
		/// <param name="vault">The ObsidianVault instance.</param>
		/// <returns>An ObsidianVaultViewModel instance populated with data from the specified vault.</returns>
		/// <exception cref="FileNotFoundException"/>
		ObsidianVaultViewModel CreateVaultViewModel(string vaultId, ObsidianVault vault)
		{
			string communityPluginsPath = Path.Combine(vault.Path, ObsidianFolderName, CommunityPluginsFileName);

			string vaultName = GetFolderNameFromPath(vault.Path)!;
			List<ObsidianCommunityPlugin> communityPlugins = [];

			if (!File.Exists(communityPluginsPath))
			{
				return new ObsidianVaultViewModel(vaultId, vaultName, vault.Path);
			}

			var pluginsList = _fileDeserializer.Deserialize<string[]>(communityPluginsPath);

			if (pluginsList is null || pluginsList?.Length < 1)
			{
				return new ObsidianVaultViewModel(vaultId, vaultName, vault.Path);

				//TODO: Make a functionality to scan for plugins even if the manifest file is missing or empty.
			}

			foreach (var plugin in pluginsList!)
			{
				string pluginManifestPath = Path.Combine(vault.Path, ObsidianFolderName, CommunityPluginFolderName, plugin, ManifestJsonFileName);

				if (!File.Exists(pluginManifestPath))
				{
					continue;
				}

				var pluginManifest = _fileDeserializer.Deserialize<ObsidianCommunityPlugin>(pluginManifestPath);

				if (pluginManifest is not null)
				{
					communityPlugins.Add(pluginManifest);
				}
				//TODO: Add logging for failed plugin manifest deserialization.
			}

			return new ObsidianVaultViewModel(vaultId, vaultName, vault.Path)
			{
				CommunityPlugins = [.. communityPlugins]
			};
		}

		/// <summary>
		/// Gets the name of the folder from the specified directory path.
		/// </summary>
		/// <param name="path">The path of the directory from which to extract the folder name. Must be a valid directory path.</param>
		/// <returns>The name of the folder extracted from the provided path.</returns>
		/// <exception cref="ArgumentException"/>
		string GetFolderNameFromPath(string path)
		{
			FileAttributes attributes = File.GetAttributes(path);

			if (!attributes.HasFlag(FileAttributes.Directory))
			{
				throw new ArgumentException($"The provided path '{path}' is not a directory.");
			}

			return path.Split(Path.DirectorySeparatorChar)[^1];
		}

		#endregion
	}
}
