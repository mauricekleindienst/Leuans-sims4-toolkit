using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace ModernDesign.MVVM.View
{
    public partial class StaffView : UserControl
    {
        private string _languageCode = "en-US";
        private const string STAFF_JSON_URL = "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/StaffMembers.json";

        public event EventHandler NavigateBackRequested;

        public StaffView()
        {
            InitializeComponent();
            InitLocalization();
            Loaded += StaffView_Loaded;
        }

        private async void StaffView_Loaded(object sender, RoutedEventArgs e)
        {
            StartLoadingAnimation();
            await LoadStaffMembersAsync();
        }

        #region Localization

        private void InitLocalization()
        {
            LoadLanguageFromIni();
            bool isSpanish = IsSpanish();

            TitleText.Text = isSpanish ? "Miembros del Staff" : "Staff Members";
            SubtitleText.Text = isSpanish
                ? "Las increíbles personas detrás de este proyecto"
                : "The amazing people behind this project";
            BackButtonText.Text = isSpanish ? "Volver" : "Back";
            LoadingText.Text = isSpanish ? "Cargando miembros del staff..." : "Loading staff members...";
            RetryButtonText.Text = isSpanish ? "Reintentar" : "Retry";
        }

        private void LoadLanguageFromIni()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string iniPath = System.IO.Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

                if (!File.Exists(iniPath)) return;

                foreach (var line in File.ReadAllLines(iniPath))
                {
                    if (line.Trim().StartsWith("Language", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('=');
                        if (parts.Length >= 2)
                            _languageCode = parts[1].Trim();
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

        private bool IsSpanish()
        {
            return _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Data Loading

        private void StartLoadingAnimation()
        {
            var storyboard = (Storyboard)Resources["SpinnerAnimation"];
            storyboard.Begin();
        }

        private void StopLoadingAnimation()
        {
            var storyboard = (Storyboard)Resources["SpinnerAnimation"];
            storyboard.Stop();
        }

        private async Task LoadStaffMembersAsync()
        {
            HttpClient httpClient = null;
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorPanel.Visibility = Visibility.Collapsed;
                StaffItemsControl.Visibility = Visibility.Collapsed;

                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);

                var json = await httpClient.GetStringAsync(STAFF_JSON_URL);
                var staffData = JsonConvert.DeserializeObject<StaffData>(json);

                if (staffData != null && staffData.Members != null && staffData.Members.Count > 0)
                {
                    DisplayStaffMembers(staffData.Members);
                    StaffItemsControl.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowError(IsSpanish()
                        ? "No se encontraron miembros del staff"
                        : "No staff members found");
                }
            }
            catch (Exception ex)
            {
                ShowError(IsSpanish()
                    ? $"Error al cargar: {ex.Message}"
                    : $"Failed to load: {ex.Message}");
            }
            finally
            {
                if (httpClient != null)
                {
                    httpClient.Dispose();
                }
                StopLoadingAnimation();
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void DisplayStaffMembers(List<StaffMember> members)
        {
            StaffItemsControl.Items.Clear();

            foreach (var member in members)
            {
                var card = CreateStaffCard(member);
                StaffItemsControl.Items.Add(card);
            }
        }

        #endregion

        #region Card Creation

        private Border CreateStaffCard(StaffMember member)
        {
            // Main card border
            var card = new Border
            {
                Width = 280,
                MinHeight = 320,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181845")),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(member.AccentColor ?? "#A78BFA60")),
                Padding = new Thickness(20),
                Margin = new Thickness(10),
                Cursor = Cursors.Hand,
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString(member.AccentColor ?? "#A78BFA"),
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 0.2
                },
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(1, 1),
                        new TranslateTransform(0, 0)
                    }
                }
            };

            // Add hover animations
            AddCardHoverAnimation(card);

            var mainStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            // Profile Image
            var imageContainer = new Border
            {
                Width = 90,
                Height = 90,
                CornerRadius = new CornerRadius(45),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF10")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16),
                BorderThickness = new Thickness(3),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(member.AccentColor ?? "#A78BFA"))
            };

            if (!string.IsNullOrEmpty(member.ImageUrl))
            {
                try
                {
                    var image = new Image
                    {
                        Width = 84,
                        Height = 84,
                        Stretch = Stretch.UniformToFill
                    };

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(member.ImageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    image.Source = bitmap;

                    // Circular clip
                    image.Clip = new EllipseGeometry(new System.Windows.Point(42, 42), 42, 42);
                    imageContainer.Child = image;
                }
                catch
                {
                    imageContainer.Child = new TextBlock
                    {
                        Text = member.Emoji ?? "👤",
                        FontSize = 40,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
            else
            {
                imageContainer.Child = new TextBlock
                {
                    Text = member.Emoji ?? "👤",
                    FontSize = 40,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            mainStack.Children.Add(imageContainer);

            // Name with emoji
            var nameStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            if (!string.IsNullOrEmpty(member.Emoji))
            {
                nameStack.Children.Add(new TextBlock
                {
                    Text = member.Emoji,
                    FontSize = 18,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            nameStack.Children.Add(new TextBlock
            {
                Text = member.Name,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            mainStack.Children.Add(nameStack);

            // Role badge
            if (!string.IsNullOrEmpty(member.Role))
            {
                var roleBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(member.AccentColor ?? "#A78BFA")) { Opacity = 0.2 },
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(0, 8, 0, 12),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                roleBadge.Child = new TextBlock
                {
                    Text = member.Role,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(member.AccentColor ?? "#A78BFA"))
                };

                mainStack.Children.Add(roleBadge);
            }

            // Description
            bool isSpanish = IsSpanish();
            string description = isSpanish && !string.IsNullOrEmpty(member.DescriptionEs)
                ? member.DescriptionEs
                : member.Description;

            if (!string.IsNullOrEmpty(description))
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0")),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16),
                    MaxWidth = 240
                });
            }

            // Social Links
            var linksPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            // Discord
            if (!string.IsNullOrEmpty(member.Discord))
            {
                var discordBtn = CreateSocialButton("💬", member.Discord, "#5865F2", () => CopyToClipboard(member.Discord, "Discord"));
                linksPanel.Children.Add(discordBtn);
            }

            // Ko-fi
            if (!string.IsNullOrEmpty(member.Kofi))
            {
                var kofiBtn = CreateSocialButton("☕", "Ko-fi", "#FF5E5B", () => OpenUrl(member.Kofi));
                linksPanel.Children.Add(kofiBtn);
            }

            // Twitter
            if (!string.IsNullOrEmpty(member.Twitter))
            {
                var twitterBtn = CreateSocialButton("🐦", "Twitter", "#1DA1F2", () => OpenUrl(member.Twitter));
                linksPanel.Children.Add(twitterBtn);
            }

            // Website
            if (!string.IsNullOrEmpty(member.Website))
            {
                var websiteBtn = CreateSocialButton("🌐", "Web", "#10B981", () => OpenUrl(member.Website));
                linksPanel.Children.Add(websiteBtn);
            }

            mainStack.Children.Add(linksPanel);

            card.Child = mainStack;
            return card;
        }

        private Border CreateSocialButton(string emoji, string label, string color, Action onClick)
        {
            var btn = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) { Opacity = 0.15 },
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(4),
                Cursor = Cursors.Hand,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 12,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                VerticalAlignment = VerticalAlignment.Center
            });

            btn.Child = stack;

            btn.MouseEnter += (s, e) =>
            {
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) { Opacity = 0.3 };
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) { Opacity = 0.15 };
            };

            btn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                if (onClick != null)
                {
                    onClick.Invoke();
                }
            };

            return btn;
        }

        private void AddCardHoverAnimation(Border card)
        {
            card.MouseEnter += (s, e) =>
            {
                var transform = card.RenderTransform as TransformGroup;
                if (transform != null)
                {
                    var scale = transform.Children[0] as ScaleTransform;
                    var translate = transform.Children[1] as TranslateTransform;

                    var scaleXAnim = new DoubleAnimation(1.02, TimeSpan.FromMilliseconds(200));
                    var scaleYAnim = new DoubleAnimation(1.02, TimeSpan.FromMilliseconds(200));
                    var translateAnim = new DoubleAnimation(-4, TimeSpan.FromMilliseconds(200));

                    if (scale != null)
                    {
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                    }
                    if (translate != null)
                    {
                        translate.BeginAnimation(TranslateTransform.YProperty, translateAnim);
                    }
                }

                if (card.Effect is DropShadowEffect effect)
                {
                    var opacityAnim = new DoubleAnimation(0.35, TimeSpan.FromMilliseconds(200));
                    effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
            };

            card.MouseLeave += (s, e) =>
            {
                var transform = card.RenderTransform as TransformGroup;
                if (transform != null)
                {
                    var scale = transform.Children[0] as ScaleTransform;
                    var translate = transform.Children[1] as TranslateTransform;

                    var scaleXAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
                    var scaleYAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
                    var translateAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));

                    if (scale != null)
                    {
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                    }
                    if (translate != null)
                    {
                        translate.BeginAnimation(TranslateTransform.YProperty, translateAnim);
                    }
                }

                if (card.Effect is DropShadowEffect effect)
                {
                    var opacityAnim = new DoubleAnimation(0.2, TimeSpan.FromMilliseconds(200));
                    effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
            };
        }

        #endregion

        #region Actions

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    IsSpanish() ? $"Error al abrir: {ex.Message}" : $"Failed to open: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyToClipboard(string text, string label)
        {
            try
            {
                Clipboard.SetText(text);
                MessageBox.Show(
                    IsSpanish()
                        ? $"'{text}' copiado al portapapeles"
                        : $"'{text}' copied to clipboard",
                    IsSpanish() ? "✓ Copiado" : "✓ Copied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    IsSpanish() ? $"Error al copiar: {ex.Message}" : $"Failed to copy: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Event Handlers

        private void BackButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (NavigateBackRequested != null)
            {
                NavigateBackRequested.Invoke(this, EventArgs.Empty);
            }
        }

        private async void RetryButton_Click(object sender, MouseButtonEventArgs e)
        {
            StartLoadingAnimation();
            await LoadStaffMembersAsync();
        }

        #endregion
    }

    #region Data Models

    public class StaffData
    {
        [JsonProperty("members")]
        public List<StaffMember> Members { get; set; }
    }

    public class StaffMember
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("emoji")]
        public string Emoji { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("descriptionEs")]
        public string DescriptionEs { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("accentColor")]
        public string AccentColor { get; set; }

        [JsonProperty("discord")]
        public string Discord { get; set; }

        [JsonProperty("kofi")]
        public string Kofi { get; set; }

        [JsonProperty("twitter")]
        public string Twitter { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }
    }

    #endregion
}