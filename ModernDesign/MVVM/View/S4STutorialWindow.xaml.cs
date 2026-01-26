using ModernDesign.Localization;
using ModernDesign.Managers;
using ModernDesign.Profile;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ModernDesign.MVVM.View
{
    public partial class S4STutorialWindow : Window
    {
        private string _category;
        private int _currentStep = 0;
        private List<TutorialStep> _steps;
        private string _tutorialId;
        private bool _isShowingMedal = false;

        public S4STutorialWindow(string category)
        {
            InitializeComponent();
            _category = category;
            _tutorialId = $"tutorial_{category}";

            // Mostrar loading y cargar async
            LoadStepsAsync();

            ApplyLanguage();
        }

        public class TutorialStep
        {
            public string Title { get; set; }
            public string Desc { get; set; }
            public string Image { get; set; }
            public string Label { get; set; }
            public string Color { get; set; }
            public string Tips { get; set; }
        }

        private async void LoadStepsAsync()
        {
            bool es = LanguageManager.IsSpanish;
            string lang = es ? "es" : "en";

            // Mostrar loading
            StepTitle.Text = es ? "Cargando tutorial..." : "Loading tutorial...";
            StepDescription.Text = es ? "Por favor espera..." : "Please wait...";

            // Load tutorial from JSON (async)
            var tutorialData = await TutorialManager.LoadTutorialAsync(_category);

            // Convert to TutorialStep list
            _steps = new List<TutorialStep>();
            foreach (var step in tutorialData.Steps)
            {
                _steps.Add(new TutorialStep
                {
                    Title = step.Title.ContainsKey(lang) ? step.Title[lang] : step.Title["en"],
                    Desc = step.Description.ContainsKey(lang) ? step.Description[lang] : step.Description["en"],
                    Image = step.Image,
                    Label = step.Label != null && step.Label.ContainsKey(lang) ? step.Label[lang] : "",
                    Color = step.Color,
                    Tips = step.Tips != null && step.Tips.ContainsKey(lang) ? step.Tips[lang] : null
                });
            }

            // Actualizar UI cuando termine de cargar
            UpdateUI();
        }

        private void ApplyLanguage()
        {
            bool es = LanguageManager.IsSpanish;

            string categoryName = GetCategoryDisplayName(_category, es);
            SubtitleText.Text = es ? $"Aprende a crear: {categoryName}" : $"Learn to create: {categoryName}";

            PrevButton.Content = es ? "← Atrás" : "← Back";
            NextButton.Content = es ? "Siguiente →" : "Next →";
            CloseButton.Content = es ? "Cerrar" : "Close";
            TipsTitle.Text = es ? "💡 Consejos" : "💡 Tips";
        }

        private string GetCategoryDisplayName(string cat, bool es)
        {
            switch (cat)
            {
                case "trait": return es ? "Rasgo" : "Trait";
                case "interaction": return es ? "Interacción" : "Interaction";
                case "career": return es ? "Carrera" : "Career";
                case "object": return es ? "Objeto" : "Object";
                case "clothing": return es ? "Ropa" : "Clothing";
                case "buff": return "Buff";
                default: return cat;
            }
        }

        private void UpdateUI()
        {
            if (_steps == null || _steps.Count == 0) return;

            bool es = LanguageManager.IsSpanish;
            var step = _steps[_currentStep];

            StepIndicator.Text = es
                ? $"Paso {_currentStep + 1} de {_steps.Count}"
                : $"Step {_currentStep + 1} of {_steps.Count}";

            StepTitle.Text = step.Title;
            StepDescription.Text = step.Desc;

            // Tips
            if (!string.IsNullOrEmpty(step.Tips))
            {
                TipsBox.Visibility = Visibility.Visible;
                TipsText.Text = step.Tips;
            }
            else
            {
                TipsBox.Visibility = Visibility.Collapsed;
            }

            // Image
            try
            {
                StepImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(step.Image, UriKind.Relative));
                PlaceholderText.Visibility = Visibility.Collapsed;
            }
            catch
            {
                PlaceholderText.Visibility = Visibility.Visible;
            }

            // Navigation buttons
            PrevButton.Visibility = _currentStep > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Si es el último paso, mostrar "Finalizar" en lugar de "Siguiente"
            if (_currentStep == _steps.Count - 1)
            {
                NextButton.Content = es ? "Finalizar Tutorial" : "Finish Tutorial";
                NextButton.Visibility = Visibility.Visible;
            }
            else
            {
                NextButton.Content = es ? "Siguiente →" : "Next →";
                NextButton.Visibility = Visibility.Visible;
            }

            // Progress dots
            UpdateProgressDots();
        }

        private void UpdateProgressDots()
        {
            ProgressDots.Children.Clear();

            for (int i = 0; i < _steps.Count; i++)
            {
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Margin = new Thickness(0, 0, 6, 0),
                    Fill = i == _currentStep
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
                };

                if (i == _currentStep)
                {
                    dot.Effect = new DropShadowEffect
                    {
                        Color = (Color)ColorConverter.ConvertFromString("#22C55E"),
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };
                }

                ProgressDots.Children.Add(dot);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 0)
            {
                _currentStep--;
                UpdateUI();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < _steps.Count - 1)
            {
                _currentStep++;

                // Asignar medallas automáticamente según el progreso
                int totalSteps = _steps.Count;
                int bronzeStep = 0; // Primera lección
                int silverStep = totalSteps / 2; // Mitad del tutorial

                if (_currentStep == bronzeStep)
                {
                    ProfileManager.SetTutorialMedal(_tutorialId, MedalType.Bronze);
                    ShowMedalNotification(MedalType.Bronze);
                }
                else if (_currentStep == silverStep)
                {
                    ProfileManager.SetTutorialMedal(_tutorialId, MedalType.Silver);
                    ShowMedalNotification(MedalType.Silver);
                }

                UpdateUI();
            }
            else
            {
                // Prevenir spam - verificar si ya está mostrando medalla
                if (_isShowingMedal) return;

                // Tutorial completado - otorgar medalla de oro
                ProfileManager.SetTutorialMedal(_tutorialId, MedalType.Gold);
                ShowMedalNotification(MedalType.Gold);
            }
        }

        private void ShowMedalNotification(MedalType medal)
        {
            // Prevenir spam de medallas
            if (_isShowingMedal) return;
            _isShowingMedal = true;

            // Deshabilitar todos los botones
            NextButton.IsEnabled = false;
            PrevButton.IsEnabled = false;
            CloseButton.IsEnabled = false;

            // Si es medalla de oro, cerrar inmediatamente la ventana
            if (medal == MedalType.Gold)
            {
                // Mostrar popup de medalla
                var medalPopup = new MedalPopupView(medal);
                medalPopup.Show();

                // Cerrar INMEDIATAMENTE esta ventana sin esperar
                this.Close();
            }
            else
            {
                // Para bronce y plata, esperar a que se cierre el popup
                var medalPopup = new MedalPopupView(medal);
                medalPopup.Closed += (s, e) =>
                {
                    // Re-habilitar botones cuando se cierre el popup
                    NextButton.IsEnabled = true;
                    PrevButton.IsEnabled = true;
                    CloseButton.IsEnabled = true;
                    _isShowingMedal = false;
                };
                medalPopup.Show();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
