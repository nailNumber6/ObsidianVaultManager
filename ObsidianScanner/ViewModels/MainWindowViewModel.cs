using Avalonia.Collections;
using ObsidianScanner.Services;

namespace ObsidianScanner.ViewModels
{
	public class MainWindowViewModel(IObsidianVaultProvider vaultProvider) : ViewModelBase
	{
		public AvaloniaList<ObsidianVaultViewModel> VaultVms { get; init; } = [.. vaultProvider.GetVaults()];
	}
}
