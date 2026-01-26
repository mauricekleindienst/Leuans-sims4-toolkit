using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace ModernDesign.MVVM.View
{
    public enum SavePreference
    {
        None,
        BackupOnStart,
        BackupRegular,
        NeverBackup
    }

    public class SaveSlotInfo : INotifyPropertyChanged
    {
        private SavePreference _preference;

        public string SlotId { get; set; }
        public int VersionCount { get; set; }
        public List<string> Files { get; set; } = new List<string>();

        public SavePreference Preference
        {
            get => _preference;
            set
            {
                if (_preference != value)
                {
                    _preference = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preference)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class SaveGamesView : Window
    {
        private string _languageCode = "en-US";
        private string _savesFolder;

        private readonly List<SaveSlotInfo> _saveSlots = new List<SaveSlotInfo>();
        public IEnumerable<SaveSlotInfo> SaveSlots => _saveSlots;

        private string _preferencesPath;
        private Dictionary<string, SavePreference> _preferencesBySlot =
            new Dictionary<string, SavePreference>(StringComparer.OrdinalIgnoreCase);

        public SaveGamesView()
        {
            InitializeComponent();
            DataContext = this;

            LoadLanguageFromIni();
            InitTexts();
            InitPaths();
            LoadPreferencesFromIni();
            ScanSavesFolder();
        }

        private void LoadLanguageFromIni()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
                string iniPath = Path.Combine(folder, "language.ini");

                if (!File.Exists(iniPath))
                    return;

                string currentSection = string.Empty;

                foreach (var rawLine in File.ReadAllLines(iniPath))
                {
                    var line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    if (!currentSection.Equals("General", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (line.StartsWith("Language", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('=');
                        if (parts.Length >= 2)
                        {
                            var value = parts[1].Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                _languageCode = value;
                            }
                        }
                        break;
                    }
                }

                if (_languageCode != "es-ES" && _languageCode != "en-US")
                    _languageCode = "en-US";
            }
            catch
            {
                _languageCode = "en-US";
            }
        }

        private void InitTexts()
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            Title = es ? "Gestor de SaveGames" : "SaveGames Manager";
            TitleText.Text = es ? "💾 Gestor de SaveGames" : "💾 SaveGames Manager";
            SubtitleText.Text = es
                ? "Administra copias de seguridad y preferencias para tus partidas guardadas"
                : "Manage backups and preferences for your saved games";

            SavesPathLabel.Text = es
                ? "Ubicación de la carpeta saves:"
                : "Saves folder location:";

            SlotsHeaderText.Text = es
                ? "📋 Tus Slots de Guardado"
                : "📋 Your Save Slots";

            RestoreButton.Content = es ? "♻️ Restaurar Savegame" : "♻️ Restore Savegame";
            SavePrefsButton.Content = es ? "💾 Guardar preferencias" : "💾 Save preferences";
            ChangeSavesFolderButton.Content = es ? "📂 Cambiar carpeta" : "📂 Change folder";
            RescanButton.Content = es ? "🔄 Reescanear" : "🔄 Rescan";
        }

        private void InitPaths()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string prefsFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
            Directory.CreateDirectory(prefsFolder);

            _preferencesPath = Path.Combine(prefsFolder, "preferences.ini");

            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseFolder = Path.Combine(docs, "Electronic Arts");

            string losSimsPath = Path.Combine(baseFolder, "Los Sims 4", "saves");
            string theSimsPath = Path.Combine(baseFolder, "The Sims 4", "saves");

            if (Directory.Exists(losSimsPath))
                _savesFolder = losSimsPath;
            else if (Directory.Exists(theSimsPath))
                _savesFolder = theSimsPath;
            else
                _savesFolder = null;

            UpdateSavesPathText();
        }

        private void UpdateSavesPathText()
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(_savesFolder) && Directory.Exists(_savesFolder))
            {
                SavesPathText.Text = _savesFolder;
            }
            else
            {
                SavesPathText.Text = es
                    ? "(No se encontró carpeta de saves. Selecciónala manualmente.)"
                    : "(No saves folder found. Please select it manually.)";
            }
        }

        private void ScanSavesFolder()
        {
            _saveSlots.Clear();

            if (string.IsNullOrEmpty(_savesFolder) || !Directory.Exists(_savesFolder))
            {
                SlotsListView.ItemsSource = null;
                SlotsListView.ItemsSource = _saveSlots;
                UpdateSlotCount();
                return;
            }

            var files = Directory.GetFiles(_savesFolder, "Slot_*.save*", SearchOption.TopDirectoryOnly);

            var regex = new Regex(@"^(Slot_\d+)\.save(\.ver\d+)?$", RegexOptions.IgnoreCase);

            var groups = files
                .Select(f => new { Path = f, Name = Path.GetFileName(f) })
                .Select(x =>
                {
                    var m = regex.Match(x.Name);
                    if (!m.Success) return null;
                    return new
                    {
                        SlotId = m.Groups[1].Value,
                        FilePath = x.Path
                    };
                })
                .Where(x => x != null)
                .GroupBy(x => x.SlotId, StringComparer.OrdinalIgnoreCase);

            foreach (var grp in groups)
            {
                var slot = new SaveSlotInfo
                {
                    SlotId = grp.Key,
                    VersionCount = grp.Count(),
                    Files = grp.Select(g => g.FilePath).ToList(),
                    Preference = SavePreference.None
                };

                if (_preferencesBySlot.TryGetValue(slot.SlotId, out var pref))
                {
                    slot.Preference = pref;
                }

                _saveSlots.Add(slot);
            }

            SlotsListView.ItemsSource = null;
            SlotsListView.ItemsSource = _saveSlots;
            UpdateSlotCount();
        }

        private void UpdateSlotCount()
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
            int count = _saveSlots.Count;

            SlotCountText.Text = es
                ? $"{count} {(count == 1 ? "slot" : "slots")}"
                : $"{count} {(count == 1 ? "slot" : "slots")}";
        }

        private void LoadPreferencesFromIni()
        {
            _preferencesBySlot.Clear();

            if (!File.Exists(_preferencesPath))
                return;

            string currentSection = string.Empty;

            foreach (var rawLine in File.ReadAllLines(_preferencesPath))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                if (!currentSection.Equals("SaveGames", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split('=');
                if (parts.Length < 2) continue;

                var slotId = parts[0].Trim();
                var value = parts[1].Trim();

                if (Enum.TryParse<SavePreference>(value, ignoreCase: true, out var pref))
                {
                    _preferencesBySlot[slotId] = pref;
                }
            }
        }

        private void SavePreferencesToIni()
        {
            List<string> lines = new List<string>();
            if (File.Exists(_preferencesPath))
            {
                lines = File.ReadAllLines(_preferencesPath).ToList();
            }

            int startIndex = lines.FindIndex(l => l.Trim().Equals("[SaveGames]", StringComparison.OrdinalIgnoreCase));
            if (startIndex >= 0)
            {
                int endIndex = startIndex + 1;
                while (endIndex < lines.Count && !lines[endIndex].Trim().StartsWith("["))
                {
                    endIndex++;
                }
                lines.RemoveRange(startIndex, endIndex - startIndex);
            }

            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                lines.Add(string.Empty);

            lines.Add("[SaveGames]");

            foreach (var slot in _saveSlots)
            {
                string line = $"{slot.SlotId}={slot.Preference}";
                lines.Add(line);
            }

            File.WriteAllLines(_preferencesPath, lines);
        }

        private void ChangeSavesFolderButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = es
                    ? "Selecciona la carpeta 'saves' de The Sims 4"
                    : "Select The Sims 4 'saves' folder",
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrEmpty(_savesFolder) && Directory.Exists(_savesFolder))
            {
                dialog.SelectedPath = _savesFolder;
            }

            var result = dialog.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                _savesFolder = dialog.SelectedPath;
                UpdateSavesPathText();
                ScanSavesFolder();
            }
        }

        private void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanSavesFolder();
        }

        private void SavePrefsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SavePreferencesToIni();

                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                MessageBox.Show(
                    es ? "Preferencias guardadas correctamente en preferences.ini."
                       : "Preferences saved successfully to preferences.ini.",
                    es ? "Gestor de SaveGames" : "SaveGames Manager",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            try
            {
                var restoreWindow = new RestoreBackupWindow(_savesFolder, _languageCode)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                bool? result = restoreWindow.ShowDialog();

                if (result == true)
                {
                    ScanSavesFolder();

                    MessageBox.Show(
                        es ? " Savegame restaurado correctamente."
                           : " Savegame restored successfully.",
                        es ? "Restauración completada" : "Restore completed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    es ? $"❌ Error al restaurar:\n{ex.Message}"
                       : $"❌ Error restoring:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}