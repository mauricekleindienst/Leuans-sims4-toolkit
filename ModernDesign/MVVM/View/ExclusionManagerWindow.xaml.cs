using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class ModRenamerWindow : Window
    {
        private string _modsFolder;
        private List<ModFileInfo> _modFiles = new List<ModFileInfo>();
        private HashSet<string> _exclusions = new HashSet<string>();
        private string _exclusionsPath;
        private bool _isSpanish = false;

        public ModRenamerWindow(string modsFolder)
        {
            InitializeComponent();
            _modsFolder = modsFolder;
            InitializeExclusionsPath();
            LoadExclusions();
            LoadLanguage();
            ApplyLanguage();
            LoadModFiles();
        }

        private void InitializeExclusionsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string qolDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "qol", "ModRenamer");

            if (!Directory.Exists(qolDir))
                Directory.CreateDirectory(qolDir);

            _exclusionsPath = Path.Combine(qolDir, "exclusions.txt");
        }

        private void LoadExclusions()
        {
            _exclusions.Clear();
            if (File.Exists(_exclusionsPath))
            {
                foreach (string line in File.ReadAllLines(_exclusionsPath))
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        _exclusions.Add(trimmed);
                }
            }
        }

        private void SaveExclusions()
        {
            File.WriteAllLines(_exclusionsPath, _exclusions.OrderBy(x => x));
        }

        private void LoadLanguage()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string languagePath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

                if (File.Exists(languagePath))
                {
                    foreach (var line in File.ReadAllLines(languagePath))
                    {
                        if (line.Trim().StartsWith("Language", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split('=');
                            if (parts.Length >= 2)
                            {
                                _isSpanish = parts[1].Trim().ToLower().Contains("es");
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void ApplyLanguage()
        {
            if (_isSpanish)
            {
                TitleText.Text = "🔧 Renombrador de Mods";
                SubtitleText.Text = "Renombra y gestiona tus archivos de mods";
                RenameAllBtn.Content = "✨ Renombrar Todo";
                RestoreAllBtn.Content = "↩️ Restaurar Todo";
                ToggleExtBtn.Content = "📦 .package ↔ .leupackage";
                FixScriptsBtn.Content = "🔧 Arreglar ubicación .ts4script";
                AddPrefixBtn.Content = "➕ Agregar Prefijo";
                RefreshBtn.Content = "🔄 Actualizar";
                ManageExclusionsBtn.Content = "⚙️ Gestionar Exclusiones";
                CloseBtn.Content = "Cerrar";
                PrefixTextBox.Text = "Prefijo...";
                StatusText.Text = "Listo";
            }
            else
            {
                PrefixTextBox.Text = "Prefix...";
            }
        }

        private void LoadModFiles()
        {
            _modFiles.Clear();
            ModListPanel.Children.Clear();

            if (string.IsNullOrEmpty(_modsFolder) || !Directory.Exists(_modsFolder))
            {
                StatusText.Text = _isSpanish ? "Carpeta Mods no encontrada" : "Mods folder not found";
                return;
            }

            var allFiles = Directory.GetFiles(_modsFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".package") || f.EndsWith(".leupackage") || f.EndsWith(".ts4script"))
                .ToList();

            var specialCharsPattern = new Regex(@"[^A-Za-z0-9_\-\.]", RegexOptions.Compiled);

            foreach (var filePath in allFiles)
            {
                string fileName = Path.GetFileName(filePath);
                bool hasSpecialChars = specialCharsPattern.IsMatch(fileName);
                bool isExcluded = _exclusions.Contains(fileName);

                var modInfo = new ModFileInfo
                {
                    OriginalPath = filePath,
                    CurrentName = fileName,
                    OriginalName = fileName,
                    HasSpecialChars = hasSpecialChars,
                    IsExcluded = isExcluded,
                    WasRenamed = false
                };

                _modFiles.Add(modInfo);
            }

            // Ordenar: primero los que tienen caracteres especiales
            _modFiles = _modFiles.OrderByDescending(m => m.HasSpecialChars).ThenBy(m => m.CurrentName).ToList();

            DisplayModFiles();

            int specialCount = _modFiles.Count(m => m.HasSpecialChars && !m.IsExcluded);
            StatusText.Text = _isSpanish
                ? $"{_modFiles.Count} archivos encontrados | {specialCount} con caracteres especiales"
                : $"{_modFiles.Count} files found | {specialCount} with special characters";
        }

        private void DisplayModFiles()
        {
            ModListPanel.Children.Clear();

            foreach (var mod in _modFiles)
            {
                var card = CreateModCard(mod);
                ModListPanel.Children.Add(card);
            }
        }

        private Border CreateModCard(ModFileInfo mod)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    mod.IsExcluded ? "#6B7280" : (mod.HasSpecialChars ? "#EF4444" : "#374151"))),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left side: File info
            var stackPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameText = new TextBlock
            {
                Text = mod.CurrentName,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Bahnschrift Light"),
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(nameText);

            if (mod.WasRenamed)
            {
                var originalText = new TextBlock
                {
                    Text = $"Original: {mod.OriginalName}",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                    FontSize = 11,
                    FontFamily = new FontFamily("Bahnschrift Light"),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                stackPanel.Children.Add(originalText);
            }

            if (mod.IsExcluded)
            {
                var excludedText = new TextBlock
                {
                    Text = _isSpanish ? "🚫 Excluido" : "🚫 Excluded",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    FontSize = 11,
                    FontFamily = new FontFamily("Bahnschrift Light"),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                stackPanel.Children.Add(excludedText);
            }

            grid.Children.Add(stackPanel);
            Grid.SetColumn(stackPanel, 0);

            // Right side: Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!mod.IsExcluded)
            {
                if (mod.WasRenamed)
                {
                    var restoreBtn = CreateSmallButton(_isSpanish ? "↩️ Restaurar" : "↩️ Restore", "#F59E0B");
                    restoreBtn.Click += (s, e) => RestoreSingleFile(mod);
                    buttonPanel.Children.Add(restoreBtn);
                }
                else if (mod.HasSpecialChars)
                {
                    var renameBtn = CreateSmallButton(_isSpanish ? "✨ Renombrar" : "✨ Rename", "#22C55E");
                    renameBtn.Click += (s, e) => RenameSingleFile(mod);
                    buttonPanel.Children.Add(renameBtn);
                }
            }

            var excludeBtn = CreateSmallButton(
                mod.IsExcluded ? (_isSpanish ? "✓ Incluir" : "✓ Include") : (_isSpanish ? "🚫 Excluir" : "🚫 Exclude"),
                mod.IsExcluded ? "#3B82F6" : "#6B7280");
            excludeBtn.Click += (s, e) => ToggleExclusion(mod);
            excludeBtn.Margin = new Thickness(5, 0, 0, 0);
            buttonPanel.Children.Add(excludeBtn);

            grid.Children.Add(buttonPanel);
            Grid.SetColumn(buttonPanel, 1);

            border.Child = grid;
            return border;
        }

        private Button CreateSmallButton(string content, string bgColor)
        {
            var button = new Button
            {
                Content = content,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Bahnschrift Light"),
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;
            button.Template = template;

            return button;
        }

        private void RenameSingleFile(ModFileInfo mod)
        {
            if (mod.IsExcluded) return;

            try
            {
                string directory = Path.GetDirectoryName(mod.OriginalPath);
                string extension = Path.GetExtension(mod.CurrentName);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(mod.CurrentName);

                var specialCharsPattern = new Regex(@"[^A-Za-z0-9_\-\.]");
                string cleanName = specialCharsPattern.Replace(nameWithoutExt, "_");
                string newFileName = cleanName + extension;
                string newPath = Path.Combine(directory, newFileName);

                if (File.Exists(newPath) && newPath != mod.OriginalPath)
                {
                    MessageBox.Show(
                        _isSpanish ? $"Ya existe un archivo con el nombre: {newFileName}" : $"A file already exists with the name: {newFileName}",
                        _isSpanish ? "Error" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                File.Move(mod.OriginalPath, newPath);
                mod.OriginalPath = newPath;
                mod.CurrentName = newFileName;
                mod.WasRenamed = true;
                mod.HasSpecialChars = false;

                DisplayModFiles();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreSingleFile(ModFileInfo mod)
        {
            if (!mod.WasRenamed) return;

            try
            {
                string directory = Path.GetDirectoryName(mod.OriginalPath);
                string originalPath = Path.Combine(directory, mod.OriginalName);

                if (File.Exists(originalPath) && originalPath != mod.OriginalPath)
                {
                    MessageBox.Show(
                        _isSpanish ? $"Ya existe un archivo con el nombre original: {mod.OriginalName}" : $"A file already exists with the original name: {mod.OriginalName}",
                        _isSpanish ? "Error" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                File.Move(mod.OriginalPath, originalPath);
                mod.OriginalPath = originalPath;
                mod.CurrentName = mod.OriginalName;
                mod.WasRenamed = false;

                var specialCharsPattern = new Regex(@"[^A-Za-z0-9_\-\.]");
                mod.HasSpecialChars = specialCharsPattern.IsMatch(mod.CurrentName);

                DisplayModFiles();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleExclusion(ModFileInfo mod)
        {
            if (mod.IsExcluded)
            {
                _exclusions.Remove(mod.CurrentName);
                _exclusions.Remove(mod.OriginalName);
                mod.IsExcluded = false;
            }
            else
            {
                _exclusions.Add(mod.CurrentName);
                mod.IsExcluded = true;
            }

            SaveExclusions();
            DisplayModFiles();
            UpdateStatus();
        }

        private void RenameAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var toRename = _modFiles.Where(m => m.HasSpecialChars && !m.IsExcluded && !m.WasRenamed).ToList();

            if (toRename.Count == 0)
            {
                MessageBox.Show(
                    _isSpanish ? "No hay archivos para renombrar." : "No files to rename.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _isSpanish ? $"¿Renombrar {toRename.Count} archivo(s)?" : $"Rename {toRename.Count} file(s)?",
                _isSpanish ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            int renamed = 0;
            foreach (var mod in toRename)
            {
                try
                {
                    RenameSingleFile(mod);
                    renamed++;
                }
                catch { }
            }

            MessageBox.Show(
                _isSpanish ? $"Se renombraron {renamed} archivo(s)." : $"Renamed {renamed} file(s).",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RestoreAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var toRestore = _modFiles.Where(m => m.WasRenamed).ToList();

            if (toRestore.Count == 0)
            {
                MessageBox.Show(
                    _isSpanish ? "No hay archivos para restaurar." : "No files to restore.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _isSpanish ? $"¿Restaurar {toRestore.Count} archivo(s) a sus nombres originales?" : $"Restore {toRestore.Count} file(s) to original names?",
                _isSpanish ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            int restored = 0;
            foreach (var mod in toRestore)
            {
                try
                {
                    RestoreSingleFile(mod);
                    restored++;
                }
                catch { }
            }

            MessageBox.Show(
                _isSpanish ? $"Se restauraron {restored} archivo(s)." : $"Restored {restored} file(s).",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ToggleExtBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                _isSpanish ? "¿Cambiar extensiones .package ↔ .leupackage?" : "Toggle .package ↔ .leupackage extensions?",
                _isSpanish ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            int toggled = 0;
            foreach (var mod in _modFiles.Where(m => !m.IsExcluded))
            {
                try
                {
                    string directory = Path.GetDirectoryName(mod.OriginalPath);
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(mod.CurrentName);
                    string currentExt = Path.GetExtension(mod.CurrentName);
                    string newExt = currentExt == ".package" ? ".leupackage" : ".package";
                    string newFileName = nameWithoutExt + newExt;
                    string newPath = Path.Combine(directory, newFileName);

                    if (!File.Exists(newPath))
                    {
                        File.Move(mod.OriginalPath, newPath);
                        mod.OriginalPath = newPath;
                        mod.CurrentName = newFileName;
                        toggled++;
                    }
                }
                catch { }
            }

            DisplayModFiles();
            MessageBox.Show(
                _isSpanish ? $"Se cambiaron {toggled} extensiones." : $"Toggled {toggled} extensions.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void FixScriptsBtn_Click(object sender, RoutedEventArgs e)
        {
            var scriptsInSubfolders = new List<string>();

            foreach (var mod in _modFiles.Where(m => m.CurrentName.EndsWith(".ts4script")))
            {
                string relativePath = mod.OriginalPath.Replace(_modsFolder, "").TrimStart('\\', '/');
                if (relativePath.Contains("\\") || relativePath.Contains("/"))
                {
                    scriptsInSubfolders.Add(mod.OriginalPath);
                }
            }

            if (scriptsInSubfolders.Count == 0)
            {
                MessageBox.Show(
                    _isSpanish ? "Todos los .ts4script están en la ubicación correcta." : "All .ts4script files are in the correct location.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _isSpanish ? $"Se encontraron {scriptsInSubfolders.Count} archivos .ts4script en subcarpetas.\n\n¿Moverlos a la carpeta Mods raíz?" : $"Found {scriptsInSubfolders.Count} .ts4script files in subfolders.\n\nMove them to the root Mods folder?",
                _isSpanish ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            int moved = 0;
            foreach (var scriptPath in scriptsInSubfolders)
            {
                try
                {
                    string fileName = Path.GetFileName(scriptPath);
                    string newPath = Path.Combine(_modsFolder, fileName);

                    if (!File.Exists(newPath))
                    {
                        File.Move(scriptPath, newPath);
                        moved++;
                    }
                }
                catch { }
            }

            LoadModFiles();
            MessageBox.Show(
                _isSpanish ? $"Se movieron {moved} archivos .ts4script." : $"Moved {moved} .ts4script files.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AddPrefixBtn_Click(object sender, RoutedEventArgs e)
        {
            string prefix = PrefixTextBox.Text.Trim();

            if (string.IsNullOrEmpty(prefix) || prefix == "Prefix..." || prefix == "Prefijo...")
            {
                MessageBox.Show(
                    _isSpanish ? "Ingresa un prefijo válido." : "Enter a valid prefix.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var toRename = _modFiles.Where(m => !m.IsExcluded && !m.CurrentName.StartsWith(prefix)).ToList();

            if (toRename.Count == 0)
            {
                MessageBox.Show(
                    _isSpanish ? "Todos los archivos ya tienen el prefijo." : "All files already have the prefix.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _isSpanish ? $"¿Agregar prefijo '{prefix}' a {toRename.Count} archivo(s)?" : $"Add prefix '{prefix}' to {toRename.Count} file(s)?",
                _isSpanish ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            int renamed = 0;
            foreach (var mod in toRename)
            {
                try
                {
                    string directory = Path.GetDirectoryName(mod.OriginalPath);
                    string newFileName = prefix + mod.CurrentName;
                    string newPath = Path.Combine(directory, newFileName);

                    if (!File.Exists(newPath))
                    {
                        File.Move(mod.OriginalPath, newPath);
                        mod.OriginalPath = newPath;
                        mod.CurrentName = newFileName;
                        renamed++;
                    }
                }
                catch { }
            }

            DisplayModFiles();
            MessageBox.Show(
                _isSpanish ? $"Se agregó el prefijo a {renamed} archivo(s)." : $"Added prefix to {renamed} file(s).",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadModFiles();
        }

        private void ManageExclusionsBtn_Click(object sender, RoutedEventArgs e)
        {
            var exclusionWindow = new ExclusionManagerWindow(_exclusions, _isSpanish);
            if (exclusionWindow.ShowDialog() == true)
            {
                _exclusions = exclusionWindow.Exclusions;
                SaveExclusions();
                LoadModFiles();
            }
        }

        private void UpdateStatus()
        {
            int specialCount = _modFiles.Count(m => m.HasSpecialChars && !m.IsExcluded);
            StatusText.Text = _isSpanish
                ? $"{_modFiles.Count} archivos | {specialCount} con caracteres especiales"
                : $"{_modFiles.Count} files | {specialCount} with special characters";
        }

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ModFileInfo
    {
        public string OriginalPath { get; set; }
        public string CurrentName { get; set; }
        public string OriginalName { get; set; }
        public bool HasSpecialChars { get; set; }
        public bool IsExcluded { get; set; }
        public bool WasRenamed { get; set; }
    }
}
