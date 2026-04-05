using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ObsidianScanner.ViewModels;
using ObsidianScanner.Views;

namespace ObsidianScanner
{
	public partial class App : Application
	{
		ServiceProvider? _services;

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				_services = CompositionRoot.Build();
				desktop.Exit += (_, _) => _services?.Dispose();
				desktop.MainWindow = new MainWindow
				{
					DataContext = _services.GetRequiredService<MainWindowViewModel>(),
				};
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
