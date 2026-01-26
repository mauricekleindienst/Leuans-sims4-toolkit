using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class ExclusionManagerWindow : Window
    {
        public HashSet<string> Exclusions { get; private set; }
        private bool _isSpanish;

        public ExclusionManagerWindow(HashSet<string> exclusions, bool isSpanish)
        {
            InitializeComponent();
            Exclusions = new HashSet<string>(exclusions);
            _isSpanish = isSpanish;
            ApplyLanguage();
            DisplayExclusions();
        }

        private void ApplyLanguage()
        {
            if (_isSpanish)
            {
                TitleText.Text = "⚙️ Gestionar Exclusiones";
                SubtitleText.Text = "Archivos que nunca serán renombrados";
                SaveBtn.Content = "Guardar";
                CancelBtn.Content = "Cancelar";
            }
        }

        private void DisplayExclusions()
        {
            ExclusionListPanel.Children.Clear();

            if (Exclusions.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = _isSpanish ? "No hay exclusiones" : "No exclusions",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                    FontSize = 13,
                    FontFamily = new FontFamily("Bahnschrift Light"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                ExclusionListPanel.Children.Add(emptyText);
                return;
            }

            foreach (var exclusion in Exclusions.OrderBy(x => x))
            {
                var card = CreateExclusionCard(exclusion);
                ExclusionListPanel.Children.Add(card);
            }
        }

        private Border CreateExclusionCard(string fileName)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = fileName,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12,
                FontFamily = new FontFamily("Bahnschrift Light"),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            grid.Children.Add(nameText);
            Grid.SetColumn(nameText, 0);

            var removeBtn = new Button
            {
                Content = "✕",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Width = 30,
                Height = 30,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;
            removeBtn.Template = template;

            removeBtn.Click += (s, e) => RemoveExclusion(fileName);

            grid.Children.Add(removeBtn);
            Grid.SetColumn(removeBtn, 1);

            border.Child = grid;
            return border;
        }

        private void RemoveExclusion(string fileName)
        {
            Exclusions.Remove(fileName);
            DisplayExclusions();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
