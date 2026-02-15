using Newtonsoft.Json;
using System.Collections.Generic;

namespace ObsidianScanner.Contracts
{
	public sealed record ObsidianCommunityPlugin(
	[JsonProperty("id")]
	string Id,
	[JsonProperty("name")]
	string Name,
	[JsonProperty("version")]
	string Version,
	[JsonProperty("description")]
	string Description,
	[JsonProperty("author")]
	string Author,
	[JsonProperty("authorUrl")]
	string AuthorUrl,
	[JsonProperty("isDesktopOnly")]
	bool IsDesktopOnly);

	public sealed record ObsidianCorePlugin(
	[JsonProperty("id")]
	string Id,
	[JsonProperty("isEnabled")]
	bool IsEnabled);

	public sealed record ObsidianConfig(
		[JsonProperty("vaults")]
		Dictionary<string, ObsidianVault> Vaults);

	public sealed record ObsidianVault(
		[JsonProperty("path")]
		string Path,
		[JsonProperty("ts")]
		long Timestamp);
	}
}
