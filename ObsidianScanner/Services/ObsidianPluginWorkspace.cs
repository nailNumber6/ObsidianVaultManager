using ObsidianScanner.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ObsidianScanner.Services
{
	public sealed class ObsidianPluginWorkspace : IObsidianPluginWorkspace
	{
		const string ObsidianJsonFileName = "obsidian.json";
		const string ManifestJsonFileName = "manifest.json";
		const string ObsidianFolderName = ".obsidian";
		const string CommunityPluginsFileName = "community-plugins.json";
		const string CommunityPluginFolderName = "plugins";
		const string DataJsonFileName = "data.json";

		readonly IFileDeserializer _fileDeserializer;
		readonly string _obsidianConfigDirectory;

		List<VaultDescriptor> _vaults = [];
		List<AggregatedPlugin> _plugins = [];

		public ObsidianPluginWorkspace(IFileDeserializer fileDeserializer)
		{
			_fileDeserializer = fileDeserializer;
			_obsidianConfigDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Obsidian");
		}

		public IReadOnlyList<VaultDescriptor> Vaults => _vaults;

		public IReadOnlyList<AggregatedPlugin> Plugins => _plugins;

		public void Refresh()
		{
			_vaults = LoadVaultDescriptors();
			if (_vaults.Count == 0)
			{
				_plugins = [];
				return;
			}

			var perVaultScans = new List<(VaultDescriptor Vault, HashSet<string> PluginIds, HashSet<string> Enabled, Dictionary<string, (bool Folder, bool DataJson)> Details)>();
			foreach (var vault in _vaults)
			{
				perVaultScans.Add(ScanVaultPlugins(vault));
			}

			var allPluginIds = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var scan in perVaultScans)
			{
				foreach (var id in scan.PluginIds)
				{
					allPluginIds.Add(id);
				}
			}

			var manifestById = new Dictionary<string, ObsidianCommunityPlugin?>(StringComparer.Ordinal);
			foreach (var id in allPluginIds)
			{
				foreach (var scan in perVaultScans)
				{
					if (!scan.Details.TryGetValue(id, out var d) || !d.Folder)
					{
						continue;
					}

					string manifestPath = Path.Combine(
						scan.Vault.Path,
						ObsidianFolderName,
						CommunityPluginFolderName,
						id,
						ManifestJsonFileName);
					if (!File.Exists(manifestPath))
					{
						continue;
					}

					try
					{
						var manifest = _fileDeserializer.Deserialize<ObsidianCommunityPlugin>(manifestPath);
						if (manifest is not null)
						{
							manifestById[id] = manifest;
							break;
						}
					}
					catch (JsonException ex)
					{
#if DEBUG
						Debug.WriteLine($"Manifest deserialize failed for '{id}' at '{manifestPath}': {ex.Message}");
#endif
					}
				}
			}

			var aggregated = new List<AggregatedPlugin>();
			foreach (var id in allPluginIds)
			{
				manifestById.TryGetValue(id, out var manifest);

				var snapshots = new List<VaultPluginSnapshot>();
				string? preferredSource = null;
				foreach (var scan in perVaultScans)
				{
					scan.Details.TryGetValue(id, out var detail);
					bool folder = detail.Folder;
					bool dataJson = detail.DataJson;
					bool enabled = scan.Enabled.Contains(id);

					snapshots.Add(new VaultPluginSnapshot(
						scan.Vault.Id,
						scan.Vault.Path,
						scan.Vault.DisplayName,
						enabled,
						folder,
						dataJson));

					if (preferredSource is null && folder && HasUsablePluginFolder(scan.Vault.Path, id))
					{
						preferredSource = scan.Vault.Path;
					}
				}

				bool sourceHasData = preferredSource is not null
					&& File.Exists(Path.Combine(
						preferredSource,
						ObsidianFolderName,
						CommunityPluginFolderName,
						id,
						DataJsonFileName));

				aggregated.Add(new AggregatedPlugin(
					id,
					manifest,
					snapshots,
					preferredSource,
					sourceHasData));
			}

			_plugins = aggregated;
		}

		public void ActivatePlugin(string targetVaultPath, string pluginId, string sourceVaultPath, bool importPluginData)
		{
			string sourcePluginDir = Path.Combine(
				sourceVaultPath,
				ObsidianFolderName,
				CommunityPluginFolderName,
				pluginId);
			if (!Directory.Exists(sourcePluginDir))
			{
				throw new DirectoryNotFoundException($"Source plugin folder not found: {sourcePluginDir}");
			}

			string targetPluginDir = Path.Combine(
				targetVaultPath,
				ObsidianFolderName,
				CommunityPluginFolderName,
				pluginId);

			if (!Directory.Exists(targetPluginDir))
			{
				CopyDirectory(sourcePluginDir, targetPluginDir);
				if (!importPluginData)
				{
					string dataPath = Path.Combine(targetPluginDir, DataJsonFileName);
					if (File.Exists(dataPath))
					{
						File.Delete(dataPath);
					}
				}
			}
			else
			{
				if (importPluginData)
				{
					string srcData = Path.Combine(sourcePluginDir, DataJsonFileName);
					string dstData = Path.Combine(targetPluginDir, DataJsonFileName);
					if (File.Exists(srcData))
					{
						File.Copy(srcData, dstData, overwrite: true);
					}
				}
			}

			AddToCommunityPlugins(targetVaultPath, pluginId);
		}

		public void DeactivatePlugin(string targetVaultPath, string pluginId)
		{
			RemoveFromCommunityPlugins(targetVaultPath, pluginId);
		}

		List<VaultDescriptor> LoadVaultDescriptors()
		{
			string configPath = Path.Combine(_obsidianConfigDirectory, ObsidianJsonFileName);
			if (!Directory.Exists(_obsidianConfigDirectory) || !File.Exists(configPath))
			{
				return [];
			}

			ObsidianConfig? config;
			try
			{
				config = _fileDeserializer.Deserialize<ObsidianConfig>(configPath);
			}
			catch (JsonException)
			{
				return [];
			}

			if (config?.Vaults is null || config.Vaults.Count < 1)
			{
				return [];
			}

			var list = new List<VaultDescriptor>();
			foreach (var pair in config.Vaults)
			{
				string vaultPath = pair.Value.Path;
				if (!IsDirectory(vaultPath))
				{
					continue;
				}

				string displayName = GetFolderNameFromPath(vaultPath);
				list.Add(new VaultDescriptor(pair.Key, vaultPath, displayName));
			}

			return list;
		}

		(VaultDescriptor Vault, HashSet<string> PluginIds, HashSet<string> Enabled, Dictionary<string, (bool Folder, bool DataJson)> Details) ScanVaultPlugins(
			VaultDescriptor vault)
		{
			var enabled = new HashSet<string>(StringComparer.Ordinal);
			string communityPath = Path.Combine(vault.Path, ObsidianFolderName, CommunityPluginsFileName);
			if (File.Exists(communityPath))
			{
				try
				{
					var ids = _fileDeserializer.Deserialize<string[]>(communityPath);
					if (ids is not null)
					{
						foreach (var id in ids.Where(s => !string.IsNullOrWhiteSpace(s)))
						{
							enabled.Add(id.Trim());
						}
					}
				}
				catch (JsonException)
				{
					// ignore corrupt community-plugins.json
				}
			}

			var pluginIds = new HashSet<string>(enabled, StringComparer.Ordinal);
			string pluginsRoot = Path.Combine(vault.Path, ObsidianFolderName, CommunityPluginFolderName);
			var details = new Dictionary<string, (bool Folder, bool DataJson)>(StringComparer.Ordinal);
			if (Directory.Exists(pluginsRoot))
			{
				foreach (string dir in Directory.GetDirectories(pluginsRoot))
				{
					string id = Path.GetFileName(dir);
					if (string.IsNullOrEmpty(id))
					{
						continue;
					}

					pluginIds.Add(id);
					bool hasData = File.Exists(Path.Combine(dir, DataJsonFileName));
					details[id] = (true, hasData);
				}
			}

			foreach (var id in pluginIds.Where(id => !details.ContainsKey(id)))
			{
				details[id] = (false, false);
			}

			return (vault, pluginIds, enabled, details);
		}

		static bool HasUsablePluginFolder(string vaultPath, string pluginId)
		{
			string pluginDir = Path.Combine(vaultPath, ObsidianFolderName, CommunityPluginFolderName, pluginId);
			if (!Directory.Exists(pluginDir))
			{
				return false;
			}

			string manifestPath = Path.Combine(pluginDir, ManifestJsonFileName);
			if (File.Exists(manifestPath))
			{
				return true;
			}

			return Directory.EnumerateFileSystemEntries(pluginDir).Any();
		}

		void AddToCommunityPlugins(string vaultPath, string pluginId)
		{
			string communityPath = Path.Combine(vaultPath, ObsidianFolderName, CommunityPluginsFileName);
			Directory.CreateDirectory(Path.Combine(vaultPath, ObsidianFolderName));

			var list = ReadCommunityPluginIds(communityPath);
			if (list.Any(id => string.Equals(id, pluginId, StringComparison.Ordinal)))
			{
				return;
			}

			list.Add(pluginId);
			_fileDeserializer.Serialize(communityPath, list.ToArray());
		}

		void RemoveFromCommunityPlugins(string vaultPath, string pluginId)
		{
			string communityPath = Path.Combine(vaultPath, ObsidianFolderName, CommunityPluginsFileName);
			if (!File.Exists(communityPath))
			{
				return;
			}

			var list = ReadCommunityPluginIds(communityPath);
			int removed = list.RemoveAll(id => string.Equals(id, pluginId, StringComparison.Ordinal));
			if (removed == 0)
			{
				return;
			}

			_fileDeserializer.Serialize(communityPath, list.ToArray());
		}

		List<string> ReadCommunityPluginIds(string communityPath)
		{
			if (!File.Exists(communityPath))
			{
				return [];
			}

			try
			{
				var ids = _fileDeserializer.Deserialize<string[]>(communityPath);
				if (ids is null)
				{
					return [];
				}

				return ids.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
			}
			catch (JsonException)
			{
				return [];
			}
		}

		static void CopyDirectory(string sourceDir, string destDir)
		{
			Directory.CreateDirectory(destDir);
			foreach (string file in Directory.GetFiles(sourceDir))
			{
				File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
			}

			foreach (string dir in Directory.GetDirectories(sourceDir))
			{
				CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
			}
		}

		static bool IsDirectory(string path)
		{
			try
			{
				return Directory.Exists(path);
			}
			catch
			{
				return false;
			}
		}

		static string GetFolderNameFromPath(string path)
		{
			var attributes = File.GetAttributes(path);
			if (!attributes.HasFlag(FileAttributes.Directory))
			{
				throw new ArgumentException($"The provided path '{path}' is not a directory.");
			}

			return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[^1];
		}
	}
}
