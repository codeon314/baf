using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents a complete browser automation and data scraping project, including URL match rules,
    /// <see cref="AutomationSequence"/> workflows, and <see cref="ScrapeDefinition"/> configurations.
    /// </summary>
    public class AutomationProject
    {
        /// <summary>
        /// Gets the file dialog filter used when opening or saving automation project files.
        /// </summary>
        public const string FileDialogFilter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";

        /// <summary>
        /// Gets the default file extension used for automation project files.
        /// </summary>
        public const string DefaultFileExtension = "json";

        /// <summary>
        /// Gets or sets whether the generated userscript should match all URLs (<c>*://*/*</c>).
        /// </summary>
        [JsonPropertyName("matchAnySite")]
        public bool MatchAnySite { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of explicit URL match patterns when <see cref="MatchAnySite"/> is <c>false</c>.
        /// </summary>
        [JsonPropertyName("matchUrls")]
        public List<string> MatchUrls { get; set; } = new();

        /// <summary>
        /// Gets or sets the ordered collection of <see cref="AutomationSequence"/> workflows in this project.
        /// </summary>
        [JsonPropertyName("sequences")]
        public List<AutomationSequence> Sequences { get; set; } = new();

        /// <summary>
        /// Gets or sets the ordered collection of <see cref="ScrapeDefinition"/> configurations in this project.
        /// </summary>
        [JsonPropertyName("scrapes")]
        public List<ScrapeDefinition> Scrapes { get; set; } = new();

        /// <summary>
        /// Returns all active <see cref="AutomationSequence"/> items in the project.
        /// </summary>
        public IReadOnlyList<AutomationSequence> GetActiveSequences()
        {
            return Sequences.Where(s => s.IsActive).ToList();
        }

        /// <summary>
        /// Returns all active <see cref="ScrapeDefinition"/> items in the project.
        /// </summary>
        public IReadOnlyList<ScrapeDefinition> GetActiveScrapes()
        {
            return Scrapes.Where(s => s.IsActive).ToList();
        }

        /// <summary>
        /// Gets whether this project contains at least one <see cref="AutomationSequence"/>.
        /// </summary>
        [JsonIgnore]
        public bool HasSequences => Sequences != null && Sequences.Count > 0;

        /// <summary>
        /// Gets whether this project contains at least one <see cref="ScrapeDefinition"/>.
        /// </summary>
        [JsonIgnore]
        public bool HasScrapes => Scrapes != null && Scrapes.Count > 0;

        /// <summary>
        /// Gets whether this project has at least one active <see cref="AutomationSequence"/>.
        /// </summary>
        [JsonIgnore]
        public bool HasActiveSequences => Sequences != null && Sequences.Any(s => s.IsActive);

        /// <summary>
        /// Gets whether this project has at least one active <see cref="ScrapeDefinition"/>.
        /// </summary>
        [JsonIgnore]
        public bool HasActiveScrapes => Scrapes != null && Scrapes.Any(s => s.IsActive);

        /// <summary>
        /// Gets whether this project has valid URL match rules configured (either <see cref="MatchAnySite"/> is true or at least one URL is present in <see cref="MatchUrls"/>).
        /// </summary>
        [JsonIgnore]
        public bool HasValidMatchRules => MatchAnySite || (MatchUrls != null && MatchUrls.Any(u => !string.IsNullOrWhiteSpace(u)));

        /// <summary>
        /// Resets this project to its default empty state.
        /// </summary>
        public void Reset()
        {
            MatchAnySite = true;
            MatchUrls.Clear();
            Sequences.Clear();
            Scrapes.Clear();
        }

        /// <summary>
        /// Adds a trimmed URL pattern to <see cref="MatchUrls"/> if it is not empty.
        /// </summary>
        public string? AddMatchUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            string trimmed = url.Trim();
            MatchUrls.Add(trimmed);
            return trimmed;
        }

        /// <summary>
        /// Updates the URL pattern at the specified <paramref name="index"/> in <see cref="MatchUrls"/>.
        /// </summary>
        public bool UpdateMatchUrl(int index, string? url)
        {
            if (index < 0 || index >= MatchUrls.Count || string.IsNullOrWhiteSpace(url))
                return false;

            MatchUrls[index] = url.Trim();
            return true;
        }

        /// <summary>
        /// Removes the specified URL pattern from <see cref="MatchUrls"/>.
        /// </summary>
        public bool RemoveMatchUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return MatchUrls.Remove(url);
        }

        /// <summary>
        /// Returns the names of all configured <see cref="AutomationSequence"/> items in the project.
        /// </summary>
        public IReadOnlyList<string> GetSequenceNames()
        {
            return Sequences
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Returns a list of sequence names prefixed with <paramref name="noneLabel"/> suitable for populating pre/post-scrape sequence dropdown controls.
        /// </summary>
        public IReadOnlyList<string> GetSequenceDropdownOptions(string noneLabel = "(None)")
        {
            var options = new List<string>(Sequences.Count + 1) { noneLabel };
            options.AddRange(GetSequenceNames());
            return options;
        }

        /// <summary>
        /// Resolves a configured sequence name against the sequences currently present in this project, returning <paramref name="noneLabel"/> if not found.
        /// </summary>
        public string ResolveSequenceDropdownSelection(string? sequenceName, string noneLabel = "(None)")
        {
            if (string.IsNullOrWhiteSpace(sequenceName))
                return noneLabel;

            string trimmed = sequenceName.Trim();
            bool exists = Sequences.Any(s => string.Equals(s.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            return exists ? trimmed : noneLabel;
        }

        /// <summary>
        /// Resolves the pre/post-scrape sequence dropdown selections for <paramref name="selectedScrape"/>,
        /// falling back to the previously selected values when no scrape is selected.
        /// </summary>
        public void GetSequenceDropdownState(
            ScrapeDefinition? selectedScrape,
            string? previousPreSelection,
            string? previousPostSelection,
            out string resolvedPreSelection,
            out string resolvedPostSelection)
        {
            string? preSource = selectedScrape != null ? selectedScrape.PreScrapeSequenceName : previousPreSelection;
            string? postSource = selectedScrape != null ? selectedScrape.PostScrapeSequenceName : previousPostSelection;

            resolvedPreSelection = ResolveSequenceDropdownSelection(preSource);
            resolvedPostSelection = ResolveSequenceDropdownSelection(postSource);
        }

        /// <summary>
        /// Returns the next default display name for a new <see cref="AutomationSequence"/>.
        /// </summary>
        public string GetSuggestedSequenceName()
        {
            return $"Sequence {Sequences.Count + 1}";
        }

        /// <summary>
        /// Creates and appends a new <see cref="AutomationSequence"/> to this project.
        /// </summary>
        public AutomationSequence AddSequence(string name)
        {
            var seq = new AutomationSequence
            {
                Name = string.IsNullOrWhiteSpace(name) ? GetSuggestedSequenceName() : name.Trim(),
                IsActive = true
            };
            Sequences.Add(seq);
            return seq;
        }

        /// <summary>
        /// Creates and appends a new <see cref="AutomationSequence"/> only when <paramref name="name"/> is non-empty.
        /// Returns <c>null</c> when the supplied name is blank or canceled.
        /// </summary>
        public AutomationSequence? TryAddSequence(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return AddSequence(name);
        }

        /// <summary>
        /// Removes the specified <see cref="AutomationSequence"/> from this project.
        /// </summary>
        public bool RemoveSequence(AutomationSequence? sequence)
        {
            if (sequence == null) return false;
            return Sequences.Remove(sequence);
        }

        /// <summary>
        /// Renames the specified <see cref="AutomationSequence"/> and updates any scrape definitions referencing its previous name.
        /// </summary>
        public bool RenameSequence(AutomationSequence? sequence, string? newName)
        {
            if (sequence == null || string.IsNullOrWhiteSpace(newName))
                return false;

            string oldName = sequence.Name;
            string trimmed = newName.Trim();
            sequence.Name = trimmed;

            foreach (var scrape in Scrapes)
            {
                if (string.Equals(scrape.PreScrapeSequenceName, oldName, StringComparison.OrdinalIgnoreCase))
                    scrape.PreScrapeSequenceName = trimmed;
                if (string.Equals(scrape.PostScrapeSequenceName, oldName, StringComparison.OrdinalIgnoreCase))
                    scrape.PostScrapeSequenceName = trimmed;
                if (string.Equals(scrape.PaginationSequenceName, oldName, StringComparison.OrdinalIgnoreCase))
                    scrape.PaginationSequenceName = trimmed;
            }

            return true;
        }

        /// <summary>
        /// Moves the <see cref="AutomationSequence"/> at <paramref name="index"/> one position up.
        /// </summary>
        public bool MoveSequenceUp(int index)
        {
            if (index <= 0 || index >= Sequences.Count) return false;
            var item = Sequences[index];
            Sequences.RemoveAt(index);
            Sequences.Insert(index - 1, item);
            return true;
        }

        /// <summary>
        /// Moves the <see cref="AutomationSequence"/> at <paramref name="index"/> one position down.
        /// </summary>
        public bool MoveSequenceDown(int index)
        {
            if (index < 0 || index >= Sequences.Count - 1) return false;
            var item = Sequences[index];
            Sequences.RemoveAt(index);
            Sequences.Insert(index + 1, item);
            return true;
        }

        /// <summary>
        /// Returns the next default display name for a new <see cref="ScrapeDefinition"/>.
        /// </summary>
        public string GetSuggestedScrapeName()
        {
            return $"Scrape {Scrapes.Count + 1}";
        }

        /// <summary>
        /// Creates and appends a new <see cref="ScrapeDefinition"/> to this project.
        /// </summary>
        public ScrapeDefinition AddScrape(string name)
        {
            var scrape = new ScrapeDefinition
            {
                Name = string.IsNullOrWhiteSpace(name) ? GetSuggestedScrapeName() : name.Trim(),
                IsActive = true
            };
            Scrapes.Add(scrape);
            return scrape;
        }

        /// <summary>
        /// Creates and appends a new <see cref="ScrapeDefinition"/> only when <paramref name="name"/> is non-empty.
        /// Returns <c>null</c> when the supplied name is blank or canceled.
        /// </summary>
        public ScrapeDefinition? TryAddScrape(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return AddScrape(name);
        }

        /// <summary>
        /// Removes the specified <see cref="ScrapeDefinition"/> from this project.
        /// </summary>
        public bool RemoveScrape(ScrapeDefinition? scrape)
        {
            if (scrape == null) return false;
            return Scrapes.Remove(scrape);
        }

        /// <summary>
        /// Renames the specified <see cref="ScrapeDefinition"/>.
        /// </summary>
        public bool RenameScrape(ScrapeDefinition? scrape, string? newName)
        {
            if (scrape == null || string.IsNullOrWhiteSpace(newName))
                return false;

            scrape.Name = newName.Trim();
            return true;
        }

        /// <summary>
        /// Moves the <see cref="ScrapeDefinition"/> at <paramref name="index"/> one position up.
        /// </summary>
        public bool MoveScrapeUp(int index)
        {
            if (index <= 0 || index >= Scrapes.Count) return false;
            var item = Scrapes[index];
            Scrapes.RemoveAt(index);
            Scrapes.Insert(index - 1, item);
            return true;
        }

        /// <summary>
        /// Moves the <see cref="ScrapeDefinition"/> at <paramref name="index"/> one position down.
        /// </summary>
        public bool MoveScrapeDown(int index)
        {
            if (index < 0 || index >= Scrapes.Count - 1) return false;
            var item = Scrapes[index];
            Scrapes.RemoveAt(index);
            Scrapes.Insert(index + 1, item);
            return true;
        }

        /// <summary>
        /// Builds a <see cref="ScriptOptions"/> instance configured from this project's match rules.
        /// </summary>
        public ScriptOptions CreateScriptOptions(int port, IpcMode mode, string? customScript = null)
        {
            var options = new ScriptOptions
            {
                Port = port,
                Mode = mode,
                CustomScript = customScript ?? string.Empty
            };

            if (!MatchAnySite && MatchUrls != null && MatchUrls.Any(u => !string.IsNullOrWhiteSpace(u)))
            {
                options.MatchUrls = MatchUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();
            }

            return options;
        }

        /// <summary>
        /// Serializes this <see cref="AutomationProject"/> to a JSON string.
        /// </summary>
        /// <param name="writeIndented">Whether to format the JSON output with indentation.</param>
        public string ToJson(bool writeIndented = true)
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = writeIndented });
        }

        /// <summary>
        /// Deserializes an <see cref="AutomationProject"/> from a JSON string (supporting both current project JSON and legacy sequence-list JSON).
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        public static AutomationProject FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Project JSON cannot be empty.", nameof(json));

            try
            {
                var project = JsonSerializer.Deserialize<AutomationProject>(json);
                if (project != null && project.Sequences != null)
                {
                    return project;
                }
            }
            catch
            {
                // Fallback to legacy format (List<AutomationSequence>)
            }

            var legacySequences = JsonSerializer.Deserialize<List<AutomationSequence>>(json);
            if (legacySequences != null)
            {
                return new AutomationProject
                {
                    MatchAnySite = true,
                    Sequences = legacySequences
                };
            }

            throw new InvalidDataException("Unable to deserialize AutomationProject from JSON.");
        }

        /// <summary>
        /// Saves this <see cref="AutomationProject"/> as indented JSON to the specified file path.
        /// </summary>
        /// <param name="filePath">The destination file path.</param>
        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, ToJson(true));
        }

        /// <summary>
        /// Asynchronously saves this <see cref="AutomationProject"/> as indented JSON to the specified file path.
        /// </summary>
        /// <param name="filePath">The destination file path.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public Task SaveToFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return File.WriteAllTextAsync(filePath, ToJson(true), cancellationToken);
        }

        /// <summary>
        /// Loads an <see cref="AutomationProject"/> from a JSON file on disk.
        /// </summary>
        /// <param name="filePath">The path to the JSON project file.</param>
        public static AutomationProject LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        /// <summary>
        /// Asynchronously loads an <see cref="AutomationProject"/> from a JSON file on disk.
        /// </summary>
        /// <param name="filePath">The path to the JSON project file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public static async Task<AutomationProject> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            string json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return FromJson(json);
        }
    }
}