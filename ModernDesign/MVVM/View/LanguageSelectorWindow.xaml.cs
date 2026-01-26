using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace ModernDesign.MVVM.View
{
    public partial class LanguageSelectorWindow : Window
    {
        private readonly string _languageSelectorPath;
        private readonly string _languageExePath;
        private readonly HttpClient _httpClient = new HttpClient();

        public LanguageSelectorWindow()
        {
            InitializeComponent();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _languageSelectorPath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language-selector");
            _languageExePath = Path.Combine(_languageSelectorPath, "Language.exe");

            ApplyLanguage();
            this.MouseLeftButtonDown += (s, e) =>
            {
                try { this.DragMove(); } catch { }
            };

            Loaded += LanguageSelectorWindow_Loaded;
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = "🌍 Cambiador de Idioma";
                SubHeaderText.Text = "Cambia el idioma de tu juego fácilmente";
                DescriptionText.Text = "Usa la herramienta Language Changer para cambiar el idioma de tu juego.\n\n" +
                                      "Haz clic en el botón de abajo para descargar y ejecutar la herramienta.";
                CloseBtn.Content = "❌ Cerrar";
            }
            else
            {
                HeaderText.Text = "🌍 Language Changer";
                SubHeaderText.Text = "Change your game language easily";
                DescriptionText.Text = "Use the Language Changer tool to switch your game language.\n\n" +
                                      "Click the button below to download and run the tool.";
                CloseBtn.Content = "❌ Close";
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

        private void LanguageSelectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Verificar si Language.exe existe
            CheckLanguageExeExists();
        }

        private void CheckLanguageExeExists()
        {
            bool exists = File.Exists(_languageExePath);

            if (!exists)
            {
                // Mostrar botón de descarga
                DownloadBtn.Visibility = Visibility.Visible;

                bool isSpanish = IsSpanishLanguage();
                StatusText.Text = isSpanish
                    ? "⚠️ Language Changer no detectado. Por favor descárgalo primero."
                    : "⚠️ Language Changer not detected. Please download it first.";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B"));
            }
            else
            {
                // Ocultar botón de descarga
                DownloadBtn.Visibility = Visibility.Collapsed;
                StatusText.Text = "";
            }
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            await DownloadLanguageExe();
        }

        private async void ChangeLanguageBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            // Verificar si Language.exe existe
            if (!File.Exists(_languageExePath))
            {
                // Si no existe, descargar primero
                var result = MessageBox.Show(
                    isSpanish
                        ? "Language Changer no está instalado.\n\n¿Deseas descargarlo ahora?"
                        : "Language Changer is not installed.\n\nDo you want to download it now?",
                    isSpanish ? "Descarga Requerida" : "Download Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadLanguageExe();
                }
                return;
            }

            // Ejecutar Language.exe
            try
            {
                StatusText.Text = isSpanish
                    ? "🚀 Abriendo Language Changer..."
                    : "🚀 Opening Language Changer...";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3EC7E8"));

                Process.Start(new ProcessStartInfo
                {
                    FileName = _languageExePath,
                    WorkingDirectory = _languageSelectorPath,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    isSpanish
                        ? $"Error al ejecutar Language Changer:\n\n{ex.Message}"
                        : $"Error running Language Changer:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DownloadLanguageExe()
        {
            bool isSpanish = IsSpanishLanguage();

            try
            {
                // Crear directorio si no existe
                if (!Directory.Exists(_languageSelectorPath))
                {
                    Directory.CreateDirectory(_languageSelectorPath);
                }

                StatusText.Text = isSpanish ? "📥 Descargando Language.exe..." : "📥 Downloading Language.exe...";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3EC7E8"));

                DownloadBtn.IsEnabled = false;
                ChangeLanguageBtn.IsEnabled = false;

                // URL del Language.exe
                string downloadUrl = "https://github.com/Johnn-sin/leuansin-dlcs/releases/download/Misc/Language.exe";

                // Descargar
                using (var response = await _httpClient.GetAsync(downloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fileStream = new FileStream(_languageExePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                StatusText.Text = isSpanish
                    ? " Language.exe descargado exitosamente"
                    : " Language.exe downloaded successfully";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E"));

                // Ocultar botón de descarga
                DownloadBtn.Visibility = Visibility.Collapsed;

                // Ejecutar Language.exe automáticamente
                if (File.Exists(_languageExePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _languageExePath,
                        WorkingDirectory = _languageSelectorPath,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = isSpanish ? $"❌ Error: {ex.Message}" : $"❌ Error: {ex.Message}";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));

                MessageBox.Show(
                    isSpanish
                        ? $"Error al descargar Language.exe:\n\n{ex.Message}"
                        : $"Error downloading Language.exe:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                DownloadBtn.IsEnabled = true;
                ChangeLanguageBtn.IsEnabled = true;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}