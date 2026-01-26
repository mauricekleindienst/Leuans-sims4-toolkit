using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class DLCSelectorWindow : Window
    {
        public ObservableCollection<DLCItem> AllDLCItems { get; set; }
        public HashSet<string> ExcludedFolders { get; private set; }

        private const string IMG_BASE_URL = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/";

        private bool _shouldLoadImages = false;

        public DLCSelectorWindow(bool shouldLoadImages = false)
        {
            InitializeComponent();
            _shouldLoadImages = shouldLoadImages;
            ApplyLanguage();
            InitializeDLCList();
            BuildCategoriesUI();
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = "⚙️ Selecciona los DLCs a Escanear";
                SubHeaderText.Text = "Haz clic en las portadas para incluir o excluir del escaneo";
                SelectAllBtn.Content = "✓ Seleccionar Todo";
                DeselectAllBtn.Content = "✗ Deseleccionar Todo";
                CancelBtn.Content = "❌ Cancelar";
                ConfirmBtn.Content = "✓ Confirmar Selección";
            }
            else
            {
                HeaderText.Text = "⚙️ Select DLCs to Scan";
                SubHeaderText.Text = "Click on covers to include or exclude from scan";
                SelectAllBtn.Content = "✓ Select All";
                DeselectAllBtn.Content = "✗ Deselect All";
                CancelBtn.Content = "❌ Cancel";
                ConfirmBtn.Content = "✓ Confirm Selection";
            }
        }

        private static bool IsSpanishLanguage()
        {
            try
            {
                string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string languagePath = System.IO.Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

                if (!System.IO.File.Exists(languagePath))
                    return false;

                var lines = System.IO.File.ReadAllLines(languagePath);
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

        private static string GetLocalImagePath(string dlcCode)
        {
            try
            {
                if (string.IsNullOrEmpty(dlcCode))
                    return string.Empty;

                string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string cacheDir = System.IO.Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "dlc_images");

                if (!System.IO.Directory.Exists(cacheDir))
                    return string.Empty;

                // Determinar extensión (.jpg o .png)
                string extension = dlcCode == "SP81" ? ".png" : ".jpg";
                string fileName = dlcCode + extension;
                string localPath = System.IO.Path.Combine(cacheDir, fileName);

                // Verificar si existe
                if (System.IO.File.Exists(localPath))
                    return localPath;

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void InitializeDLCList()
        {
            AllDLCItems = new ObservableCollection<DLCItem>
            {
                // Base folders
                new DLCItem { Code = "__Installer", DisplayName = "Installer Files", Category = "Base", IsSelected = false },
                new DLCItem { Code = "Data", DisplayName = "Base Game Data", Category = "Base", IsSelected = false },
                new DLCItem { Code = "Delta", DisplayName = "Delta Updates", Category = "Base", IsSelected = false },
                new DLCItem { Code = "Game", DisplayName = "Game Core Files", Category = "Base", IsSelected = false },
                new DLCItem { Code = "Support", DisplayName = "Support Files", Category = "Base", IsSelected = false },

                // Expansion Packs (EP01 - EP20)
                new DLCItem { Code = "EP01", DisplayName = "Get to Work", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP02", DisplayName = "Get Together", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP03", DisplayName = "City Living", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP04", DisplayName = "Cats & Dogs", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP05", DisplayName = "Seasons", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP06", DisplayName = "Get Famous", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP07", DisplayName = "Island Living", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP08", DisplayName = "Discover University", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP09", DisplayName = "Eco Lifestyle", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP10", DisplayName = "Snowy Escape", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP11", DisplayName = "Cottage Living", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP12", DisplayName = "High School Years", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP13", DisplayName = "Growing Together", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP14", DisplayName = "Horse Ranch", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP15", DisplayName = "For Rent", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP16", DisplayName = "Lovestruck", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP17", DisplayName = "Life & Death", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP18", DisplayName = "Businesses & Hobbies", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP19", DisplayName = "Enchanted by Nature", Category = "Expansion Packs", IsSelected = false },
                new DLCItem { Code = "EP20", DisplayName = "Adventure Awaits", Category = "Expansion Packs", IsSelected = false },

                // Game Packs (GP01 - GP12)
                new DLCItem { Code = "GP01", DisplayName = "Outdoor Retreat", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP02", DisplayName = "Spa Day", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP03", DisplayName = "Dine Out", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP04", DisplayName = "Vampires", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP05", DisplayName = "Parenthood", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP06", DisplayName = "Jungle Adventure", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP07", DisplayName = "StrangerVille", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP08", DisplayName = "Realm of Magic", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP09", DisplayName = "Star Wars: Journey to Batuu", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP10", DisplayName = "Dream Home Decorator", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP11", DisplayName = "My Wedding Stories", Category = "Game Packs", IsSelected = false },
                new DLCItem { Code = "GP12", DisplayName = "Werewolves", Category = "Game Packs", IsSelected = false },

                // Stuff Packs (Numeración oficial saltada)
                new DLCItem { Code = "SP01", DisplayName = "Luxury Party Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP02", DisplayName = "Perfect Patio Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP03", DisplayName = "Cool Kitchen Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP04", DisplayName = "Spooky Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP05", DisplayName = "Movie Hangout Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP06", DisplayName = "Romantic Garden Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP07", DisplayName = "Kids Room Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP08", DisplayName = "Backyard Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP09", DisplayName = "Vintage Glamour Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP10", DisplayName = "Bowling Night Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP11", DisplayName = "Fitness Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP12", DisplayName = "Toddler Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP13", DisplayName = "Laundry Day Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP14", DisplayName = "My First Pet Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP15", DisplayName = "Moschino Stuff Pack", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP16", DisplayName = "Tiny Living", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP17", DisplayName = "Nifty Knitting", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP18", DisplayName = "Paranormal", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP46", DisplayName = "Home Chef Hustle Stuff", Category = "Stuff Packs", IsSelected = false },
                new DLCItem { Code = "SP49", DisplayName = "Crystal Creations Stuff Pack", Category = "Stuff Packs", IsSelected = false },

                // Kits & Creator Kits (SP20 - SP81)
                new DLCItem { Code = "SP20", DisplayName = "Throwback Fit Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP21", DisplayName = "Country Kitchen Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP22", DisplayName = "Bust The Dust Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP23", DisplayName = "Courtyard Oasis Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP24", DisplayName = "Fashion Street-Set", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP25", DisplayName = "Industrial Loft Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP26", DisplayName = "Incheon Arrivals Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP28", DisplayName = "Modern Menswear Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP29", DisplayName = "Blooming Rooms Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP30", DisplayName = "Carnaval Streetwear Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP31", DisplayName = "Decor to the Max Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP32", DisplayName = "Moonlight Chic Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP33", DisplayName = "Little Campers Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP34", DisplayName = "First Fits Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP35", DisplayName = "Desert Luxe Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP36", DisplayName = "Pastel Pop Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP37", DisplayName = "Everyday Clutter Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP38", DisplayName = "Simtimates Collection Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP39", DisplayName = "Bathroom Clutter Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP40", DisplayName = "Greenhouse Haven Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP41", DisplayName = "Basement Treasures Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP42", DisplayName = "Grunge Revival Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP43", DisplayName = "Book Nook Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP44", DisplayName = "Poolside Splash Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP45", DisplayName = "Modern Luxe Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP47", DisplayName = "Castle Estate Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP48", DisplayName = "Goth Galore Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP50", DisplayName = "Urban Homage Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP51", DisplayName = "Party Essentials Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP52", DisplayName = "Riviera Retreat Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP53", DisplayName = "Cozy Bistro Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP54", DisplayName = "Artist Studio Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP55", DisplayName = "Storybook Nursery Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP56", DisplayName = "Sweet Slumber Party Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP57", DisplayName = "Cozy Kitsch Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP58", DisplayName = "Comfy Gamer Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP59", DisplayName = "Secret Sanctuary Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP60", DisplayName = "Casanova Cave Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP61", DisplayName = "Refined Living Room Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP62", DisplayName = "Business Chic Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP63", DisplayName = "Sleek Bathroom Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP64", DisplayName = "Sweet Allure Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP65", DisplayName = "Restoration Workshop Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP66", DisplayName = "Golden Years Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP67", DisplayName = "Kitchen Clutter Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP68", DisplayName = "SpongeBob’s House Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP69", DisplayName = "Autumn Apparel Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP70", DisplayName = "SpongeBob Kid’s Room Kit", Category = "Kits", IsSelected = false },
                new DLCItem { Code = "SP71", DisplayName = "Grange Mudroom Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP72", DisplayName = "Essential Glam Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP73", DisplayName = "Modern Retreat Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP74", DisplayName = "Garden to Table Kit", Category = "Creator Kits", IsSelected = false },
                new DLCItem { Code = "SP81", DisplayName = "Prairie Dreams Set", Category = "Kits", IsSelected = false },

                // Free Packs
                new DLCItem { Code = "FP01", DisplayName = "Holiday Celebration Pack", Category = "Free Packs", IsSelected = false },
            };
        }

        private void BuildCategoriesUI()
        {
            CategoriesPanel.Children.Clear();

            // Base Game Category (Special)
            var baseItems = AllDLCItems.Where(x => x.Category == "Base").ToList();
            if (baseItems.Any())
            {
                CategoriesPanel.Children.Add(CreateCategorySection("Base Game", baseItems, "#22C55E", true));
            }

            // Expansion Packs
            var epItems = AllDLCItems.Where(x => x.Category == "Expansion Packs").ToList();
            if (epItems.Any())
            {
                CategoriesPanel.Children.Add(CreateCategorySection("Expansion Packs", epItems, "#EF4444"));
            }

            // Game Packs
            var gpItems = AllDLCItems.Where(x => x.Category == "Game Packs").ToList();
            if (gpItems.Any())
            {
                CategoriesPanel.Children.Add(CreateCategorySection("Game Packs", gpItems, "#3B82F6"));
            }

            // Stuff Packs
            var spItems = AllDLCItems.Where(x => x.Category == "Stuff Packs").ToList();
            if (spItems.Any())
            {
                CategoriesPanel.Children.Add(CreateCategorySection("Stuff Packs", spItems, "#F59E0B"));
            }

            // Kits
            var fpItems = AllDLCItems.Where(x => x.Category == "Kits").ToList();
            if (fpItems.Any())
            {
                CategoriesPanel.Children.Add(CreateCategorySection("Kits", fpItems, "#8B5CF6"));
            }
        }

        private StackPanel CreateCategorySection(string categoryName, List<DLCItem> items, string accentColor, bool isBaseGame = false)
        {
            var section = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            // Category Header
            var header = new TextBlock
            {
                Text = categoryName,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            section.Children.Add(header);

            // Items WrapPanel
            var wrapPanel = new WrapPanel();

            foreach (var item in items)
            {
                var card = CreateDLCCard(item, accentColor, isBaseGame);
                wrapPanel.Children.Add(card);
            }

            section.Children.Add(wrapPanel);
            return section;
        }

        private Border CreateDLCCard(DLCItem item, string accentColor, bool isBaseGame)
        {
            var card = new Border
            {
                Width = 140,
                Height = isBaseGame ? 80 : 180,
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(8),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = item
            };

            var grid = new Grid();

            // Background Image or Color
            if (isBaseGame)
            {
                // Base game: logo
                var img = new Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(IMG_BASE_URL + "logo.png")),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(10)
                };
                grid.Children.Add(img);
            }
            else
            {
                //  MODIFICADO: DLC thumbnail - usar imagen local si está habilitado
                string imageSource = IMG_BASE_URL + item.Code + ".jpg";

                if (_shouldLoadImages)
                {
                    string localPath = GetLocalImagePath(item.Code);
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        imageSource = localPath;
                    }
                }

                var img = new Image
                {
                    Stretch = Stretch.UniformToFill
                };

                try
                {
                    // Intentar cargar la imagen
                    if (imageSource.StartsWith("http"))
                    {
                        img.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(imageSource));
                    }
                    else
                    {
                        // Imagen local
                        img.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(imageSource, System.UriKind.Absolute));
                    }
                }
                catch
                {
                    // Si falla, usar color de fondo
                    var fallbackBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor))
                    };
                    grid.Children.Add(fallbackBorder);
                }

                grid.Children.Add(img);
            }

            // Overlay cuando NO está seleccionado (oscuro)
            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20)),
                CornerRadius = new CornerRadius(8),
                Visibility = item.IsSelected ? Visibility.Collapsed : Visibility.Visible
            };
            overlay.SetValue(FrameworkElement.NameProperty, "Overlay");
            grid.Children.Add(overlay);

            // Fondo verde semi-transparente cuando SÍ está seleccionado
            var greenOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 34, 197, 94)), // Verde aesthetic
                CornerRadius = new CornerRadius(8),
                Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed
            };
            greenOverlay.SetValue(FrameworkElement.NameProperty, "GreenOverlay");
            grid.Children.Add(greenOverlay);

            // Checkmark GRANDE
            var checkmark = new TextBlock
            {
                Text = "✓",
                Foreground = Brushes.White,
                FontSize = 60,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 10,
                    ShadowDepth = 2,
                    Opacity = 0.8
                }
            };
            checkmark.SetValue(FrameworkElement.NameProperty, "Checkmark");
            grid.Children.Add(checkmark);

            // Title
            var titleBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(5, 3, 5, 3)
            };

            var title = new TextBlock
            {
                Text = isBaseGame ? item.DisplayName : item.DisplayName,
                Foreground = Brushes.White,
                FontSize = isBaseGame ? 9 : 10,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            titleBorder.Child = title;
            grid.Children.Add(titleBorder);

            // Código del DLC (pequeño)
            if (!isBaseGame)
            {
                var codeText = new TextBlock
                {
                    Text = item.Code,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 5, 0),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 5,
                        ShadowDepth = 1,
                        Opacity = 0.8
                    }
                };
                grid.Children.Add(codeText);
            }

            card.Child = grid;

            // Click event
            card.MouseLeftButtonUp += (s, e) =>
            {
                item.IsSelected = !item.IsSelected;
                UpdateCardVisuals(card, item, accentColor);
            };

            return card;
        }

        private void UpdateCardVisuals(Border card, DLCItem item, string accentColor)
        {
            var grid = card.Child as Grid;
            if (grid == null) return;

            foreach (var child in grid.Children)
            {
                if (child is Border overlay && overlay.Name == "Overlay")
                {
                    overlay.Visibility = item.IsSelected ? Visibility.Collapsed : Visibility.Visible;
                }
                else if (child is Border greenOverlay && greenOverlay.Name == "GreenOverlay")
                {
                    greenOverlay.Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (child is TextBlock checkmark && checkmark.Name == "Checkmark")
                {
                    checkmark.Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Mostrar/ocultar placeholder
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            var searchText = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                BuildCategoriesUI();
                return;
            }

            var filtered = AllDLCItems.Where(x =>
                x.DisplayName.ToLower().Contains(searchText) ||
                x.Code.ToLower().Contains(searchText)).ToList();

            CategoriesPanel.Children.Clear();

            if (filtered.Any())
            {
                var section = CreateCategorySection("Resultados de Búsqueda", filtered, "#8B5CF6");
                CategoriesPanel.Children.Add(section);
            }
            else
            {
                var noResults = new TextBlock
                {
                    Text = "No se encontraron resultados",
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                CategoriesPanel.Children.Add(noResults);
            }
        }

        private void MainBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                try
                {
                    this.DragMove();
                }
                catch { }
            }
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in AllDLCItems)
                item.IsSelected = true;
            BuildCategoriesUI();
        }

        private void DeselectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in AllDLCItems)
                item.IsSelected = false;
            BuildCategoriesUI();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            ExcludedFolders = new HashSet<string>();

            foreach (var item in AllDLCItems.Where(x => !x.IsSelected))
            {
                ExcludedFolders.Add(item.Code);
            }

            this.DialogResult = true;
            this.Close();
        }
    }

    public class DLCItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}