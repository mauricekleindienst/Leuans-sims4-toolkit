using System;
using System.IO;
using System.Windows;

namespace ModernDesign.MVVM.View
{
    public partial class OfflineWarningWindow3 : Window
    {
        public bool UserConfirmed { get; private set; } = false;

        public OfflineWarningWindow3()
        {
            InitializeComponent();
            LoadLanguage();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void LoadLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                WarningTitle.Text = "⚠️ ADVERTENCIA CRÍTICA";
                WarningMessage.Text =
                    "⚠️ IMPORTANTE: Esta actualización es SOLO para versiones crackeadas y offline.\n\n" +
                    "🔒 PROTECCIÓN OBLIGATORIA:\n" +
                    "Antes de continuar, DEBES tener una copia de seguridad completa de tu carpeta de The Sims 4.\n\n" +
                    "❌ RIESGOS:\n" +
                    "• Si tu versión no es compatible, el juego NO funcionará\n" +
                    "• Podrías perder tu progreso si no tienes backup\n" +
                    "• No hay forma de revertir los cambios sin backup\n\n" +
                    "🎗 Una vez hayas hecho esto, ya no estarás en la categoria de 'Otras versiones', para actualizar en el futuro, deberás seleccionar 'Leuans Version'\n\n" +

                    "¿Tienes tu backup y deseas continuar bajo tu propio riesgo?";

                RequirementsTitle.Text = " REQUISITOS:";
                Requirement1.Text = "• Tener una versión crackeada (Anadius, FitGirl, etc.)";
                Requirement2.Text = "• Tener backup completo de tu carpeta del juego";
                Requirement3.Text = "• Saber restaurar el backup si algo sale mal";

                ProtectedBtn.Content = " Entiendo, continuar";
                CancelBtn.Content = "❌ Cancelar";
            }
            else
            {
                WarningTitle.Text = "⚠️ CRITICAL WARNING";
                WarningMessage.Text =
                    "⚠️ IMPORTANT: This update is ONLY for versions like Anadius, Fitgirl, Elamigos, etc.\n\n" +
                    "🔒 MANDATORY PROTECTION:\n" +
                    "Before continuing, you MUST have a complete backup of your The Sims 4 folder and your savegames.\n\n" +
                    "❓ HOW TO CREATE A BACKUP:\n" +
                    "- Go into your Documents folder and create a backup for the 'Electronic Arts' folder\n" +
                    "- Go into your Sims 4 root folder (where the game is installed) and create a backup for your whole game\n\n" +
                    "❌ RISKS:\n" +
                    "• If your version is not compatible, the game will NOT work and you will have to use our Cracking Tool\n" +
                    "• You could lose your progress if you don't have a backup\n" +
                    "• There's no way to revert changes without backup\n\n" +
                    "🎗 Once you do this, you will stop having 'Other Versions' and to update in the future you'll need to select 'Leuans Version'\n\n" +
                    "Do you have your backup and want to continue at your own risk?";

                RequirementsTitle.Text = " REQUIREMENTS:";
                Requirement1.Text = "• Have a cracked version (Anadius, FitGirl, Elamigos, etc.)";
                Requirement2.Text = "• Have complete backup of your game folder and your savegames (Documents Folder)";
                Requirement3.Text = "• Know how to restore the backup if something goes wrong";

                ProtectedBtn.Content = " I understand, continue";
                CancelBtn.Content = "❌ Cancel";
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