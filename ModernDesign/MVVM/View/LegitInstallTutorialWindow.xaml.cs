using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace ModernDesign.MVVM.View
{
    public partial class LegitInstallTutorialWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private string _legitInstallUrl = "https://www.youtube.com/watch?v=YOUR_VIDEO_ID_HERE"; // Fallback por defecto

        public LegitInstallTutorialWindow()
        {
            InitializeComponent();
            ApplyLanguage();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            // Cargar el link dinámicamente
            Loaded += async (s, e) => await LoadLegitInstallLinkAsync();
        }

        private async Task LoadLegitInstallLinkAsync()
        {
            try
            {
                string url = "https://zeroauno.blob.core.windows.net/leuan/TheSims4/Utility/links.txt";
                string content = await _httpClient.GetStringAsync(url);

                // Parsear el contenido
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("legitInstall") && trimmed.Contains("="))
                    {
                        var parts = trimmed.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            _legitInstallUrl = parts[1].Trim();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Si falla, usar el URL por defecto
                System.Diagnostics.Debug.WriteLine($"Error loading legit install link: {ex.Message}");
            }
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = " Guía de Instalación Legítima";
                SubHeaderText.Text = "Sigue estos pasos para instalar The Sims 4 legítimamente";

                Step1Title.Text = "📥 Paso 1: Descarga el Juego Base";
                Step1Description.Text = "Descarga el juego base de The Sims 4 desde una de estas plataformas oficiales:\n\n" +
                                       "• Steam: Busca 'The Sims 4' en la tienda de Steam\n" +
                                       "• EA App: Descarga desde la EA App oficial\n\n" +
                                       "⚠️ Asegúrate de descargar e instalar el juego base completo antes de continuar.";

                Step2Title.Text = "🎁 Paso 2: Descarga DLCs con Este Toolkit";
                Step2Description.Text = "Una vez que tengas el juego base instalado:\n\n" +
                                       "1. Usa este toolkit para descargar todos los DLCs gratis\n" +
                                       "2. El toolkit manejará la descarga e instalación\n" +
                                       "3. Todos los DLCs se agregarán a tu juego automáticamente";

                Step3Title.Text = "🔓 Paso 3: Activa los DLCs con DLC Unlocker";
                Step3Description.Text = "Después de descargar los DLCs:\n\n" +
                                       "• Usa la función DLC Unlocker integrada en este toolkit\n" +
                                       "• Activará automáticamente todos los DLCs descargados\n" +
                                       "• ¡Inicia el juego y disfruta todo el contenido!";

                VideoTitle.Text = "📺 Ver Tutorial en Video";
                VideoDescription.Text = "Para una guía detallada paso a paso, mira nuestro video tutorial:";
                VideoBtn.Content = "🎬 Abrir Tutorial de YouTube";

                CloseBtn.Content = " ¡Entendido!";
            }
            else
            {
                HeaderText.Text = " Legit Installation Guide";
                SubHeaderText.Text = "Follow these steps to install The Sims 4 legitimately";

                Step1Title.Text = "📥 Step 1: Download the Base Game";
                Step1Description.Text = "Download The Sims 4 base game from one of these official platforms:\n\n" +
                                       "• Steam: Search for 'The Sims 4' in the Steam store\n" +
                                       "• EA App: Download from the official EA App\n\n" +
                                       "⚠️ Make sure to download and install the complete base game before continuing.";

                Step2Title.Text = "🎁 Step 2: Download DLCs with This Toolkit";
                Step2Description.Text = "Once you have the base game installed:\n\n" +
                                       "1. Use this toolkit to download all DLCs for free\n" +
                                       "2. The toolkit will handle the download and installation\n" +
                                       "3. All DLCs will be added to your game automatically";

                Step3Title.Text = "🔓 Step 3: Activate DLCs with DLC Unlocker";
                Step3Description.Text = "After downloading the DLCs:\n\n" +
                                       "• Use the built-in DLC Unlocker feature in this toolkit\n" +
                                       "• It will automatically activate all downloaded DLCs\n" +
                                       "• Launch the game and enjoy all content!";

                VideoTitle.Text = "📺 Watch Video Tutorial";
                VideoDescription.Text = "For a detailed step-by-step guide, watch our video tutorial:";
                VideoBtn.Content = "🎬 Open YouTube Tutorial";

                CloseBtn.Content = " Got It!";
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

        private void VideoBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = _legitInstallUrl, //  USA EL LINK DINÁMICO
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open YouTube: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}