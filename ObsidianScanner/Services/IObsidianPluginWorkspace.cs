using System.Collections.Generic;

namespace ObsidianScanner.Services
{
	public interface IObsidianPluginWorkspace
	{
		IReadOnlyList<VaultDescriptor> Vaults { get; }

		IReadOnlyList<AggregatedPlugin> Plugins { get; }

		void Refresh();

		void ActivatePlugin(string targetVaultPath, string pluginId, string sourceVaultPath, bool importPluginData);

		void DeactivatePlugin(string targetVaultPath, string pluginId);
	}
}
