using Avalonia.Collections;
using ObsidianScanner.Contracts;

namespace ObsidianScanner.ViewModels
{
	public class ObsidianVaultViewModel(string id, string name, string path)
	{
		string Id { get; init; } = id;

		public string Name { get; init; } = name;

		public string Path { get; init; } = path;

		public AvaloniaList<ObsidianCommunityPlugin> CommunityPlugins { get; init; } = [];
	}
}
