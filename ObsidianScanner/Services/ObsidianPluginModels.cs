using System.Collections.Generic;
using ObsidianScanner.Contracts;

namespace ObsidianScanner.Services
{
	public sealed record VaultDescriptor(string Id, string Path, string DisplayName);

	/// <param name="ListedInCommunityPlugins">Plugin id is listed in <c>community-plugins.json</c>.</param>
	/// <param name="VaultAllowsCommunityPlugins">False when Obsidian Restricted mode is on (<c>app.json</c> has <c>restrictedMode: true</c>).</param>
	/// <param name="DataJsonPropertyCount">Recursive count of JSON object property entries in <c>data.json</c> (0 if missing or invalid).</param>
	public sealed record VaultPluginSnapshot(
		string VaultId,
		string VaultPath,
		string VaultDisplayName,
		bool ListedInCommunityPlugins,
		bool VaultAllowsCommunityPlugins,
		bool PluginFolderExists,
		bool HasDataJson,
		int DataJsonPropertyCount);

	public sealed record AggregatedPlugin(
		string Id,
		ObsidianCommunityPlugin? Manifest,
		IReadOnlyList<VaultPluginSnapshot> PerVault,
		string? PreferredSourceVaultPath,
		bool SourceHasDataJson);
}
