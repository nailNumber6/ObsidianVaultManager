using ObsidianScanner.ViewModels;
using System.Collections.Generic;

namespace ObsidianScanner.Services
{
	public interface IObsidianVaultProvider
	{
		IEnumerable<ObsidianVaultViewModel> GetVaults();
	}
}
