using System;
using System.Windows;
using System.Windows.Controls;

namespace ModernDesign.MVVM.View
{
    public partial class MainSelectionView : UserControl
    {
        public MainSelectionView()
        {
            InitializeComponent();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = "🎮 ¿Qué deseas hacer?";
                SubHeaderText.Text = "Selecciona una opción para continuar";
                DownloadDLCsBtn.Content = "Descargar DLC's";
                DownloadDLCsBtn.ToolTip = "Si ya posees el juego y quieres instalarle los DLC's";
                UpdateGameBtn.Content = "Actualizar el Juego";
                UpdateGameBtn.ToolTip = "Si quieres actualizar tu Sims 4 a la última versión";
            }
            else
            {
                HeaderText.Text = "🎮 What do you want to do?";
                SubHeaderText.Text = "Select an option to continue";
                DownloadDLCsBtn.Content = "Download DLC's";
                DownloadDLCsBtn.ToolTip = "If you already own the game and want to install DLC's";
                UpdateGameBtn.Content = "Update the Game";
                UpdateGameBtn.ToolTip = "If you want to update your Sims 4 to the latest version";
            }
        }

        private static bool IsSpanishLanguage()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
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

        private void DownloadDLCsBtn_Click(object sender, RoutedEventArgs e)
        {
            // Abrir InstallModeSelector como ventana
            var installModeSelector = new InstallModeSelector();
            installModeSelector.Owner = Window.GetWindow(this);
            installModeSelector.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            installModeSelector.ShowDialog();

            //  CERRAR MainSelectionWindow después de abrir InstallModeSelector
            Window.GetWindow(this)?.Close();
        }

        private void UpdateGameBtn_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar popup de advertencia
            bool isSpanish = IsSpanishLanguage();

            string message = isSpanish
                ? "⚠️ ADVERTENCIA IMPORTANTE ⚠️\n\n" +
                  "Te recordamos que esto solo es para las versiones offline y crackeadas.\n\n" +
                  "Si tienes el juego de Steam/EA, no necesitas actualizar tu juego, " +
                  "ya que tu plataforma lo actualizará automáticamente.\n\n" +
                  "¿Deseas continuar?"
                : "⚠️ IMPORTANT WARNING ⚠️\n\n" +
                  "Updating your game is only for offline and cracked versions.\n\n" +
                  "If you have the game from Steam/EA, you don't need to update your game, " +
                  "as your platform will update it automatically.\n\n" +
                  "Do you want to continue?";

            string title = isSpanish ? "Advertencia" : "Warning";

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Abrir UpdateVersionSelectorWindow como ventana
                var versionSelector = new UpdateVersionSelectorWindow();
                versionSelector.Owner = Window.GetWindow(this);
                versionSelector.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                versionSelector.ShowDialog();

                //  CERRAR MainSelectionWindow después de abrir UpdateVersionSelectorWindow
                Window.GetWindow(this)?.Close();
            }
        }
    }
}