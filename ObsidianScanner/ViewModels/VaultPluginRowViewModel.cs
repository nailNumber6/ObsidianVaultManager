using System;
using System.Reactive;
using System.Threading.Tasks;
using ObsidianScanner.Services;
using ReactiveUI;

namespace ObsidianScanner.ViewModels
{
	public sealed class VaultPluginRowViewModel : ViewModelBase
	{
		readonly IObsidianPluginWorkspace _workspace;
		readonly Action _reloadWorkspace;
		readonly Action<string> _setError;
		readonly Action? _onPanelInteractionChanged;
		readonly VaultPluginSnapshot _snapshot;
		readonly string? _preferredSourceVaultPath;
		readonly bool _aggregateSourceHasDataJson;
		readonly string _pluginId;

		bool _importPluginData;
		bool _isSelected;

		public VaultPluginRowViewModel(
			IObsidianPluginWorkspace workspace,
			string pluginId,
			VaultPluginSnapshot snapshot,
			string? preferredSourceVaultPath,
			bool aggregateSourceHasDataJson,
			Action reloadWorkspace,
			Action<string> setError,
			Action? onPanelInteractionChanged = null)
		{
			_workspace = workspace;
			_pluginId = pluginId;
			_snapshot = snapshot;
			_preferredSourceVaultPath = preferredSourceVaultPath;
			_aggregateSourceHasDataJson = aggregateSourceHasDataJson;
			_reloadWorkspace = reloadWorkspace;
			_setError = setError;
			_onPanelInteractionChanged = onPanelInteractionChanged;

			ActivateCommand = ReactiveCommand.CreateFromTask(() => RunActivateAsync(reloadAfter: true));
			DeactivateCommand = ReactiveCommand.CreateFromTask(() => RunDeactivateAsync(reloadAfter: true));
		}

		public string VaultPath => _snapshot.VaultPath;

		public string VaultDisplayName => _snapshot.VaultDisplayName;

		public bool ListedInCommunityPlugins => _snapshot.ListedInCommunityPlugins;

		public bool VaultAllowsCommunityPlugins => _snapshot.VaultAllowsCommunityPlugins;

		/// <summary>True when the plugin is listed and Obsidian is allowed to load community plugins (not Restricted mode).</summary>
		public bool IsPluginActiveInObsidian =>
			_snapshot.ListedInCommunityPlugins && _snapshot.VaultAllowsCommunityPlugins;

		public bool ShowRestrictedModeHint =>
			_snapshot.ListedInCommunityPlugins && !_snapshot.VaultAllowsCommunityPlugins;

		public bool IsInstalled => _snapshot.PluginFolderExists;

		public int DataJsonPropertyCount => _snapshot.DataJsonPropertyCount;

		public bool ShowImportDataOption =>
			!_snapshot.ListedInCommunityPlugins && _aggregateSourceHasDataJson;

		public bool ImportPluginData
		{
			get => _importPluginData;
			set => this.RaiseAndSetIfChanged(ref _importPluginData, value);
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (!this.RaiseAndSetIfChanged(ref _isSelected, value))
				{
					return;
				}

				_onPanelInteractionChanged?.Invoke();
			}
		}

		public bool CanActivate =>
			!_snapshot.ListedInCommunityPlugins
			&& (_snapshot.PluginFolderExists || !string.IsNullOrEmpty(_preferredSourceVaultPath));

		public bool CanDeactivate => _snapshot.ListedInCommunityPlugins;

		public ReactiveCommand<Unit, Unit> ActivateCommand { get; }

		public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }

		public string GetActivateSourceVaultPath()
		{
			if (_snapshot.PluginFolderExists)
			{
				return _snapshot.VaultPath;
			}

			if (!string.IsNullOrEmpty(_preferredSourceVaultPath))
			{
				return _preferredSourceVaultPath;
			}

			throw new InvalidOperationException("No source vault is available for this plugin.");
		}

		public async Task RunActivateAsync(bool reloadAfter)
		{
			_setError(string.Empty);
			try
			{
				await Task.Run(() =>
				{
					string source = GetActivateSourceVaultPath();
					_workspace.ActivatePlugin(_snapshot.VaultPath, _pluginId, source, ImportPluginData);
				}).ConfigureAwait(true);
				if (reloadAfter)
				{
					_reloadWorkspace();
				}
			}
			catch (Exception ex)
			{
				_setError(ex.Message);
			}
		}

		public async Task RunDeactivateAsync(bool reloadAfter)
		{
			_setError(string.Empty);
			try
			{
				await Task.Run(() => _workspace.DeactivatePlugin(_snapshot.VaultPath, _pluginId))
					.ConfigureAwait(true);
				if (reloadAfter)
				{
					_reloadWorkspace();
				}
			}
			catch (Exception ex)
			{
				_setError(ex.Message);
			}
		}
	}
}
