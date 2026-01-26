using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class SocialView : UserControl
    {
        private string _languageCode = "en-US";

        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION - Your links here
        // ═══════════════════════════════════════════════════════════════
        private const string WEBSITE_URL = "https://leuan.zeroauno.com/sims4-toolkit/index.html";
        private const string DISCORD_SERVER_URL = "https://discord.gg/JYnpPt4nUu";
        private const string PERSONAL_DISCORD = "leuan";

        // ═══════════════════════════════════════════════════════════════
        // EVENT - Para navegar a Staff Members
        // ═══════════════════════════════════════════════════════════════
        public event EventHandler NavigateToStaffRequested;

        public SocialView()
        {
            InitializeComponent();
            InitLocalization();
        }

        #region Localization

        private void InitLocalization()
        {
            LoadLanguageFromIni();
            bool isSpanish = IsSpanish();

            // Header
            TitleText.Text = isSpanish ? "Conéctate con Nosotros" : "Connect with Us";
            SubtitleText.Text = isSpanish
                ? "Únete a nuestra comunidad y mantente actualizado con las últimas noticias"
                : "Join our community and stay updated with the latest news";

            // Social Cards
            WebsiteTitle.Text = isSpanish ? "Sitio Web" : "Website";
            DiscordServerTitle.Text = isSpanish ? "Servidor de Discord" : "Discord Server";
            DiscordServerDesc.Text = isSpanish ? "Únete a la comunidad" : "Join the community";
            PersonalDiscordTitle.Text = isSpanish ? "Discord Personal" : "Personal Discord";
            PersonalDiscordDesc.Text = PERSONAL_DISCORD;

            // Staff Button
            StaffButtonTitle.Text = isSpanish ? "Miembros del Staff" : "Staff Members";
            StaffButtonDesc.Text = isSpanish ? "Conoce a nuestro increíble equipo" : "Meet our amazing team";

            // Thanks Section
            ThanksTitle.Text = isSpanish ? "Agradecimientos Especiales" : "Special Thanks";

            AnadiusThanks.Text = isSpanish
                ? "Por todo el increíble trabajo en los desbloqueadores de DLC. Descansa en paz. 🕊️"
                : "For all the incredible work on DLC unlockers. Rest in peace. 🕊️";

            CommunityThanks.Text = isSpanish
                ? "Por mantener viva y próspera la escena del modding."
                : "For keeping the modding scene alive and thriving.";

            CreatorsThanks.Text = isSpanish
                ? "Tu creatividad hace Los Sims 4 infinitamente mejor."
                : "Your creativity makes The Sims 4 infinitely better.";

            TestersThanks.Text = isSpanish
                ? "Por encontrar bugs y sugerir mejoras."
                : "For finding bugs and suggesting improvements.";

            DiscordThanks.Text = isSpanish
                ? "Por ser una comunidad increíble y solidaria."
                : "For being an amazing and supportive community.";

            YouTitle.Text = isSpanish ? "¡Tú! 💜" : "You! 💜";
            YouThanks.Text = isSpanish
                ? "Por usar esta herramienta y apoyar el proyecto."
                : "For using this tool and supporting the project.";

            FooterText.Text = isSpanish ? "Hecho con 💜 por Leuan" : "Made with 💜 by Leuan";
        }

        private void LoadLanguageFromIni()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string iniPath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

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

        private bool IsSpanish() => _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

        #endregion

        #region Click Handlers

        private void WebsiteCard_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl(WEBSITE_URL);
        }

        private void DiscordServerCard_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl(DISCORD_SERVER_URL);
        }

        private void PersonalDiscordCard_Click(object sender, MouseButtonEventArgs e)
        {
            CopyDiscordToClipboard();
        }

        private void StaffMembersCard_Click(object sender, MouseButtonEventArgs e)
        {
            var staffWindow = new Window
            {
                Title = "Staff Members",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")),
                Content = new StaffView()
            };

            staffWindow.ShowDialog(); // Modal
                                      // o
                                      // staffWindow.Show(); // No modal
        }

        #endregion

        #region Helper Methods

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
                bool isSpanish = IsSpanish();
                MessageBox.Show(
                    isSpanish
                        ? $"No se pudo abrir el enlace:\n{url}\n\nError: {ex.Message}"
                        : $"Could not open link:\n{url}\n\nError: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyDiscordToClipboard()
        {
            bool isSpanish = IsSpanish();

            try
            {
                Clipboard.SetText(PERSONAL_DISCORD);

                MessageBox.Show(
                    isSpanish
                        ? $"'{PERSONAL_DISCORD}' copiado al portapapeles.\n\nPégalo en Discord para agregarme como amigo."
                        : $"'{PERSONAL_DISCORD}' copied to clipboard.\n\nPaste it in Discord to add me as a friend.",
                    isSpanish ? "✓ Copiado" : "✓ Copied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    isSpanish
                        ? $"❌ No se pudo copiar al portapapeles.\n\nMi Discord es: {PERSONAL_DISCORD}\n\nError: {ex.Message}"
                        : $"❌ Could not copy to clipboard.\n\nMy Discord is: {PERSONAL_DISCORD}\n\nError: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}