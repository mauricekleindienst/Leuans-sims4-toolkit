using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace ModernDesign.MVVM.View
{
    public partial class OfflineWarningWindow : Window
    {
        public bool UserConfirmed { get; private set; } = false;

        public OfflineWarningWindow()
        {
            InitializeComponent();
            LoadLanguage();
        }

        private void LoadLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                WarningTitle.Text = "⚠️ ¡ADVERTENCIA!";
                WarningMessage.Text =
                    "Hacer esto si tienes una copia un poco antigua de tu juego puede causar que tus savegames se corrompan.\n\n" +
                    "No es la update en sí, son los mods los que corrompen el savegame.\n\n" +
                    "Así que lo más recomendado es que leas este post en EA si o si:";
                ProtectedBtn.Content = "Ya me he protegido";
                CancelBtn.Content = "Cancelar";
            }
            else
            {
                WarningTitle.Text = "⚠️ WARNING!";
                WarningMessage.Text =
                    "Doing this if you have a slightly old copy of your game can cause your savegames to become corrupted.\n\n" +
                    "It's not the update itself, it's the mods that corrupt the savegame.\n\n" +
                    "So it is highly recommended that you read this EA post:";
                ProtectedBtn.Content = "I'm Protected";
                CancelBtn.Content = "Cancel";
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = e.Uri.AbsoluteUri,
                    UseShellExecute = false
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProtectedBtn_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = true;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = false;
            this.DialogResult = false;
            this.Close();
        }
    }
}