using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ModernDesign.Managers;
using Newtonsoft.Json;

namespace ModernDesign
{
    public partial class ThemeSelector : Window
    {
        private ThemeData _selectedTheme = null;
        private bool _isDeveloperMode = false;
        private const string THEMES_URL = "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/UIThemes.json";

        public ThemeData SelectedTheme => _selectedTheme;

        public ThemeSelector()
        {
            InitializeComponent();
            _isDeveloperMode = DeveloperModeManager.IsDeveloperModeUnlocked();
            LoadThemesAsync();
        }

        private async void LoadThemesAsync()
        {
            try
            {
                // Show loading message
                txtThemeHint.Text = "Loading themes...";
                txtThemeHint.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B95A8"));

                List<ThemeData> themes = await FetchThemesFromGitHubAsync();

                if (themes == null || themes.Count == 0)
                {
                    // Fallback to default themes if GitHub fails
                    themes = GetDefaultThemes();
                    txtThemeHint.Text = "Using offline themes (GitHub unavailable)";
                }
                else
                {
                    txtThemeHint.Text = "Please select a theme to continue";
                }

                // Apply developer mode lock status
                foreach (var theme in themes)
                {
                    if (theme.IsLocked && _isDeveloperMode)
                    {
                        theme.IsLocked = false; // Unlock for developer mode users
                    }
                }

                // Sort by order
                themes.Sort((a, b) => a.Order.CompareTo(b.Order));

                // Create UI buttons
                foreach (var theme in themes)
                {
                    Button themeButton = new Button
                    {
                        Content = theme.Name,
                        Style = (Style)FindResource("ThemeCardButton"),
                        Tag = theme
                    };

                    themeButton.Click += ThemeButton_Click;
                    ThemeItemsControl.Items.Add(themeButton);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading themes: {ex.Message}\n\nUsing default themes instead.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                LoadDefaultThemes();
            }
        }

        private async Task<List<ThemeData>> FetchThemesFromGitHubAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string jsonContent = await client.GetStringAsync(THEMES_URL);

                    var themeResponse = JsonConvert.DeserializeObject<ThemeResponse>(jsonContent);

                    return themeResponse?.Themes ?? new List<ThemeData>();
                }
            }
            catch
            {
                return null;
            }
        }

        private List<ThemeData> GetDefaultThemes()
        {
            return new List<ThemeData>
            {
                new ThemeData
                {
                    Name = "Iconic (Default)",
                    Color1 = "#22D3EE",
                    Color2 = "#1E293B",
                    Color3 = "#21b96b",
                    IsLocked = false,
                    IsFree = true,
                    Order = 1
                },
                new ThemeData
                {
                    Name = "Midnight Black",
                    Color1 = "#1a1a1a",
                    Color2 = "#0a0a0a",
                    Color3 = "#2d2d2d",
                    IsLocked = false,
                    IsFree = true,
                    Order = 2
                },
                new ThemeData
                {
                    Name = "Pure White",
                    Color1 = "#f5f5f5",
                    Color2 = "#ffffff",
                    Color3 = "#e0e0e0",
                    IsLocked = false,
                    IsFree = true,
                    Order = 3
                }
            };
        }

        private void LoadDefaultThemes()
        {
            var themes = GetDefaultThemes();

            foreach (var theme in themes)
            {
                Button themeButton = new Button
                {
                    Content = theme.Name,
                    Style = (Style)FindResource("ThemeCardButton"),
                    Tag = theme
                };

                themeButton.Click += ThemeButton_Click;
                ThemeItemsControl.Items.Add(themeButton);
            }

            txtThemeHint.Text = "Please select a theme to continue";
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            ThemeData theme = clickedButton.Tag as ThemeData;

            if (theme.IsLocked)
            {
                MessageBox.Show(
                    "This theme is locked and requires Developer Mode.\n\n" +
                    "To unlock Developer Mode, you need to:\n" +
                    "✓ Earn all Gold medals in tutorials\n" +
                    "✓ Visit all features in the toolkit\n" +
                    "✓ Support the project on Patreon",
                    "Theme Locked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _selectedTheme = theme;
            btnConfirm.IsEnabled = true;
            txtThemeHint.Text = $"✓ Selected: {theme.Name}";
            txtThemeHint.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));

            // Visual feedback - highlight selected
            foreach (Button btn in ThemeItemsControl.Items)
            {
                if (btn == clickedButton)
                {
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D9FF"));
                    btn.BorderThickness = new Thickness(3);
                }
                else
                {
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3244"));
                    btn.BorderThickness = new Thickness(2);
                }
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTheme != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }

    // JSON Deserialization Classes
    public class ThemeResponse
    {
        public string Version { get; set; }
        public List<ThemeData> Themes { get; set; }
    }

    public class ThemeData
    {
        public string Name { get; set; }
        public string Color1 { get; set; }
        public string Color2 { get; set; }
        public string Color3 { get; set; }
        public bool IsLocked { get; set; }
        public bool IsFree { get; set; }
        public int Order { get; set; }
    }
}