using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class CrackConfigWindow : Window
    {
        private string _configPath;
        private Dictionary<string, string> _configValues = new Dictionary<string, string>();
        private List<Entitlement> _entitlements = new List<Entitlement>();
        private readonly HttpClient _httpClient = new HttpClient();
        private bool _customDocumentsEnabled = false;

        public class Entitlement
        {
            public string Key { get; set; }
            public string Group { get; set; }
            public string Version { get; set; }
            public string Type { get; set; }
            public string EntitlementTag { get; set; }
        }

        public CrackConfigWindow(string configPath)
        {
            InitializeComponent();
            _configPath = configPath;

            ApplyLanguage();
            LoadConfiguration();
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = "⚙️ CONFIGURACIÓN DEL CRACK ⚙️";
                SubHeaderText.Text = "Configura los ajustes de tu juego crackeado";
                SaveBtn.Content = "💾 GUARDAR";
                CloseBtn.Content = "✕ CERRAR";
                AddEntitlementBtn.Content = "➕ AGREGAR";
                AutoDetectVersionBtn.Content = "🔍 Auto-Detectar";
                ResetToDefaultBtn.Content = "🔄 RESTABLECER";
            }
        }

        private void CustomDocumentsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            var result = MessageBox.Show(
                isSpanish
                    ? "⚠️ ADVERTENCIA IMPORTANTE ⚠️\n\n¿Estás seguro de que deseas habilitar la carpeta Documents personalizada?\n\nEsto cambiará la ubicación donde se guardan:\n• Mods y CC\n• Partidas guardadas\n• Archivos de la Galería (Tray)\n• Capturas de pantalla\n• Configuraciones del juego\n\n⚠️ Si no sabes lo que estás haciendo, esto puede causar PÉRDIDA DE DATOS o que el juego no encuentre tus archivos.\n\n¿Continuar de todos modos?"
                    : "⚠️ IMPORTANT WARNING ⚠️\n\nAre you sure you want to enable custom Documents folder?\n\nThis will change the location where the following are saved:\n• Mods and CC\n• Save games\n• Gallery files (Tray)\n• Screenshots\n• Game settings\n\n⚠️ If you don't know what you're doing, this can cause DATA LOSS or make the game unable to find your files.\n\nContinue anyway?",
                isSpanish ? "Confirmación Requerida" : "Confirmation Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _customDocumentsEnabled = true;
                CustomDocumentsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                CustomDocumentsCheckBox.IsChecked = false;
            }
        }

        private void CustomDocumentsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _customDocumentsEnabled = false;
            CustomDocumentsPanel.Visibility = Visibility.Collapsed;
            CustomDocumentsTextBox.Clear();
        }

        private void BrowseCustomDocuments_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = IsSpanishLanguage()
                    ? "Selecciona la carpeta personalizada para Documents"
                    : "Select custom Documents folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CustomDocumentsTextBox.Text = dialog.SelectedPath;
            }
        }

        private static bool IsSpanishLanguage()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string languagePath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

                if (!File.Exists(languagePath))
                    return false;

                var lines = File.ReadAllLines(languagePath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("Language") && trimmed.Contains("="))
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length == 2)
                        {
                            return parts[1].Trim().ToLower().Contains("es");
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try { this.DragMove(); } catch { }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    MessageBox.Show("Configuration file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string content = File.ReadAllText(_configPath);

                // Parse configuration
                _configValues["Version"] = ExtractValue(content, @"""Version""\s+""([^""]+)""");
                _configValues["Language"] = ExtractValue(content, @"""Language""\s+""([^""]+)""");
                _configValues["LanguageRegistrySpoof"] = ExtractValue(content, @"""LanguageRegistrySpoof""\s+""([^""]+)""");
                _configValues["FakeAuth"] = ExtractValue(content, @"""FakeAuth""\s+""([^""]+)""");
                _configValues["LoadExtraDLLs"] = ExtractValue(content, @"""LoadExtraDLLs""\s+""([^""]+)""");
                _configValues["Username"] = ExtractValue(content, @"""Username""\s+""([^""]+)""");
                _configValues["PersonaId"] = ExtractValue(content, @"""PersonaId""\s+""([^""]+)""");
                _configValues["UserId"] = ExtractValue(content, @"""UserId""\s+""([^""]+)""");
                _configValues["Avatar"] = ExtractValue(content, @"""Avatar""\s+""([^""]+)""");
                _configValues["UseLastOnlineProfile"] = ExtractValue(content, @"""UseLastOnlineProfile""\s+""([^""]+)""");

                // Custom Documents
                string documentsLine = ExtractDocumentsLine(content);
                if (!string.IsNullOrEmpty(documentsLine))
                {
                    _customDocumentsEnabled = true;
                    _configValues["Documents"] = documentsLine;
                    CustomDocumentsCheckBox.IsChecked = true;
                    CustomDocumentsTextBox.Text = documentsLine;
                    CustomDocumentsPanel.Visibility = Visibility.Visible;
                }

                // Set UI values
                VersionTextBox.Text = _configValues["Version"];
                SetLanguageComboBox(_configValues["Language"]);
                LanguageRegistrySpoofCheckBox.IsChecked = _configValues["LanguageRegistrySpoof"].ToLower() == "true";
                FakeAuthCheckBox.IsChecked = _configValues["FakeAuth"].ToLower() == "true";

                // LoadExtraDLLs - si NO está vacío, está habilitado
                LoadExtraDLLsCheckBox.IsChecked = !string.IsNullOrEmpty(_configValues["LoadExtraDLLs"]);

                UsernameTextBox.Text = _configValues["Username"];
                PersonaIdTextBox.Text = _configValues["PersonaId"];
                UserIdTextBox.Text = _configValues["UserId"];
                AvatarPathTextBox.Text = _configValues["Avatar"];
                UseLastOnlineProfileCheckBox.IsChecked = _configValues["UseLastOnlineProfile"].ToLower() == "true";

                // Load entitlements
                LoadEntitlements(content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEntitlements(string content)
        {
            _entitlements.Clear();

            var pattern = @"""(SIMS4\.OFF\.SOLP\.0x[0-9A-F]+|[A-Z0-9\-:]+)""\s*\{[^}]*""Group""\s+""([^""]+)""[^}]*""Version""\s+""([^""]+)""[^}]*""Type""\s+""([^""]+)""[^}]*""EntitlementTag""\s+""([^""]+)""[^}]*\}";
            var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                _entitlements.Add(new Entitlement
                {
                    Key = match.Groups[1].Value,
                    Group = match.Groups[2].Value,
                    Version = match.Groups[3].Value,
                    Type = match.Groups[4].Value,
                    EntitlementTag = match.Groups[5].Value
                });
            }

            RefreshEntitlementsList();
        }

        private void RefreshEntitlementsList()
        {
            EntitlementsPanel.Children.Clear();

            foreach (var entitlement in _entitlements)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 5),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    BorderThickness = new Thickness(1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var stackPanel = new StackPanel();

                var keyText = new TextBlock
                {
                    Text = entitlement.Key,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };

                var tagText = new TextBlock
                {
                    Text = $"Tag: {entitlement.EntitlementTag}",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Margin = new Thickness(0, 3, 0, 0)
                };

                stackPanel.Children.Add(keyText);
                stackPanel.Children.Add(tagText);

                var deleteBtn = new Button
                {
                    Content = "🗑️",
                    Width = 30,
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(139, 0, 0)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Tag = entitlement
                };
                deleteBtn.Click += DeleteEntitlement_Click;

                Grid.SetColumn(stackPanel, 0);
                Grid.SetColumn(deleteBtn, 1);

                grid.Children.Add(stackPanel);
                grid.Children.Add(deleteBtn);

                border.Child = grid;
                EntitlementsPanel.Children.Add(border);
            }
        }

        private void AddEntitlement_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEntitlementDialog();
            if (dialog.ShowDialog() == true)
            {
                _entitlements.Add(new Entitlement
                {
                    Key = dialog.EntitlementKey,
                    Group = "THESIMS4PC",
                    Version = "0",
                    Type = "DEFAULT",
                    EntitlementTag = dialog.EntitlementTag
                });

                RefreshEntitlementsList();
            }
        }

        private void DeleteEntitlement_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entitlement = button.Tag as Entitlement;

            var result = MessageBox.Show(
                $"Delete entitlement {entitlement.Key}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _entitlements.Remove(entitlement);
                RefreshEntitlementsList();
            }
        }

        private async void AutoDetectVersion_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();
            AutoDetectVersionBtn.IsEnabled = false;
            AutoDetectVersionBtn.Content = isSpanish ? "⏳ Descargando..." : "⏳ Downloading...";

            try
            {
                string url = "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/leuans4.cfg";
                string content = await _httpClient.GetStringAsync(url);

                string version = ExtractValue(content, @"""Version""\s+""([^""]+)""");

                if (!string.IsNullOrEmpty(version))
                {
                    VersionTextBox.Text = version;
                    MessageBox.Show(
                        isSpanish ? $"✓ Versión detectada: {version}" : $"✓ Version detected: {version}",
                        isSpanish ? "Éxito" : "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        isSpanish ? "No se pudo detectar la versión." : "Could not detect version.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                AutoDetectVersionBtn.IsEnabled = true;
                AutoDetectVersionBtn.Content = isSpanish ? "🔍 Auto-Detectar" : "🔍 Auto-Detect";
            }
        }

        private async void ResetToDefault_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            var result = MessageBox.Show(
                isSpanish
                    ? "⚠️ ADVERTENCIA ⚠️\n\n¿Estás seguro de que deseas restablecer la configuración a los valores predeterminados?\n\nEsto descargará el archivo leuans4.cfg desde GitHub y REEMPLAZARÁ tu configuración actual.\n\n¡Todos tus cambios personalizados se perderán!"
                    : "⚠️ WARNING ⚠️\n\nAre you sure you want to reset the configuration to default values?\n\nThis will download the leuans4.cfg file from GitHub and REPLACE your current configuration.\n\nAll your custom changes will be lost!",
                isSpanish ? "Confirmar Restablecimiento" : "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            ResetToDefaultBtn.IsEnabled = false;
            ResetToDefaultBtn.Content = isSpanish ? "⏳ Descargando..." : "⏳ Downloading...";

            try
            {
                string url = "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/leuans4.cfg";
                string content = await _httpClient.GetStringAsync(url);

                // Backup del archivo actual
                string backupPath = _configPath + ".backup";
                File.Copy(_configPath, backupPath, true);

                // Reemplazar con el nuevo contenido
                File.WriteAllText(_configPath, content);

                MessageBox.Show(
                    isSpanish
                        ? "✓ Configuración restablecida exitosamente.\n\nSe creó un respaldo en:\n" + backupPath
                        : "✓ Configuration reset successfully.\n\nA backup was created at:\n" + backupPath,
                    isSpanish ? "Éxito" : "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Recargar configuración
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                ResetToDefaultBtn.IsEnabled = true;
                ResetToDefaultBtn.Content = isSpanish ? "🔄 RESTABLECER" : "🔄 RESET TO DEFAULT";
            }
        }

        private string ExtractValue(string content, string pattern)
        {
            var match = Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value : "";
        }

        private string ExtractDocumentsLine(string content)
        {
            // Buscar línea descomentada de Documents
            var match = Regex.Match(content, @"^\s*""Documents""\s+""([^""]+)""", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value : "";
        }

        private void SetLanguageComboBox(string languageCode)
        {
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag.ToString() == languageCode)
                {
                    LanguageComboBox.SelectedItem = item;
                    return;
                }
            }
            LanguageComboBox.SelectedIndex = 0;
        }

        private void BrowseAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Select Avatar Image"
            };

            if (dialog.ShowDialog() == true)
            {
                string binPath = Path.GetDirectoryName(_configPath);
                string relativePath = GetRelativePath(binPath, dialog.FileName);
                AvatarPathTextBox.Text = relativePath.Replace("\\", "/");
            }
        }

        private string GetRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath + Path.DirectorySeparatorChar);
            Uri toUri = new Uri(toPath);
            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            try
            {
                string content = File.ReadAllText(_configPath);

                // Update basic values
                var selectedLanguage = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "en_US";
                content = ReplaceValue(content, @"""Version""\s+""[^""]+""", $"\"Version\"               \"{VersionTextBox.Text}\"");
                content = ReplaceValue(content, @"""Language""\s+""[^""]+""", $"\"Language\"              \"{selectedLanguage}\"");
                content = ReplaceValue(content, @"""LanguageRegistrySpoof""\s+""[^""]+""", $"\"LanguageRegistrySpoof\" \"{(LanguageRegistrySpoofCheckBox.IsChecked == true ? "true" : "false")}\"");
                content = ReplaceValue(content, @"""FakeAuth""\s+""[^""]+""", $"\"FakeAuth\"              \"{(FakeAuthCheckBox.IsChecked == true ? "true" : "false")}\"");

                // LoadExtraDLLs - si está desmarcado, poner vacío
                string loadExtraDLLsValue = LoadExtraDLLsCheckBox.IsChecked == true ? "leuan-u.dll" : "";
                content = ReplaceValue(content, @"""LoadExtraDLLs""\s+""[^""]*""", $"\"LoadExtraDLLs\"         \"{loadExtraDLLsValue}\"");

                content = ReplaceValue(content, @"""Username""\s+""[^""]+""", $"\"Username\"              \"{UsernameTextBox.Text}\"");
                content = ReplaceValue(content, @"""Avatar""\s+""[^""]+""", $"\"Avatar\"                \"{AvatarPathTextBox.Text}\"");
                content = ReplaceValue(content, @"""UseLastOnlineProfile""\s+""[^""]+""", $"\"UseLastOnlineProfile\"  \"{(UseLastOnlineProfileCheckBox.IsChecked == true ? "true" : "false")}\"");

                // Update entitlements section
                content = RebuildEntitlementsSection(content);

                // Custom Documents
                if (_customDocumentsEnabled && !string.IsNullOrWhiteSpace(CustomDocumentsTextBox.Text))
                {
                    // Descomentar la línea Documents
                    content = Regex.Replace(content,
                        @"//\s*""Documents""\s+""[^""]*""",
                        $"\"Documents\"             \"{CustomDocumentsTextBox.Text.Replace("\\", "\\\\")}\"");

                    // Si no existe, agregarla después de "Emulator" {
                    if (!Regex.IsMatch(content, @"""Documents""\s+""[^""]+"""))
                    {
                        content = Regex.Replace(content,
                            @"(""Emulator""\s*\{)",
                            $"$1\n        \"Documents\"             \"{CustomDocumentsTextBox.Text.Replace("\\", "\\\\")}\"");
                    }
                }
                else
                {
                    // Comentar la línea Documents si existe
                    content = Regex.Replace(content,
                        @"^\s*""Documents""\s+""([^""]+)""",
                        "//\"Documents\"             \"$1\"",
                        RegexOptions.Multiline);
                }

                // Save file
                File.WriteAllText(_configPath, content);


                MessageBox.Show(
                    isSpanish ? "¡Configuración guardada exitosamente!" : "Configuration saved successfully!",
                    isSpanish ? "Éxito" : "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{(isSpanish ? "Error al guardar: " : "Error saving: ")}{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string RebuildEntitlementsSection(string content)
        {
            var startPattern = @"""Entitlements""\s*\{";
            var startMatch = Regex.Match(content, startPattern);
            if (!startMatch.Success) return content;

            int startIndex = startMatch.Index + startMatch.Length;
            int braceCount = 1;
            int endIndex = startIndex;

            for (int i = startIndex; i < content.Length; i++)
            {
                if (content[i] == '{') braceCount++;
                if (content[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine();

            foreach (var ent in _entitlements)
            {
                sb.AppendLine($"        \"{ent.Key}\"");
                sb.AppendLine("        {");
                sb.AppendLine($"            \"Group\"             \"{ent.Group}\"");
                sb.AppendLine($"            \"Version\"           \"{ent.Version}\"");
                sb.AppendLine($"            \"Type\"              \"{ent.Type}\"");
                sb.AppendLine($"            \"EntitlementTag\"    \"{ent.EntitlementTag}\"");
                sb.AppendLine("        }");
            }

            sb.Append("    ");

            string before = content.Substring(0, startIndex);
            string after = content.Substring(endIndex);
            return before + sb.ToString() + after;
        }

        private string ReplaceValue(string content, string pattern, string replacement)
        {
            return Regex.Replace(content, pattern, replacement);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Dialog para agregar entitlements
    public class AddEntitlementDialog : Window
    {
        private TextBox _keyTextBox;
        private TextBox _tagTextBox;
        private TextBlock _calculatedTagText;
        private CheckBox _manualTagCheckBox;

        public string EntitlementKey { get; private set; }
        public string EntitlementTag { get; private set; }

        public AddEntitlementDialog()
        {
            Title = "Add Entitlement";
            Width = 500;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20)
            };

            var stackPanel = new StackPanel();

            var header = new TextBlock
            {
                Text = "➕ ADD ENTITLEMENT",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var keyLabel = new TextBlock
            {
                Text = "Entitlement Key:",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 5)
            };

            _keyTextBox = new TextBox
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                FontFamily = new FontFamily("Consolas"),
            };
            _keyTextBox.TextChanged += KeyTextBox_TextChanged;

            _calculatedTagText = new TextBlock
            {
                Text = "Auto-calculated tag will appear here",
                Foreground = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Margin = new Thickness(0, 5, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            _manualTagCheckBox = new CheckBox
            {
                Content = "Enter tag manually",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            _manualTagCheckBox.Checked += (s, e) => _tagTextBox.IsEnabled = true;
            _manualTagCheckBox.Unchecked += (s, e) => _tagTextBox.IsEnabled = false;

            var tagLabel = new TextBlock
            {
                Text = "Entitlement Tag:",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 5)
            };

            _tagTextBox = new TextBox
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                FontFamily = new FontFamily("Consolas"),
                IsEnabled = false
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var addBtn = new Button
            {
                Content = "✓ ADD",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(30, 90, 142)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            addBtn.Click += AddBtn_Click;

            var cancelBtn = new Button
            {
                Content = "✕ CANCEL",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            cancelBtn.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(addBtn);
            buttonPanel.Children.Add(cancelBtn);

            stackPanel.Children.Add(header);
            stackPanel.Children.Add(keyLabel);
            stackPanel.Children.Add(_keyTextBox);
            stackPanel.Children.Add(_calculatedTagText);
            stackPanel.Children.Add(_manualTagCheckBox);
            stackPanel.Children.Add(tagLabel);
            stackPanel.Children.Add(_tagTextBox);
            stackPanel.Children.Add(buttonPanel);

            border.Child = stackPanel;
            Content = border;

            MouseLeftButtonDown += (s, e) => { try { DragMove(); } catch { } };
        }

        private void KeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_manualTagCheckBox.IsChecked == true) return;

            string key = _keyTextBox.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                _calculatedTagText.Text = "Auto-calculated tag will appear here";
                return;
            }

            var match = Regex.Match(key, @"SIMS4\.OFF\.SOLP\.(0x[0-9A-F]+)");
            if (match.Success)
            {
                string hex = match.Groups[1].Value;
                try
                {
                    long decimalValue = Convert.ToInt64(hex, 16);
                    string calculatedTag = $"ENTITLEMENT_{hex}:{decimalValue}";
                    _calculatedTagText.Text = $"✓ Auto-calculated: {calculatedTag}";
                    _tagTextBox.Text = calculatedTag;
                }
                catch
                {
                    _calculatedTagText.Text = "⚠️ Could not calculate - please enter manually";
                }
            }
            else
            {
                _calculatedTagText.Text = "⚠️ Format not recognized - please enter tag manually";
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_keyTextBox.Text))
            {
                MessageBox.Show("Please enter an entitlement key", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_tagTextBox.Text))
            {
                MessageBox.Show("Please enter an entitlement tag", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EntitlementKey = _keyTextBox.Text.Trim();
            EntitlementTag = _tagTextBox.Text.Trim();
            DialogResult = true;
        }


    }
}