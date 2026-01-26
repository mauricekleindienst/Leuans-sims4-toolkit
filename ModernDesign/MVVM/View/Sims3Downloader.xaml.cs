using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ModernDesign.Managers;
using ModernDesign.MVVM.View;

namespace ModernDesign
{
    public partial class Sims3Downloader : Window
    {
        public Sims3Downloader()
        {
            InitializeComponent();
            ApplyLanguage();
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                TitleText.Text = "Elige Tu Método";
                SubtitleText.Text = "Selecciona cómo deseas instalar los DLC's de Sims 3";

                // Automatic
                AutomaticTitle.Text = "Automático";
                AutomaticDesc.Text = "Déjanos encargarnos de todo por ti";

                // Manual
                ManualTitle.Text = "Manual";
                ManualDesc.Text = "Control completo sobre cada detalle";
            }
            else
            {
                TitleText.Text = "Choose Your Method";
                SubtitleText.Text = "Select how you want to install Sims 3 DLC's";

                // Automatic
                AutomaticTitle.Text = "Automatic";
                AutomaticDesc.Text = "Let us handle everything for you";

                // Manual
                ManualTitle.Text = "Manual";
                ManualDesc.Text = "Complete control over every detail";
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

        private void AutomaticBtn_Click(object sender, MouseButtonEventArgs e)
        {
            // Verificar si tiene Developer Mode activado
            bool hasDeveloperMode = DeveloperModeManager.IsDeveloperModeUnlocked();

            if (!hasDeveloperMode)
            {
                // Mostrar popup indicando que es solo para Developer Mode
                ShowDeveloperModeRequiredPopup();
                return;
            }

            // Si tiene Developer Mode, abrir Sims3Updater
            OpenSims3Updater();
        }

        private void ShowDeveloperModeRequiredPopup()
        {
            bool isSpanish = IsSpanishLanguage();

            string title = isSpanish ? "🔒 Modo Desarrollador Requerido" : "🔒 Developer Mode Required";

            string message = isSpanish
                ? "El modo automático está reservado exclusivamente para usuarios con Developer Mode activado.\n\n" +
                  "Para desbloquear esta función, necesitas:\n" +
                  "✅ Obtener todas las medallas de oro en los tutoriales\n" +
                  "✅ Visitar todas las características principales\n" +
                  "✅ Ser un Patreon Supporter\n\n" +
                  "Por favor, usa el modo Manual o desbloquea el Developer Mode."
                : "Automatic mode is exclusively reserved for users with Developer Mode activated.\n\n" +
                  "To unlock this feature, you need:\n" +
                  "✅ Obtain all gold medals in tutorials\n" +
                  "✅ Visit all main features\n" +
                  "✅ Be a Patreon Supporter\n\n" +
                  "Please use Manual mode or unlock Developer Mode.";

            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void OpenSims3Updater()
        {
            try
            {
                bool isSpanish = IsSpanishLanguage();
                MessageBox.Show(
                    isSpanish
                        ? "✅ Developer Mode verificado. Abriendo instalador automático..."
                        : "✅ Developer Mode verified. Opening automatic installer...",
                    isSpanish ? "Acceso Concedido" : "Access Granted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Aquí abrirías la ventana Sims3Updater cuando la tengas creada
                var sims3Updater = new Sims3Updater();
                sims3Updater.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                bool isSpanish = IsSpanishLanguage();
                MessageBox.Show(
                    isSpanish
                        ? $"Error al abrir el instalador: {ex.Message}"
                        : $"Error opening installer: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ManualBtn_Click(object sender, MouseButtonEventArgs e)
        {
            // Por ahora no hace nada, como solicitaste
            bool isSpanish = IsSpanishLanguage();
            MessageBox.Show(
                isSpanish
                    ? "El modo manual estará disponible próximamente."
                    : "Manual mode will be available soon.",
                isSpanish ? "Próximamente" : "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}