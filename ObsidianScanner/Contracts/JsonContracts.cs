using Newtonsoft.Json;
using System.Collections.Generic;

namespace ObsidianScanner.Contracts
{
	/// <summary>Shape of a community plugin <c>manifest.json</c> under <c>.obsidian/plugins/{id}/</c>.</summary>
	public sealed record ObsidianCommunityPlugin(
		[JsonProperty("id")]
		string? Id,
		[JsonProperty("name")]
		string? Name,
		[JsonProperty("version")]
		string? Version,
		[JsonProperty("description")]
		string? Description,
		[JsonProperty("author")]
		string? Author,
		[JsonProperty("authorUrl")]
		string? AuthorUrl,
		[JsonProperty("isDesktopOnly", DefaultValueHandling = DefaultValueHandling.Populate)]
		bool IsDesktopOnly = false);

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
		long Timestamp,
		[JsonProperty("open")]
		bool Open);
}
