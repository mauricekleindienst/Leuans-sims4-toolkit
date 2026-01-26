using ModernDesign.Localization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ModernDesign.MVVM.View
{
    public partial class InstallMethodSelectorWindow : Window
    {
        private List<GameLibraryItem> _games = new List<GameLibraryItem>();

        public InstallMethodSelectorWindow()
        {
            InitializeComponent();

            // Borrar ETag cacheado al iniciar
            try
            {
                string cachedETagPath = GetCachedETagPath();
                if (File.Exists(cachedETagPath))
                {
                    File.Delete(cachedETagPath);
                    Debug.WriteLine("🗑️ Deleted cached ETag - will fetch fresh from GitHub");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting cached ETag: {ex.Message}");
            }

            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = "🎮 Biblioteca de Juegos";
                SubHeaderText.Text = "Selecciona el juego que deseas instalar";
                CloseBtn.Content = "❌ Cerrar";
            }
            else
            {
                HeaderText.Text = "🎮 Game Library";
                SubHeaderText.Text = "Select the game you want to install";
                CloseBtn.Content = "❌ Close";
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Cargar juegos desde GitHub/caché
            _games = await LoadGamesFromDatabase();
            CreateGameCards();
        }

        // ============================================================
        // ETAG & CACHE MANAGEMENT
        // ============================================================

        private async Task<string> GetGitHubETag()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Leuans-Sims4-Toolkit");
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    var request = new HttpRequestMessage(HttpMethod.Head,
                        "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/games_library.json");

                    var response = await httpClient.SendAsync(request);

                    if (response.Headers.ETag != null)
                    {
                        return response.Headers.ETag.Tag.Trim('"');
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting ETag: {ex.Message}");
            }

            return null;
        }

        private string GetCacheFolderPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cacheFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "qol", "databases_cache", "GamesLibrary");

            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            return cacheFolder;
        }

        private string GetCachedDatabasePath()
        {
            return Path.Combine(GetCacheFolderPath(), "games_library.json");
        }

        private string GetCachedETagPath()
        {
            return Path.Combine(GetCacheFolderPath(), "games_library_etag.txt");
        }

        private string GetCachedImagesPath()
        {
            string cacheFolder = GetCacheFolderPath();
            string imagesFolder = Path.Combine(cacheFolder, "covers");

            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            return imagesFolder;
        }

        private async Task<string> GetCachedCoverImage(string gameId, string coverImageUrl)
        {
            try
            {
                string imagesFolder = GetCachedImagesPath();
                string extension = Path.GetExtension(new Uri(coverImageUrl).AbsolutePath);
                if (string.IsNullOrEmpty(extension))
                    extension = ".jpg";

                string cachedImagePath = Path.Combine(imagesFolder, $"{gameId}{extension}");

                // Si ya existe localmente, usarla
                if (File.Exists(cachedImagePath))
                {
                    Debug.WriteLine($"Using cached cover for {gameId}");
                    return cachedImagePath;
                }

                // Descargar y guardar
                Debug.WriteLine($"📥 Downloading cover for {gameId}...");
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    byte[] imageData = await httpClient.GetByteArrayAsync(coverImageUrl);
                    File.WriteAllBytes(cachedImagePath, imageData);
                    Debug.WriteLine($"Cover cached for {gameId}");
                }

                return cachedImagePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error caching cover for {gameId}: {ex.Message}");
                return null;
            }
        }

        private async Task<List<GameLibraryItem>> LoadGamesFromDatabase()
        {
            try
            {
                string cachedDatabasePath = GetCachedDatabasePath();
                string cachedETagPath = GetCachedETagPath();

                // Obtener ETag actual de GitHub
                string currentETag = await GetGitHubETag();

                // Leer ETag guardado localmente
                string cachedETag = null;
                if (File.Exists(cachedETagPath))
                {
                    cachedETag = File.ReadAllText(cachedETagPath).Trim();
                }

                string jsonContent;

                // Si el archivo existe, el ETag no cambió
                if (File.Exists(cachedDatabasePath) && currentETag != null && cachedETag == currentETag)
                {
                    Debug.WriteLine("Using cached games library (ETag matches)");
                    jsonContent = File.ReadAllText(cachedDatabasePath);
                }
                else
                {
                    // Descargar desde GitHub
                    Debug.WriteLine("📥 Downloading fresh games library from GitHub...");
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(10);

                        jsonContent = await httpClient.GetStringAsync(
                            "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/games_library.json");

                        // Guardar en caché
                        File.WriteAllText(cachedDatabasePath, jsonContent);

                        // Guardar ETag
                        if (currentETag != null)
                        {
                            File.WriteAllText(cachedETagPath, currentETag);
                        }

                        Debug.WriteLine("Games library cached successfully");
                    }
                }

                // Deserializar JSON
                var database = JsonConvert.DeserializeObject<GamesLibraryDatabase>(jsonContent);

                // Convertir a GameLibraryItem
                var games = new List<GameLibraryItem>();
                foreach (var entry in database.Games)
                {
                    games.Add(new GameLibraryItem
                    {
                        Id = entry.Id,
                        Name = entry.Name,
                        NameES = entry.NameES,
                        CoverImageUrl = entry.CoverImageUrl,
                        ActionType = entry.ActionType,
                        WindowName = entry.WindowName,
                        RequiresVault = entry.RequiresVault,
                        WebsiteUrl = entry.WebsiteUrl
                    });
                }

                Debug.WriteLine($"Loaded {games.Count} games");
                return games;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading games library: {ex.Message}");
                return new List<GameLibraryItem>();
            }
        }

        // ============================================================
        // CREATE GAME CARDS
        // ============================================================

        private async void CreateGameCards()
        {
            bool isSpanish = IsSpanishLanguage();

            foreach (var game in _games)
            {
                // Card container
                Border card = new Border
                {
                    Width = 180,
                    Height = 260,
                    Margin = new Thickness(10),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                    Cursor = Cursors.Hand,
                    Tag = game
                };

                card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.5,
                    Color = Colors.Black
                };

                Grid cardGrid = new Grid();

                // NUEVO: Obtener imagen desde caché o descargar
                string cachedImagePath = await GetCachedCoverImage(game.Id, game.CoverImageUrl);

                // Cover Image
                if (!string.IsNullOrEmpty(cachedImagePath) && File.Exists(cachedImagePath))
                {
                    try
                    {
                        Image coverImage = new Image
                        {
                            Stretch = Stretch.UniformToFill
                        };

                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(cachedImagePath, UriKind.Absolute);
                        bitmap.EndInit();

                        coverImage.Source = bitmap;
                        cardGrid.Children.Add(coverImage);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error loading cached image for {game.Id}: {ex.Message}");
                        AddFallbackGradient(cardGrid, game, isSpanish);
                    }
                }
                else
                {
                    // Fallback: gradiente si no hay imagen
                    AddFallbackGradient(cardGrid, game, isSpanish);
                }

                // Hover overlay (inicialmente oculto)
                Border hoverOverlay = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    Opacity = 0,
                    Name = "HoverOverlay"
                };

                Grid overlayGrid = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center
                };

                Button downloadBtn = new Button
                {
                    Content = "⬇️ Download",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(20, 10, 20, 10),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Tag = game
                };

                downloadBtn.Style = (Style)FindResource("ActionButton");
                downloadBtn.Click += GameCard_Click;

                overlayGrid.Children.Add(downloadBtn);
                hoverOverlay.Child = overlayGrid;
                cardGrid.Children.Add(hoverOverlay);

                // Game title at bottom
                Border titleBar = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 30, 41, 59)),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Padding = new Thickness(10)
                };

                TextBlock titleText = new TextBlock
                {
                    Text = isSpanish ? game.NameES : game.Name,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Center
                };

                titleBar.Child = titleText;
                cardGrid.Children.Add(titleBar);

                card.Child = cardGrid;

                // Hover animations
                card.MouseEnter += (s, e) =>
                {
                    var storyboard = new Storyboard();
                    var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
                    Storyboard.SetTarget(fadeIn, hoverOverlay);
                    Storyboard.SetTargetProperty(fadeIn, new PropertyPath(Border.OpacityProperty));
                    storyboard.Children.Add(fadeIn);
                    storyboard.Begin();
                };

                card.MouseLeave += (s, e) =>
                {
                    var storyboard = new Storyboard();
                    var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
                    Storyboard.SetTarget(fadeOut, hoverOverlay);
                    Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Border.OpacityProperty));
                    storyboard.Children.Add(fadeOut);
                    storyboard.Begin();
                };

                GamesPanel.Children.Add(card);
            }
        }

        // NUEVO: Método helper para crear gradiente de fallback
        private void AddFallbackGradient(Grid cardGrid, GameLibraryItem game, bool isSpanish)
        {
            Border fallback = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#667eea"), 0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#764ba2"), 1)
                    }
                }
            };

            Grid fallbackGrid = new Grid();
            TextBlock placeholderText = new TextBlock
            {
                Text = isSpanish ? game.NameES : game.Name,
                Foreground = Brushes.White,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };

            fallbackGrid.Children.Add(placeholderText);
            fallback.Child = fallbackGrid;
            cardGrid.Children.Add(fallback);
        }


        // ============================================================
        // GAME CARD CLICK HANDLER
        // ============================================================

        private void GameCard_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            GameLibraryItem game = btn.Tag as GameLibraryItem;

            if (game.RequiresVault)
            {
                ShowVaultPopup();
            }
            else if (game.ActionType == "openWindow")
            {
                OpenGameWindow(game.WindowName);
            }
            else if (game.ActionType == "openWebsite")
            {
                OpenWebsite(game.WebsiteUrl);
            }
        }

        private void OpenGameWindow(string windowName)
        {
            try
            {
                Window window = null;

                switch (windowName)
                {
                    case "InstallModeSelector":
                        window = new InstallModeSelector();
                        break;
                    case "Sims3Updater":
                        window = new Sims3Downloader(); 
                        break;
                    case "Sims3Downloader":
                        window = new Sims3Downloader();
                        break;
                    default:
                        MessageBox.Show($"Window '{windowName}' not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                }

                if (window != null)
                {
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenWebsite(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("No website URL provided", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Abrir en el navegador predeterminado
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening website: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowVaultPopup()
        {
            var vaultPopup = new VaultAccessPopupWindow();
            vaultPopup.Owner = this;
            vaultPopup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            vaultPopup.ShowDialog();
        }

        // ============================================================
        // HELPERS
        // ============================================================

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

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }



    // ============================================================
    // DATA CLASSES
    // ============================================================

    public class GameLibraryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameES { get; set; }
        public string CoverImageUrl { get; set; }
        public string ActionType { get; set; } // "openWindow", "showVaultPopup", "openWebsite"
        public string WindowName { get; set; }
        public bool RequiresVault { get; set; }
        public string WebsiteUrl { get; set; }
    }

    public class GamesLibraryDatabase
    {
        [JsonProperty("games")]
        public List<GameLibraryEntry> Games { get; set; }
    }

    public class GameLibraryEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nameES")]
        public string NameES { get; set; }

        [JsonProperty("coverImageUrl")]
        public string CoverImageUrl { get; set; }

        [JsonProperty("actionType")]
        public string ActionType { get; set; }

        [JsonProperty("windowName")]
        public string WindowName { get; set; }

        [JsonProperty("requiresVault")]
        public bool RequiresVault { get; set; }

        [JsonProperty("websiteUrl")]
        public string WebsiteUrl { get; set; }
    }
}