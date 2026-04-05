using Microsoft.Extensions.DependencyInjection;
using ObsidianScanner.Services;
using ObsidianScanner.ViewModels;

namespace ObsidianScanner
{
	internal static class CompositionRoot
	{
		public static ServiceProvider Build()
		{
			var services = new ServiceCollection();
			services.AddSingleton<IFileDeserializer, JsonFileDeserializer>();
			services.AddSingleton<IObsidianPluginWorkspace, ObsidianPluginWorkspace>();
			services.AddTransient<MainWindowViewModel>();
			return services.BuildServiceProvider();
		}
	}
}
