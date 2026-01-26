using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ModernDesign.Localization;
using Newtonsoft.Json;
using WinForms = System.Windows.Forms;

namespace ModernDesign.MVVM.View
{
    public partial class ModManagerWindow : Window
    {
        private string _modsFolder = "";
        private string _gameFolder = "";
        private List<ModItem> _allMods = new List<ModItem>();
        private List<ModItem> _filteredMods = new List<ModItem>();
        private List<string> _favorites = new List<string>();
        private Dictionary<string, List<string>> _collections = new Dictionary<string, List<string>>();
        private const string MODS_JSON_URL = "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/mods_database.json";
        private string _currentView = "all"; // "all", "favorites", "collection"
        private string _currentCollectionName = "";

        public ModManagerWindow()
        {
            InitializeComponent();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool es = LanguageManager.IsSpanish;
            this.Title = es ? "Gestor de Mods" : "Mod Manager";
            TitleText.Text = es ? "🎮 Gestor de Mods" : "🎮 Mod Manager";
            SubtitleText.Text = es ? "Descarga, administra y organiza tus mods favoritos" : "Download, manage and organize your favorite mods";
            SearchBox.Text = es ? "Buscar mods..." : "Search mods...";

            ShowAllButton.Content = es ? "Todos" : "All";
            ShowFavoritesButton.Content = es ? "⭐ Favoritos" : "⭐ Favorites";
            ShowCollectionsButton.Content = es ? "📁 Colecciones" : "📁 Collections";
            DownloadAllFavoritesButton.Content = es ? "⬇️ Descargar Favoritos" : "⬇️ Download Favorites";
            UninstallAllFavoritesButton.Content = es ? "🗑️ Desinstalar Favoritos" : "🗑️ Uninstall Favorites";
            UploadCollectionButton.Content = es ? "📤 Subir Colección" : "📤 Upload Collection";
            TestGameButton.Content = es ? "🎮 Abrir Juego" : "🎮 Open Game";
            CheckUpdatesButton.Content = es ? "🔄 Verificar Actualizaciones" : "🔄 Check Updates";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await DetectModsFolder();
            LoadGameFolder();
            LoadFavorites();
            LoadCollections();
            await LoadModsFromDatabase();
            DisplayMods(_allMods);
        }

        private async Task DetectModsFolder()
        {
            try
            {
                bool es = LanguageManager.IsSpanish;
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] possiblePaths = new string[]
                {
                    System.IO.Path.Combine(documentsPath, "Electronic Arts", "Los Sims 4", "Mods"),
                    System.IO.Path.Combine(documentsPath, "Electronic Arts", "The Sims 4", "Mods")
                };

                foreach (string path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        _modsFolder = path;
                        ModsFolderText.Text = AbbreviatePath(_modsFolder, 50);
                        return;
                    }
                }
                ModsFolderText.Text = es ? "Carpeta Mods no detectada" : "Mods folder not detected";
            }
            catch { }
        }

        private void LoadGameFolder()
        {
            try
            {
                bool es = LanguageManager.IsSpanish;
                string settingsPath = GetModManagerSettingsPath();
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("GameFolder="))
                        {
                            _gameFolder = line.Substring("GameFolder=".Length).Trim();
                            GameFolderText.Text = AbbreviatePath(_gameFolder, 50);
                            return;
                        }
                    }
                }
                GameFolderText.Text = es ? "Carpeta del juego no configurada" : "Game folder not configured";
            }
            catch { }
        }

        private void SaveGameFolder()
        {
            try
            {
                string settingsPath = GetModManagerSettingsPath();
                var lines = new List<string>();
                if (File.Exists(settingsPath))
                {
                    lines = File.ReadAllLines(settingsPath).ToList();
                    lines.RemoveAll(l => l.StartsWith("GameFolder="));
                }
                lines.Add($"GameFolder={_gameFolder}");
                File.WriteAllLines(settingsPath, lines);
            }
            catch { }
        }

        private string AbbreviatePath(string path, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength) return path;
            int charsToShow = (maxLength - 3) / 2;
            return path.Substring(0, charsToShow) + "..." + path.Substring(path.Length - charsToShow);
        }

        private string GetModManagerFolderPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = System.IO.Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "qol", "modmanager");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private string GetModManagerSettingsPath() => System.IO.Path.Combine(GetModManagerFolderPath(), "settings.ini");
        private string GetFavoritesPath() => System.IO.Path.Combine(GetModManagerFolderPath(), "favorites.ini");
        private string GetCollectionsPath() => System.IO.Path.Combine(GetModManagerFolderPath(), "collections.ini");

        private async Task LoadModsFromDatabase()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ModsScrollViewer.Visibility = Visibility.Collapsed;

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(15);
                    var json = await httpClient.GetStringAsync(MODS_JSON_URL);
                    var database = JsonConvert.DeserializeObject<ModDatabase>(json);

                    if (database?.Mods != null)
                    {
                        _allMods = database.Mods.Select(m => new ModItem
                        {
                            Id = m.Id,
                            Name = m.Name,
                            NameES = m.NameES,
                            Description = m.Description,
                            DescriptionES = m.DescriptionES,
                            Author = m.Author,
                            DownloadUrl = m.DownloadUrl,
                            FileName = m.FileName,
                            Version = m.Version,
                            GameVersion = m.GameVersion,
                            RequiresManualInstall = m.RequiresManualInstall,
                            PatreonUrl = m.PatreonUrl,
                            AccentColor = m.AccentColor
                        }).ToList();
                    }
                }

                LoadingPanel.Visibility = Visibility.Collapsed;
                ModsScrollViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(es ? $"Error cargando mods: {ex.Message}" : $"Error loading mods: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadFavorites()
        {
            try
            {
                string favPath = GetFavoritesPath();
                if (File.Exists(favPath)) _favorites = File.ReadAllLines(favPath).ToList();
            }
            catch { }
        }

        private void SaveFavorites()
        {
            try { File.WriteAllLines(GetFavoritesPath(), _favorites); }
            catch { }
        }

        private void LoadCollections()
        {
            try
            {
                string collPath = GetCollectionsPath();
                if (File.Exists(collPath))
                {
                    var lines = File.ReadAllLines(collPath);
                    string currentCollection = null;

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentCollection = line.Trim('[', ']');
                            if (!_collections.ContainsKey(currentCollection))
                            {
                                _collections[currentCollection] = new List<string>();
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(line) && currentCollection != null)
                        {
                            _collections[currentCollection].Add(line.Trim());
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveCollections()
        {
            try
            {
                var lines = new List<string>();
                foreach (var collection in _collections)
                {
                    lines.Add($"[{collection.Key}]");
                    lines.AddRange(collection.Value);
                    lines.Add("");
                }
                File.WriteAllLines(GetCollectionsPath(), lines);
            }
            catch { }
        }

        private void ToggleFavorite(string modId)
        {
            if (_favorites.Contains(modId)) _favorites.Remove(modId);
            else _favorites.Add(modId);
            SaveFavorites();
            RefreshCurrentView();
        }

        private void DisplayMods(List<ModItem> mods)
        {

            ModsPanel.Children.Clear();

            var sortedMods = mods.OrderByDescending(m => _favorites.Contains(m.Id)).ToList();
            foreach (var mod in sortedMods)
            {
                var card = CreateModCard(mod);
                ModsPanel.Children.Add(card);
            }

            // Show/hide favorites buttons
            if (DownloadAllFavoritesButton != null && UninstallAllFavoritesButton != null)
            {
                if (_currentView == "favorites")
                {
                    DownloadAllFavoritesButton.Visibility = Visibility.Visible;
                    UninstallAllFavoritesButton.Visibility = Visibility.Visible;
                }
                else
                {
                    DownloadAllFavoritesButton.Visibility = Visibility.Collapsed;
                    UninstallAllFavoritesButton.Visibility = Visibility.Collapsed;
                }
            }

            // Show/hide collection buttons (verificar que existan primero)
            if (DownloadCollectionButton != null && UninstallCollectionButton != null)
            {
                if (_currentView == "collection")
                {
                    DownloadCollectionButton.Visibility = Visibility.Visible;
                    UninstallCollectionButton.Visibility = Visibility.Visible;
                }
                else
                {
                    DownloadCollectionButton.Visibility = Visibility.Collapsed;
                    UninstallCollectionButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private Border CreateModCard(ModItem mod)
        {
            bool es = LanguageManager.IsSpanish;
            bool isFavorite = _favorites.Contains(mod.Id);

            var card = new Border
            {
                Width = 340,
                MinHeight = 200,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(12),
                Padding = new Thickness(20),
                Effect = new DropShadowEffect { Color = (Color)ColorConverter.ConvertFromString(mod.AccentColor ?? "#6366F1"), BlurRadius = 12, ShadowDepth = 0, Opacity = 0.3 }
            };

            var mainStack = new StackPanel();
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mod.AccentColor ?? "#6366F1")),
                Margin = new Thickness(0, 0, 12, 0),
                Child = new TextBlock { Text = GetInitials(mod.Name), FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            Grid.SetColumn(iconBorder, 0);
            headerGrid.Children.Add(iconBorder);

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoStack.Children.Add(new TextBlock { Text = es ? mod.NameES : mod.Name, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis });
            infoStack.Children.Add(new TextBlock { Text = $"by {mod.Author}", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 2, 0, 0) });
            Grid.SetColumn(infoStack, 1);
            headerGrid.Children.Add(infoStack);

            // Add to Collection button
            var addToCollectionBtn = new Button { Content = "➕", FontSize = 16, Width = 35, Height = 35, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")), BorderThickness = new Thickness(0), Foreground = Brushes.White, Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0), ToolTip = es ? "Añadir a colección" : "Add to collection" };
            addToCollectionBtn.Click += (s, e) => AddModToCollection(mod);
            Grid.SetColumn(addToCollectionBtn, 2);
            headerGrid.Children.Add(addToCollectionBtn);

            var favButton = new Button
            {
                Content = isFavorite ? "⭐" : "☆",
                FontSize = 20,
                Width = 35,
                Height = 35,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24")),
                Cursor = Cursors.Hand,
                Tag = mod.Id,
                ToolTip = es ? (isFavorite ? "Quitar de favoritos" : "Añadir a favoritos") : (isFavorite ? "Remove from favorites" : "Add to favorites"),
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };

            // Crear TransformGroup para combinar escala y rotación
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new RotateTransform(0));
            favButton.RenderTransform = transformGroup;

            favButton.Click += (s, args) =>
            {
                var button = s as Button;
                var transforms = button.RenderTransform as TransformGroup;
                var scaleTransform = transforms.Children[0] as ScaleTransform;
                var rotateTransform = transforms.Children[1] as RotateTransform;

                // Animación de escala (crecer)
                var scaleUpX = new DoubleAnimation
                {
                    To = 1.8,
                    Duration = TimeSpan.FromMilliseconds(750),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var scaleUpY = new DoubleAnimation
                {
                    To = 1.8,
                    Duration = TimeSpan.FromMilliseconds(750),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                // Animación de rotación (360 grados)
                var rotateAnimation = new DoubleAnimation
                {
                    To = 360,
                    Duration = TimeSpan.FromMilliseconds(1500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Cuando termine la primera mitad, empezar a encoger
                scaleUpX.Completed += (sender2, e2) =>
                {
                    var scaleDownX = new DoubleAnimation
                    {
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(750),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    var scaleDownY = new DoubleAnimation
                    {
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(750),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDownX);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDownY);
                };

                // Resetear rotación al terminar
                rotateAnimation.Completed += (sender3, e3) =>
                {
                    rotateTransform.Angle = 0;
                };

                // Iniciar animaciones
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUpX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUpY);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

                // Toggle favorito
                ToggleFavorite(mod.Id);
            };

            Grid.SetColumn(favButton, 3);
            headerGrid.Children.Add(favButton);
            mainStack.Children.Add(headerGrid);

            mainStack.Children.Add(new TextBlock { Text = es ? mod.DescriptionES : mod.Description, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12), MaxHeight = 40 });

            var versionStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            versionStack.Children.Add(new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")) { Opacity = 0.2 }, CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 8, 0), Child = new TextBlock { Text = $"v{mod.Version}", FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")) } });
            versionStack.Children.Add(new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) { Opacity = 0.2 }, CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Child = new TextBlock { Text = $"Game {mod.GameVersion}", FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) } });
            mainStack.Children.Add(versionStack);

            var buttonsGrid = new Grid();
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var downloadBtn = CreateActionButton(mod.RequiresManualInstall ? "🔗" : "⬇️", "#22C55E", 0, mod.RequiresManualInstall ? (es ? "Abrir Patreon" : "Open Patreon") : (es ? "Descargar mod" : "Download mod"));
            downloadBtn.Click += async (s, e) => { if (mod.RequiresManualInstall) OpenPatreonPage(mod); else await DownloadMod(mod); };
            buttonsGrid.Children.Add(downloadBtn);

            var backupBtn = CreateActionButton("💾", "#3B82F6", 1, es ? "Crear backup" : "Create backup");
            backupBtn.Click += (s, e) => BackupMod(mod);
            buttonsGrid.Children.Add(backupBtn);

            var deleteBtn = CreateActionButton("🗑️", "#EF4444", 2, es ? "Eliminar mod" : "Delete mod");
            deleteBtn.Click += (s, e) => DeleteMod(mod);
            buttonsGrid.Children.Add(deleteBtn);

            mainStack.Children.Add(buttonsGrid);
            card.Child = mainStack;
            return card;
        }

        private Button CreateActionButton(string content, string color, int column, string tooltip)
        {
            var btn = new Button
            {
                Content = content,
                FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = column == 0 ? new Thickness(0, 0, 4, 0) : column == 1 ? new Thickness(2, 0, 2, 0) : new Thickness(4, 0, 0, 0),
                ToolTip = tooltip
            };
            Grid.SetColumn(btn, column);
            return btn;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();
            return (words[0][0].ToString() + words[1][0].ToString()).ToUpper();
        }

        private void RefreshCurrentView()
        {
            if (_currentView == "favorites")
            {
                var favMods = _allMods.Where(m => _favorites.Contains(m.Id)).ToList();
                _filteredMods = favMods;
                DisplayMods(favMods);
            }
            else if (_currentView == "collection" && !string.IsNullOrEmpty(_currentCollectionName))
            {
                var modsInCollection = _allMods.Where(m => _collections[_currentCollectionName].Contains(m.Id)).ToList();
                _filteredMods = modsInCollection;
                DisplayMods(modsInCollection);
            }
            else
            {
                _filteredMods.Clear();
                DisplayMods(_allMods);
            }
        }

        #region Mod Actions

        private async Task DownloadMod(ModItem mod)
        {
            try
            {
                bool es = LanguageManager.IsSpanish;

                if (string.IsNullOrEmpty(_modsFolder) || !Directory.Exists(_modsFolder))
                {
                    MessageBox.Show(es ? "Por favor selecciona la carpeta Mods primero." : "Please select the Mods folder first.", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(es ? $"¿Descargar e instalar '{mod.NameES}'?" : $"Download and install '{mod.Name}'?", es ? "Confirmar" : "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                string tempZip = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{mod.FileName}.zip");

                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(mod.DownloadUrl);
                    File.WriteAllBytes(tempZip, data);
                }

                ZipFile.ExtractToDirectory(tempZip, _modsFolder);
                File.Delete(tempZip);

                MessageBox.Show(es ? $"¡Mod '{mod.NameES}' instalado correctamente!" : $"Mod '{mod.Name}' installed successfully!", es ? "Éxito" : "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(es ? $"Error descargando mod: {ex.Message}" : $"Error downloading mod: {ex.Message}", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DownloadCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            if (string.IsNullOrEmpty(_currentCollectionName)) return;

            var modsInCollection = _allMods.Where(m => _collections[_currentCollectionName].Contains(m.Id)).ToList();

            var result = MessageBox.Show(
                es ? $"¿Descargar toda la colección '{_currentCollectionName}' ({modsInCollection.Count} mods)?"
                   : $"Download entire collection '{_currentCollectionName}' ({modsInCollection.Count} mods)?",
                es ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            foreach (var mod in modsInCollection)
            {
                if (!mod.RequiresManualInstall)
                {
                    await DownloadMod(mod);
                }
            }
        }

        private void UninstallCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            if (string.IsNullOrEmpty(_currentCollectionName)) return;

            var modsInCollection = _allMods.Where(m => _collections[_currentCollectionName].Contains(m.Id)).ToList();

            var result = MessageBox.Show(
                es ? $"¿Desinstalar toda la colección '{_currentCollectionName}' ({modsInCollection.Count} mods)?"
                   : $"Uninstall entire collection '{_currentCollectionName}' ({modsInCollection.Count} mods)?",
                es ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            foreach (var mod in modsInCollection)
            {
                DeleteMod(mod);
            }
        }

        private void OpenPatreonPage(ModItem mod)
        {
            try
            {
                bool es = LanguageManager.IsSpanish;
                var result = MessageBox.Show(es ? $"Este mod requiere instalación manual.\n\n¿Abrir la página de Patreon de {mod.Author}?" : $"This mod requires manual installation.\n\nOpen {mod.Author}'s Patreon page?", es ? "Instalación Manual" : "Manual Installation", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = mod.PatreonUrl, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(es ? $"Error abriendo página: {ex.Message}" : $"Error opening page: {ex.Message}", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupMod(ModItem mod)
        {
            try
            {
                bool es = LanguageManager.IsSpanish;

                if (string.IsNullOrEmpty(_modsFolder) || !Directory.Exists(_modsFolder))
                {
                    MessageBox.Show(es ? "Por favor selecciona la carpeta Mods primero." : "Please select the Mods folder first.", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string backupFolder = System.IO.Path.Combine(GetModManagerFolderPath(), "backups");
                if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = System.IO.Path.Combine(backupFolder, $"{mod.FileName}_{timestamp}.zip");

                var modFiles = Directory.GetFiles(_modsFolder, $"*{mod.FileName}*", SearchOption.AllDirectories);

                if (modFiles.Length == 0)
                {
                    MessageBox.Show(es ? "Mod no encontrado en la carpeta Mods." : "Mod not found in Mods folder.", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var zip = ZipFile.Open(backupPath, ZipArchiveMode.Create))
                {
                    foreach (var file in modFiles)
                    {
                        zip.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
                    }
                }

                MessageBox.Show(es ? $"Backup creado:\n{backupPath}" : $"Backup created:\n{backupPath}", es ? "Éxito" : "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(es ? $"Error creando backup: {ex.Message}" : $"Error creating backup: {ex.Message}", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteMod(ModItem mod)
        {
            try
            {
                bool es = LanguageManager.IsSpanish;

                if (string.IsNullOrEmpty(_modsFolder) || !Directory.Exists(_modsFolder))
                {
                    MessageBox.Show(es ? "Por favor selecciona la carpeta Mods primero." : "Please select the Mods folder first.", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(es ? $"¿Eliminar '{mod.NameES}'?\n\nEsta acción no se puede deshacer." : $"Delete '{mod.Name}'?\n\nThis action cannot be undone.", es ? "Confirmar Eliminación" : "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                var modFiles = Directory.GetFiles(_modsFolder, $"*{mod.FileName}*", SearchOption.AllDirectories);
                foreach (var file in modFiles) File.Delete(file);

                MessageBox.Show(es ? $"Mod '{mod.NameES}' eliminado correctamente." : $"Mod '{mod.Name}' deleted successfully.", es ? "Éxito" : "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(es ? $"Error eliminando mod: {ex.Message}" : $"Error deleting mod: {ex.Message}", es ? "Error" : "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Collections Management

        private void AddModToCollection(ModItem mod)
        {
            bool es = LanguageManager.IsSpanish;

            var dialog = new Window
            {
                Title = es ? "Añadir a Colección" : "Add to Collection",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                WindowStyle = WindowStyle.ToolWindow
            };

            var mainStack = new StackPanel { Margin = new Thickness(20) };

            mainStack.Children.Add(new TextBlock { Text = es ? "Selecciona una colección:" : "Select a collection:", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) });

            var listBox = new ListBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Height = 120, Margin = new Thickness(0, 0, 0, 10) };

            foreach (var collection in _collections.Keys)
            {
                listBox.Items.Add(collection);
            }

            mainStack.Children.Add(listBox);

            var newCollectionStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var newCollectionTextBox = new TextBox { Width = 200, Padding = new Thickness(8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), Foreground = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")) };
            var createBtn = new Button { Content = es ? "➕ Crear" : "➕ Create", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 6, 12, 6), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };

            createBtn.Click += (s, e) =>
            {
                string newName = newCollectionTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(newName) && !_collections.ContainsKey(newName))
                {
                    _collections[newName] = new List<string>();
                    listBox.Items.Add(newName);
                    listBox.SelectedItem = newName;
                    newCollectionTextBox.Clear();
                    SaveCollections();
                }
            };

            newCollectionStack.Children.Add(newCollectionTextBox);
            newCollectionStack.Children.Add(createBtn);
            mainStack.Children.Add(newCollectionStack);

            var addBtn = new Button { Content = es ? "Añadir" : "Add", Padding = new Thickness(20, 10, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Center };

            addBtn.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string selectedCollection = listBox.SelectedItem.ToString();
                    if (!_collections[selectedCollection].Contains(mod.Id))
                    {
                        _collections[selectedCollection].Add(mod.Id);
                        SaveCollections();
                        MessageBox.Show(es ? $"Mod añadido a '{selectedCollection}'" : $"Mod added to '{selectedCollection}'", es ? "Éxito" : "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        dialog.Close();
                    }
                    else
                    {
                        MessageBox.Show(es ? "El mod ya está en esta colección" : "Mod already in this collection", es ? "Info" : "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            };

            mainStack.Children.Add(addBtn);
            dialog.Content = mainStack;
            dialog.ShowDialog();
        }

        private void ShowCollectionsButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            var dialog = new Window
            {
                Title = es ? "Mis Colecciones" : "My Collections",
                Width = 500,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                WindowStyle = WindowStyle.ToolWindow
            };

            var mainStack = new StackPanel { Margin = new Thickness(20) };

            mainStack.Children.Add(new TextBlock { Text = es ? "📁 Mis Colecciones" : "📁 My Collections", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 20) });

            var listBox = new ListBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Height = 350 };

            foreach (var collection in _collections)
            {
                listBox.Items.Add($"{collection.Key} ({collection.Value.Count} mods)");
            }

            mainStack.Children.Add(listBox);

            var buttonsStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };

            var viewBtn = new Button { Content = es ? "Ver Mods" : "View Mods", Padding = new Thickness(15, 8, 15, 8), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            viewBtn.Click += (sender2, args2) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string selected = listBox.SelectedItem.ToString();
                    string collectionName = selected.Substring(0, selected.IndexOf(" ("));

                    SearchBox.Text = LanguageManager.IsSpanish ? "Buscar mods..." : "Search mods...";
                    SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

                    _currentView = "collection";
                    _currentCollectionName = collectionName;

                    var modsInCollection = _allMods.Where(m => _collections[collectionName].Contains(m.Id)).ToList();
                    _filteredMods = modsInCollection;
                    DisplayMods(modsInCollection);
                    dialog.Close();
                }
            };

            var deleteBtn = new Button { Content = es ? "Eliminar" : "Delete", Padding = new Thickness(15, 8, 15, 8), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            deleteBtn.Click += (sender3, args3) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string selected = listBox.SelectedItem.ToString();
                    string collectionName = selected.Substring(0, selected.IndexOf(" ("));
                    var result = MessageBox.Show(es ? $"¿Eliminar colección '{collectionName}'?" : $"Delete collection '{collectionName}'?", es ? "Confirmar" : "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        _collections.Remove(collectionName);
                        SaveCollections();
                        listBox.Items.Remove(selected);
                    }
                }
            };

            var closeBtn = new Button { Content = es ? "Cerrar" : "Close", Padding = new Thickness(15, 8, 15, 8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            closeBtn.Click += (sender4, args4) => dialog.Close();

            buttonsStack.Children.Add(viewBtn);
            buttonsStack.Children.Add(deleteBtn);
            buttonsStack.Children.Add(closeBtn);

            mainStack.Children.Add(buttonsStack);
            dialog.Content = mainStack;
            dialog.ShowDialog();
        }
        #endregion

        #region Event Handlers

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            bool es = LanguageManager.IsSpanish;

            // Si el texto es el placeholder, ignorar
            if (searchText == (es ? "buscar mods..." : "search mods...").ToLower())
            {
                return;
            }

            // Si está vacío, restaurar la vista actual
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshCurrentView();
                return;
            }

            // Filtrar según la vista actual
            List<ModItem> modsToSearch = new List<ModItem>();

            if (_currentView == "favorites")
            {
                modsToSearch = _allMods.Where(m => _favorites.Contains(m.Id)).ToList();
            }
            else if (_currentView == "collection" && !string.IsNullOrEmpty(_currentCollectionName))
            {
                modsToSearch = _allMods.Where(m => _collections[_currentCollectionName].Contains(m.Id)).ToList();
            }
            else
            {
                modsToSearch = _allMods;
            }

            // Aplicar búsqueda
            _filteredMods = modsToSearch.Where(m =>
                m.Name.ToLower().Contains(searchText) ||
                m.NameES.ToLower().Contains(searchText) ||
                m.Author.ToLower().Contains(searchText) ||
                m.Description.ToLower().Contains(searchText) ||
                m.DescriptionES.ToLower().Contains(searchText)
            ).ToList();

            DisplayMods(_filteredMods);
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;
            if (SearchBox.Text == (es ? "Buscar mods..." : "Search mods..."))
            {
                SearchBox.Text = "";
                SearchBox.Foreground = Brushes.White;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = es ? "Buscar mods..." : "Search mods...";
                SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            }
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = LanguageManager.IsSpanish ? "Buscar mods..." : "Search mods...";
            SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            _currentView = "all";
            _currentCollectionName = "";
            _filteredMods.Clear();
            DisplayMods(_allMods);
        }
        private void ShowFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = LanguageManager.IsSpanish ? "Buscar mods..." : "Search mods...";
            SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            _currentView = "favorites";
            _currentCollectionName = "";
            var favMods = _allMods.Where(m => _favorites.Contains(m.Id)).ToList();
            _filteredMods = favMods;
            DisplayMods(favMods);
        }

        private async void DownloadAllFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            var result = MessageBox.Show(
                es ? $"¿Descargar todos los favoritos ({_favorites.Count} mods)?"
                   : $"Download all favorites ({_favorites.Count} mods)?",
                es ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            foreach (var favId in _favorites)
            {
                var mod = _allMods.FirstOrDefault(m => m.Id == favId);
                if (mod != null && !mod.RequiresManualInstall)
                {
                    await DownloadMod(mod);
                }
            }
        }

        private void UninstallAllFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            var result = MessageBox.Show(
                es ? $"¿Desinstalar todos los favoritos ({_favorites.Count} mods)?"
                   : $"Uninstall all favorites ({_favorites.Count} mods)?",
                es ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            foreach (var favId in _favorites)
            {
                var mod = _allMods.FirstOrDefault(m => m.Id == favId);
                if (mod != null)
                {
                    DeleteMod(mod);
                }
            }
        }

        private void UploadCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            var result = MessageBox.Show(
                es ? "Para subir tu colección:\n\n1. Únete a nuestro Discord: discord.gg/JYnpPt4nUu\n2. Ve al canal #colecciones\n3. Comparte tu archivo de colección\n4. Si es aprobada, será publicada\n\n¿Abrir Discord ahora?"
                   : "To upload your collection:\n\n1. Join our Discord: discord.gg/JYnpPt4nUu\n2. Go to #collections channel\n3. Share your collection file\n4. If approved, it will be published\n\nOpen Discord now?",
                es ? "Subir Colección" : "Upload Collection",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/JYnpPt4nUu",
                    UseShellExecute = true
                });
            }
        }

        private void TestGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool es = LanguageManager.IsSpanish;

                if (string.IsNullOrEmpty(_gameFolder) || !Directory.Exists(_gameFolder))
                {
                    MessageBox.Show(
                        es ? "Por favor configura la carpeta del juego primero." : "Please configure the game folder first.",
                        es ? "Error" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                string[] possiblePaths = new[]
                {
                    System.IO.Path.Combine(_gameFolder, "Game-cracked", "Bin", "TS4_x64.exe"),
                    System.IO.Path.Combine(_gameFolder, "Game", "Bin", "TS4_x64.exe")
                };

                string exePath = possiblePaths.FirstOrDefault(p => File.Exists(p));

                if (exePath == null)
                {
                    MessageBox.Show(
                        es ? "No se encontró TS4_x64.exe en la carpeta del juego." : "TS4_x64.exe not found in game folder.",
                        es ? "Error" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                MessageBox.Show(
                    es ? "¡Juego iniciado! Prueba tus mods." : "Game started! Test your mods.",
                    es ? "Éxito" : "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(
                    es ? $"Error iniciando juego: {ex.Message}" : $"Error starting game: {ex.Message}",
                    es ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            MessageBox.Show(
                es ? "Esta función está en Beta.\n\nVerificará si tus mods están actualizados con la última versión del juego."
                   : "This feature is in Beta.\n\nIt will check if your mods are updated with the latest game version.",
                es ? "Beta" : "Beta",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // TODO: Implement update checking logic
        }

        private void ChangeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = es ? "Selecciona la carpeta Mods de The Sims 4" : "Select The Sims 4 Mods folder",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                _modsFolder = dialog.SelectedPath;
                ModsFolderText.Text = AbbreviatePath(_modsFolder, 50);
            }
        }

        private void ChangeGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;

            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = es ? "Selecciona la carpeta raíz de The Sims 4" : "Select The Sims 4 root folder",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                _gameFolder = dialog.SelectedPath;
                GameFolderText.Text = AbbreviatePath(_gameFolder, 50);
                SaveGameFolder();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #endregion
    }

    #region Data Models

    public class ModDatabase
    {
        [JsonProperty("mods")]
        public List<ModDatabaseEntry> Mods { get; set; }
    }

    public class ModDatabaseEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nameES")]
        public string NameES { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("descriptionES")]
        public string DescriptionES { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("gameVersion")]
        public string GameVersion { get; set; }

        [JsonProperty("requiresManualInstall")]
        public bool RequiresManualInstall { get; set; }

        [JsonProperty("patreonUrl")]
        public string PatreonUrl { get; set; }

        [JsonProperty("accentColor")]
        public string AccentColor { get; set; }
    }

    public class ModItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameES { get; set; }
        public string Description { get; set; }
        public string DescriptionES { get; set; }
        public string Author { get; set; }
        public string DownloadUrl { get; set; }
        public string FileName { get; set; }
        public string Version { get; set; }
        public string GameVersion { get; set; }
        public bool RequiresManualInstall { get; set; }
        public string PatreonUrl { get; set; }
        public string AccentColor { get; set; }
    }

    #endregion
}