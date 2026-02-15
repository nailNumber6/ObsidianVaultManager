using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ObsidianScanner.ViewModels;
using ObsidianScanner.Views;

namespace ObsidianScanner
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				//TODO: Use DI container to resolve dependencies.
				desktop.MainWindow = new MainWindow
				{
					DataContext = new MainWindowViewModel(new ObsidianVaultProvider(new JsonFileDeserializer())),
				};
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}