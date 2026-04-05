using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ObsidianScanner.Services;
using ReactiveUI;

namespace ObsidianScanner.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		readonly IObsidianPluginWorkspace _workspace;
		string _lastError = string.Empty;
		DateTimeOffset? _lastRefreshedAt;
		ObsidianPluginViewModel? _selectedPlugin;

		public MainWindowViewModel()
			: this(new ObsidianPluginWorkspace(new JsonFileDeserializer()))
		{
		}

		public MainWindowViewModel(IObsidianPluginWorkspace workspace)
		{
			_workspace = workspace;
			RefreshCommand = ReactiveCommand.Create(ReloadFromWorkspace);
			ReloadFromWorkspace();
		}

		public ObservableCollection<ObsidianPluginViewModel> Plugins { get; } = [];

		public ObsidianPluginViewModel? SelectedPlugin
		{
			get => _selectedPlugin;
			set
			{
				this.RaiseAndSetIfChanged(ref _selectedPlugin, value);
				this.RaisePropertyChanged(nameof(HasSelectedPlugin));
				this.RaisePropertyChanged(nameof(NoSelectedPlugin));
			}
		}

		public bool HasSelectedPlugin => SelectedPlugin is not null;

		public bool NoSelectedPlugin => SelectedPlugin is null;

		public string LastError
		{
			get => _lastError;
			private set
			{
				this.RaiseAndSetIfChanged(ref _lastError, value);
				this.RaisePropertyChanged(nameof(HasError));
			}
		}

		public bool HasError => !string.IsNullOrEmpty(LastError);

		public DateTimeOffset? LastRefreshedAt
		{
			get => _lastRefreshedAt;
			private set
			{
				this.RaiseAndSetIfChanged(ref _lastRefreshedAt, value);
				this.RaisePropertyChanged(nameof(LastRefreshedDisplay));
			}
		}

		public string LastRefreshedDisplay => LastRefreshedAt is { } t
			? $"Last refreshed: {t.ToLocalTime():g}"
			: string.Empty;

		public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

		void ReloadFromWorkspace()
		{
			string? keepPluginId = SelectedPlugin?.Id;
			LastError = string.Empty;
			try
			{
				_workspace.Refresh();
				LastRefreshedAt = DateTimeOffset.Now;
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
			}

			Plugins.Clear();
			foreach (var plugin in _workspace.Plugins)
			{
				Plugins.Add(new ObsidianPluginViewModel(_workspace, plugin, ReloadFromWorkspace, msg => LastError = msg));
			}

			SelectedPlugin = string.IsNullOrEmpty(keepPluginId)
				? null
				: Plugins.FirstOrDefault(p => p.Id == keepPluginId);
		}
	}
}
