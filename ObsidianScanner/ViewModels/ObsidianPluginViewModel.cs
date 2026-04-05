using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ObsidianScanner.Services;
using ReactiveUI;

namespace ObsidianScanner.ViewModels
{
	public sealed class ObsidianPluginViewModel : ViewModelBase
	{
		readonly IObsidianPluginWorkspace _workspace;
		readonly Action _reloadWorkspace;
		readonly Action<string> _setError;
		string _vaultSearchText = string.Empty;

		public ObsidianPluginViewModel(
			IObsidianPluginWorkspace workspace,
			AggregatedPlugin plugin,
			Action reloadWorkspace,
			Action<string> setError)
		{
			_workspace = workspace;
			_reloadWorkspace = reloadWorkspace;
			_setError = setError;

			Id = plugin.Id;
			Name = plugin.Manifest?.Name ?? plugin.Id;
			Version = plugin.Manifest?.Version ?? "—";
			Description = plugin.Manifest?.Description ?? "";
			Author = plugin.Manifest?.Author ?? "";

			var rows = plugin.PerVault
				.Select(snap => new VaultPluginRowViewModel(
					workspace,
					plugin.Id,
					snap,
					plugin.PreferredSourceVaultPath,
					plugin.SourceHasDataJson,
					reloadWorkspace,
					setError,
					RaiseBulkActivateAvailabilityChanged))
				.ToList();

			// Uninstalled vaults first, then A–Z by vault name within each group.
			foreach (var row in rows.OrderBy(r => r.IsInstalled).ThenBy(r => r.VaultDisplayName, StringComparer.OrdinalIgnoreCase))
			{
				AllVaultRows.Add(row);
				row.WhenAnyValue(r => r.IsSelected)
					.Subscribe(_ => RaiseBulkActivateAvailabilityChanged());
			}

			ApplyVaultFilter();

			ActivateSelectedCommand = ReactiveCommand.CreateFromTask(
				ActivateSelectedVaultsAsync,
				this.WhenAnyValue(x => x.CanActivateSelected));
		}

		/// <summary>All vault rows for this plugin (fixed order: not installed first, then by name).</summary>
		public ObservableCollection<VaultPluginRowViewModel> AllVaultRows { get; } = [];

		/// <summary>Subset of <see cref="AllVaultRows"/> after applying <see cref="VaultSearchText"/>.</summary>
		public ObservableCollection<VaultPluginRowViewModel> FilteredVaultRows { get; } = [];

		public string VaultSearchText
		{
			get => _vaultSearchText;
			set
			{
				if (_vaultSearchText == value)
				{
					return;
				}

				this.RaiseAndSetIfChanged(ref _vaultSearchText, value);
				ApplyVaultFilter();
			}
		}

		public bool CanActivateSelected => AllVaultRows.Any(r => r.IsSelected && r.CanActivate);

		public ReactiveCommand<Unit, Unit> ActivateSelectedCommand { get; }

		public string Id { get; }

		public string Name { get; }

		public string Version { get; }

		public string Description { get; }

		public string Author { get; }

		public string ListTitle => string.IsNullOrEmpty(Version) || Version == "—"
			? Name
			: $"{Name} ({Version})";

		void RaiseBulkActivateAvailabilityChanged()
		{
			this.RaisePropertyChanged(nameof(CanActivateSelected));
		}

		void ApplyVaultFilter()
		{
			string q = (VaultSearchText ?? string.Empty).Trim();
			List<VaultPluginRowViewModel> matches = q.Length > 0
				? AllVaultRows.Where(r => VaultSearchMatchesWordPrefix(r.VaultDisplayName, r.VaultPath, q)).ToList()
				: [.. AllVaultRows];

			FilteredVaultRows.Clear();
			foreach (var row in matches)
			{
				FilteredVaultRows.Add(row);
			}
		}

		/// <summary>
		/// Matches the query against <strong>word starts</strong> only (not arbitrary substrings).
		/// Words come from display name and path: path separators become breaks; camelCase / PascalCase adds breaks
		/// (e.g. SomeDing → Some, Ding so "d" matches Ding).
		/// </summary>
		internal static bool VaultSearchMatchesWordPrefix(string displayName, string vaultPath, string query)
		{
			string q = query.Trim();
			if (q.Length == 0)
			{
				return true;
			}

			string haystack = $"{displayName} {vaultPath}";
			return EnumerateSearchTokens(haystack).Any(t => t.StartsWith(q, StringComparison.OrdinalIgnoreCase));
		}

		static IEnumerable<string> EnumerateSearchTokens(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				yield break;
			}

			string s = text.Replace('\\', ' ').Replace('/', ' ');
			// lower/digit then Upper → word boundary (e.g. vaultDiary → vault, Diary)
			s = BoundaryLowerThenUpper().Replace(s, " ");
			// ACRONYM then Word (e.g. XMLParser → XML, Parser)
			s = BoundaryAcronymThenWord().Replace(s, " ");

			foreach (string segment in NonLetterDigitRuns().Split(s))
			{
				if (segment.Length > 0)
				{
					yield return segment;
				}
			}
		}

		static Regex BoundaryLowerThenUpper() => _boundaryLowerThenUpper ??= new Regex(@"(?<=[a-z0-9\p{Ll}])(?=\p{Lu})", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		static Regex BoundaryAcronymThenWord() => _boundaryAcronymThenWord ??= new Regex(@"(?<=\p{Lu})(?=\p{Lu}\p{Ll})", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		static Regex NonLetterDigitRuns() => _nonLetterDigitRuns ??= new Regex(@"[^\p{L}\p{Nd}]+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		static Regex? _boundaryLowerThenUpper;

		static Regex? _boundaryAcronymThenWord;

		static Regex? _nonLetterDigitRuns;

		async Task ActivateSelectedVaultsAsync()
		{
			_setError(string.Empty);
			var targets = AllVaultRows.Where(r => r.IsSelected && r.CanActivate).ToList();
			if (targets.Count == 0)
			{
				return;
			}

			try
			{
				await Task.Run(() =>
				{
					foreach (var r in targets)
					{
						_workspace.ActivatePlugin(r.VaultPath, Id, r.GetActivateSourceVaultPath(), r.ImportPluginData);
					}
				}).ConfigureAwait(true);
				_reloadWorkspace();
			}
			catch (Exception ex)
			{
				_setError(ex.Message);
			}
		}
	}
}
