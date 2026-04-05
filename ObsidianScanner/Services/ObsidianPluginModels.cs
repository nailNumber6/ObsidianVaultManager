using System.Collections.Generic;
using ObsidianScanner.Contracts;

namespace ObsidianScanner.Services
{
	public sealed record VaultDescriptor(string Id, string Path, string DisplayName);

	public sealed record VaultPluginSnapshot(
		string VaultId,
		string VaultPath,
		string VaultDisplayName,
		bool IsEnabled,
		bool PluginFolderExists,
		bool HasDataJson);

	public sealed record AggregatedPlugin(
		string Id,
		ObsidianCommunityPlugin? Manifest,
		IReadOnlyList<VaultPluginSnapshot> PerVault,
		string? PreferredSourceVaultPath,
		bool SourceHasDataJson);
}
