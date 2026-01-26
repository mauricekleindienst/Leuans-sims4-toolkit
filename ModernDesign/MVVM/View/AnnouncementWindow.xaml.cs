using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModernDesign.MVVM.View
{
    public partial class AnnouncementWindow : Window
    {
        private static bool _hasBeenShownThisSession = false;
        private readonly string _appDataPath;
        private readonly string _profileIniPath;
        private readonly string _scripturesPath;
        private readonly string _phrasesFilePath;
        private readonly string _scripturesFilePath;
        private bool _isSpanish;

        public AnnouncementWindow(string announcementText, string imageUrl = null, string logoUrl = null)
        {
            InitializeComponent();

            // Configurar rutas
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Leuan's - Sims 4 ToolKit");
            _profileIniPath = Path.Combine(_appDataPath, "Profile.ini");
            _scripturesPath = Path.Combine(_appDataPath, "qol", "scriptures");
            _phrasesFilePath = Path.Combine(_scripturesPath, "Phrases.txt");
            _scripturesFilePath = Path.Combine(_scripturesPath, "TheScriptures.txt");

            // Crear directorios si no existen
            Directory.CreateDirectory(_scripturesPath);

            this.Loaded += AnnouncementWindow_Loaded;

            // Establecer el texto del anuncio
            AnnouncementTextBlock.Text = announcementText;

            // Cargar imagen si existe
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    AnnouncementImage.Source = new BitmapImage(new Uri(imageUrl));
                    ImageBorder.Visibility = Visibility.Visible;
                }
                catch
                {
                    ImageBorder.Visibility = Visibility.Collapsed;
                }
            }

            // Cargar logo si existe
            if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                try
                {
                    LogoImage.Source = new BitmapImage(new Uri(logoUrl));
                    LogoBorder.Visibility = Visibility.Visible;
                }
                catch
                {
                    LogoBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void AnnouncementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLanguage();
            LoadSettings();
            await ApplyPhilosophicalOrScriptureMode();
        }

        private void ApplyLanguage()
        {
            string languageCode = GetLanguageCode();
            _isSpanish = languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            if (_isSpanish)
            {
                // Vista de anuncio
                HeaderText.Text = "📢 Anuncio";
                CloseButton.Content = "Cerrar";

                // Vista de configuración
                SettingsHeaderText.Text = "⚙️ Configuración";
                SaveSettingsButton.Content = "Guardar";
                ShowAnnouncementsText.Text = "Mostrar nuevos anuncios al iniciar el toolkit";
                ShowAnnouncementsDesc.Text = "Mostrar anuncios cuando se inicia el toolkit";
                CustomBackgroundText.Text = "Personalizar Fondo del Anuncio";
                CustomBackgroundDesc.Text = "Cambiar el color de fondo de los anuncios";
                PhilosophicalModeText.Text = "Modo Filosófico";
                PhilosophicalModeDesc.Text = "Mostrar frases motivacionales con los anuncios";
                ScriptureModeText.Text = "Modo Escrituras";
                ScriptureModeDesc.Text = "Mostrar versículos bíblicos con los anuncios";
                EverydayScriptureText.Text = "Escritura Diaria";
                EverydayScriptureDesc.Text = "Mostrar siempre un versículo diario, incluso si los anuncios están desactivados";
            }
            else
            {
                HeaderText.Text = "📢 Announcement";
                CloseButton.Content = "Close";
                SettingsHeaderText.Text = "⚙️ Settings";
                SaveSettingsButton.Content = "Save";
                ShowAnnouncementsText.Text = "Show new announcements on startup";
                ShowAnnouncementsDesc.Text = "Display announcements when toolkit starts";
                CustomBackgroundText.Text = "Customize Announcement Background";
                CustomBackgroundDesc.Text = "Change the background color of announcements";
                PhilosophicalModeText.Text = "Philosophical Mode";
                PhilosophicalModeDesc.Text = "Show motivational phrases with announcements";
                ScriptureModeText.Text = "Scripture Mode";
                ScriptureModeDesc.Text = "Show Bible verses with announcements";
                EverydayScriptureText.Text = "Everyday Scripture";
                EverydayScriptureDesc.Text = "Always show a daily verse, even if announcements are disabled";
            }
        }

        private string GetLanguageCode()
        {
            string languageIniPath = Path.Combine(_appDataPath, "language.ini");
            string languageCode = "en-US";

            try
            {
                if (File.Exists(languageIniPath))
                {
                    string[] lines = File.ReadAllLines(languageIniPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Language = ", StringComparison.OrdinalIgnoreCase))
                        {
                            languageCode = line.Substring("Language = ".Length).Trim();
                            break;
                        }
                    }
                }
            }
            catch { }

            return languageCode;
        }

        public static bool ShouldShowAnnouncement()
        {
            return !_hasBeenShownThisSession;
        }

        public static void MarkAsShown()
        {
            _hasBeenShownThisSession = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MarkAsShown();
            SaveSettings(); // Guardar configuración al cerrar
            this.Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AnnouncementView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsView.Visibility = Visibility.Collapsed;
            AnnouncementView.Visibility = Visibility.Visible;
        }

        private void ShowAnnouncementsToggle_Click(object sender, RoutedEventArgs e)
        {
            // Siempre mantenerlo en true
            ShowAnnouncementsToggle.IsChecked = true;

            // Mostrar popup
            string message = _isSpanish
                ? "Solo los Supporters pueden desactivar completamente los anuncios"
                : "Only Supporters can disable totally the announcements";

            MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_profileIniPath))
                {
                    var lines = File.ReadAllLines(_profileIniPath);
                    bool inAnnouncementSection = false;

                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();

                        if (trimmed == "[Announcement]")
                        {
                            inAnnouncementSection = true;
                            continue;
                        }

                        if (trimmed.StartsWith("[") && trimmed != "[Announcement]")
                        {
                            inAnnouncementSection = false;
                        }

                        if (inAnnouncementSection && trimmed.Contains("="))
                        {
                            var parts = trimmed.Split(new[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                string value = parts[1].Trim();

                                switch (key)
                                {
                                    case "CustomBackground":
                                        bool customBg = bool.Parse(value);
                                        CustomBackgroundToggle.IsChecked = customBg;
                                        ColorPickerPanel.Visibility = customBg ? Visibility.Visible : Visibility.Collapsed;
                                        break;

                                    case "BackgroundColor":
                                        ApplyBackgroundColor(value);
                                        break;

                                    case "PhilosophicalMode":
                                        PhilosophicalModeToggle.IsChecked = bool.Parse(value);
                                        break;

                                    case "ScriptureMode":
                                        ScriptureModeToggle.IsChecked = bool.Parse(value);
                                        break;

                                    case "EverydayScripture":
                                        EverydayScriptureToggle.IsChecked = bool.Parse(value);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            string message = _isSpanish ? "Configuración guardada" : "Settings saved";
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            BackButton_Click(null, null);
        }

        private void SaveSettings()
        {
            try
            {
                var lines = File.Exists(_profileIniPath)
                    ? File.ReadAllLines(_profileIniPath).ToList()
                    : new System.Collections.Generic.List<string>();

                // Buscar si existe la sección [Announcement]
                int announcementIndex = lines.FindIndex(l => l.Trim() == "[Announcement]");

                if (announcementIndex == -1)
                {
                    // Agregar sección al final
                    lines.Add("");
                    lines.Add("[Announcement]");
                    announcementIndex = lines.Count - 1;
                }

                // Encontrar el final de la sección [Announcement]
                int nextSectionIndex = lines.FindIndex(announcementIndex + 1, l => l.Trim().StartsWith("["));

                // Eliminar configuraciones antiguas de announcement
                if (nextSectionIndex == -1)
                {
                    lines.RemoveRange(announcementIndex + 1, lines.Count - announcementIndex - 1);
                }
                else
                {
                    lines.RemoveRange(announcementIndex + 1, nextSectionIndex - announcementIndex - 1);
                }

                // Crear el color hex actual
                byte r = (byte)RedSlider.Value;
                byte g = (byte)GreenSlider.Value;
                byte b = (byte)BlueSlider.Value;
                string hexColor = $"#{r:X2}{g:X2}{b:X2}";

                // Agregar nuevas configuraciones
                int insertIndex = announcementIndex + 1;
                lines.Insert(insertIndex++, $"CustomBackground = {CustomBackgroundToggle.IsChecked.ToString().ToLower()}");
                lines.Insert(insertIndex++, $"BackgroundColor = {hexColor}");
                lines.Insert(insertIndex++, $"PhilosophicalMode = {PhilosophicalModeToggle.IsChecked.ToString().ToLower()}");
                lines.Insert(insertIndex++, $"ScriptureMode = {ScriptureModeToggle.IsChecked.ToString().ToLower()}");
                lines.Insert(insertIndex++, $"EverydayScripture = {EverydayScriptureToggle.IsChecked.ToString().ToLower()}");

                File.WriteAllLines(_profileIniPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RedSlider == null || GreenSlider == null || BlueSlider == null)
                return;

            // Actualizar los valores numéricos
            RedValue.Text = ((int)RedSlider.Value).ToString();
            GreenValue.Text = ((int)GreenSlider.Value).ToString();
            BlueValue.Text = ((int)BlueSlider.Value).ToString();

            // Crear el color
            byte r = (byte)RedSlider.Value;
            byte g = (byte)GreenSlider.Value;
            byte b = (byte)BlueSlider.Value;

            Color color = Color.FromRgb(r, g, b);
            string hexColor = $"#{r:X2}{g:X2}{b:X2}";

            // Actualizar preview
            ColorPreview.Background = new SolidColorBrush(color);
            ColorHexText.Text = hexColor;

            // Aplicar al fondo si está activado
            if (CustomBackgroundToggle.IsChecked == true)
            {
                MainBorder.Background = new SolidColorBrush(color);
            }
        }

        private void CustomBackgroundToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (CustomBackgroundToggle.IsChecked == true)
            {
                ColorPickerPanel.Visibility = Visibility.Visible;

                // Aplicar el color actual
                byte r = (byte)RedSlider.Value;
                byte g = (byte)GreenSlider.Value;
                byte b = (byte)BlueSlider.Value;
                Color color = Color.FromRgb(r, g, b);
                MainBorder.Background = new SolidColorBrush(color);
            }
            else
            {
                ColorPickerPanel.Visibility = Visibility.Collapsed;
                ApplyBackgroundColor("#0F172A"); // Color por defecto
            }
        }

        private void ApplyBackgroundColor(string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                MainBorder.Background = new SolidColorBrush(color);

                // Actualizar sliders si existen
                if (RedSlider != null && GreenSlider != null && BlueSlider != null)
                {
                    RedSlider.Value = color.R;
                    GreenSlider.Value = color.G;
                    BlueSlider.Value = color.B;
                }
            }
            catch
            {
                // Color inválido, ignorar
            }
        }

        private void PhilosophicalModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            // Si se activa Philosophical Mode, desactivar Scripture Mode
            if (PhilosophicalModeToggle.IsChecked == true)
            {
                ScriptureModeToggle.IsChecked = false;
            }
        }

        private void ScriptureModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            // Si se activa Scripture Mode, desactivar Philosophical Mode
            if (ScriptureModeToggle.IsChecked == true)
            {
                PhilosophicalModeToggle.IsChecked = false;
            }
        }

        private void EverydayScriptureToggle_Changed(object sender, RoutedEventArgs e)
        {
            // Lógica manejada externamente
        }

        private async Task ApplyPhilosophicalOrScriptureMode()
        {
            bool philosophicalMode = PhilosophicalModeToggle.IsChecked == true;
            bool scriptureMode = ScriptureModeToggle.IsChecked == true;

            if (philosophicalMode)
            {
                string phrase = await GetRandomPhrase();
                if (!string.IsNullOrEmpty(phrase))
                {
                    PhilosophicalOrScriptureText.Text = phrase;
                    PhilosophicalOrScriptureText.Visibility = Visibility.Visible;
                    SeparatorText.Visibility = Visibility.Visible;
                }
            }
            else if (scriptureMode)
            {
                string verse = await GetRandomScripture();
                if (!string.IsNullOrEmpty(verse))
                {
                    PhilosophicalOrScriptureText.Text = verse;
                    PhilosophicalOrScriptureText.Visibility = Visibility.Visible;
                    SeparatorText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Ocultar si ninguno está activo
                PhilosophicalOrScriptureText.Visibility = Visibility.Collapsed;
                SeparatorText.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<string> GetRandomPhrase()
        {
            await EnsureFileExists(_phrasesFilePath, "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/Phrases.txt");
            return GetRandomLineFromFile(_phrasesFilePath);
        }

        private async Task<string> GetRandomScripture()
        {
            await EnsureFileExists(_scripturesFilePath, "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/TheScriptures.txt");
            return GetRandomLineFromFile(_scripturesFilePath);
        }

        private async Task EnsureFileExists(string filePath, string url)
        {
            if (!File.Exists(filePath))
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var content = await client.GetStringAsync(url);
                        File.WriteAllText(filePath, content);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error downloading file: {ex.Message}");
                }
            }
        }

        private string GetRandomLineFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath)
                        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("#"))
                        .ToArray();

                    if (lines.Length > 0)
                    {
                        var random = new Random();
                        return lines[random.Next(lines.Length)].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading file: {ex.Message}");
            }

            return string.Empty;
        }
    }
}