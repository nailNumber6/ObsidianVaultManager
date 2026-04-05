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
		readonly VaultPluginSnapshot _snapshot;
		readonly string? _preferredSourceVaultPath;
		readonly bool _aggregateSourceHasDataJson;
		readonly string _pluginId;

		bool _importPluginData;

		public VaultPluginRowViewModel(
			IObsidianPluginWorkspace workspace,
			string pluginId,
			VaultPluginSnapshot snapshot,
			string? preferredSourceVaultPath,
			bool aggregateSourceHasDataJson,
			Action reloadWorkspace,
			Action<string> setError)
		{
			_workspace = workspace;
			_pluginId = pluginId;
			_snapshot = snapshot;
			_preferredSourceVaultPath = preferredSourceVaultPath;
			_aggregateSourceHasDataJson = aggregateSourceHasDataJson;
			_reloadWorkspace = reloadWorkspace;
			_setError = setError;

			ActivateCommand = ReactiveCommand.CreateFromTask(ActivateAsync);
			DeactivateCommand = ReactiveCommand.CreateFromTask(DeactivateAsync);
		}

		public string VaultDisplayName => _snapshot.VaultDisplayName;

		public bool IsEnabled => _snapshot.IsEnabled;

		public bool IsInstalled => _snapshot.PluginFolderExists;

		public bool ShowImportDataOption => !IsEnabled && _aggregateSourceHasDataJson;

		public bool ImportPluginData
		{
			get => _importPluginData;
			set => this.RaiseAndSetIfChanged(ref _importPluginData, value);
		}

		public bool CanActivate => !IsEnabled
			&& (_snapshot.PluginFolderExists || !string.IsNullOrEmpty(_preferredSourceVaultPath));

		public bool CanDeactivate => IsEnabled;

		public ReactiveCommand<Unit, Unit> ActivateCommand { get; }

		public ReactiveCommand<Unit, Unit> DeactivateCommand { get; }

		string ResolveSourceVaultPath()
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

		async Task ActivateAsync()
		{
			_setError(string.Empty);
			try
			{
				await Task.Run(() =>
				{
					string source = ResolveSourceVaultPath();
					_workspace.ActivatePlugin(_snapshot.VaultPath, _pluginId, source, ImportPluginData);
				}).ConfigureAwait(true);
				_reloadWorkspace();
			}
			catch (Exception ex)
			{
				_setError(ex.Message);
			}
		}

		async Task DeactivateAsync()
		{
			_setError(string.Empty);
			try
			{
				await Task.Run(() => _workspace.DeactivatePlugin(_snapshot.VaultPath, _pluginId))
					.ConfigureAwait(true);
				_reloadWorkspace();
			}
			catch (Exception ex)
			{
				_setError(ex.Message);
			}
		}
	}
}
