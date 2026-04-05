using System;
using System.Collections.ObjectModel;
using ObsidianScanner.Services;

namespace ObsidianScanner.ViewModels
{
	public sealed class ObsidianPluginViewModel : ViewModelBase
	{
		public ObsidianPluginViewModel(
			IObsidianPluginWorkspace workspace,
			AggregatedPlugin plugin,
			Action reloadWorkspace,
			Action<string> setError)
		{
			Id = plugin.Id;
			Name = plugin.Manifest?.Name ?? plugin.Id;
			Version = plugin.Manifest?.Version ?? "—";
			Description = plugin.Manifest?.Description ?? "";
			Author = plugin.Manifest?.Author ?? "";

			foreach (var snap in plugin.PerVault)
			{
				VaultRows.Add(new VaultPluginRowViewModel(
					workspace,
					plugin.Id,
					snap,
					plugin.PreferredSourceVaultPath,
					plugin.SourceHasDataJson,
					reloadWorkspace,
					setError));
			}
		}

		public string Id { get; }

		public string Name { get; }

		public string Version { get; }

		public string Description { get; }

		public string Author { get; }

		public string ListTitle => string.IsNullOrEmpty(Version) || Version == "—"
			? Name
			: $"{Name} ({Version})";

		public ObservableCollection<VaultPluginRowViewModel> VaultRows { get; } = [];
	}
}
