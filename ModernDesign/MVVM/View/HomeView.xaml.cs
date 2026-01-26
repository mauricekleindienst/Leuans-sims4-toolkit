using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ModernDesign.Managers;
using System.Threading.Tasks;

namespace ModernDesign.MVVM.View
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            this.Loaded += HomeView_Loaded;
        }


        private async void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLanguage();
            CheckDeveloperModeStatus();

            // Verificar si debe mostrar el tutorial
            if (!TutorialManager.HasCompletedTutorial())
            {
                // Mostrar la ventana de tutorial
                var tutorialWindow = new TutorialWelcomeWindow();
                tutorialWindow.Owner = Window.GetWindow(this);
                tutorialWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                tutorialWindow.ShowDialog();

                // Después de cerrar el tutorial, marcar como completado
                TutorialManager.SetTutorialCompleted(true);
            }

            // Verificar anuncios
            await CheckAndShowAnnouncementAsync();
        }

        private async Task CheckAndShowAnnouncementAsync()
        {
            try
            {
                var announcementData = await AnnouncementManager.GetAnnouncementAsync();

                if (announcementData.IsEnabled && !string.IsNullOrWhiteSpace(announcementData.Text))
                {
                    // Mostrar la ventana de anuncio con imagen y logo
                    AnnouncementWindow announcementWindow = new AnnouncementWindow(
                        announcementData.Text,
                        announcementData.ImageUrl,
                        announcementData.LogoUrl
                    );

                    announcementWindow.Owner = Window.GetWindow(this);
                    announcementWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    announcementWindow.ShowDialog();
                }
            }
            catch (Exception)
            {
                // Si falla, simplemente no mostramos el anuncio
                // No interrumpimos la experiencia del usuario
            }
        }

        private void CheckDeveloperModeStatus()
        {
            bool isDeveloperUnlocked = DeveloperModeManager.IsDeveloperModeUnlocked();

            if (isDeveloperUnlocked)
            {
                // OCULTAR la card de Support
                SupportCard.Visibility = Visibility.Collapsed;
                SupportTitleText.Visibility = Visibility.Collapsed;

                // MOSTRAR el mensaje de bienvenida para Supporters
                SupporterWelcomeCard.Visibility = Visibility.Visible;
            }
            else
            {
                // MOSTRAR la card de Support
                SupportCard.Visibility = Visibility.Visible;
                SupportTitleText.Visibility = Visibility.Visible;

                // OCULTAR el mensaje de bienvenida
                SupporterWelcomeCard.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyLanguage()
        {
            string languageCode = GetLanguageCode();
            bool isSpanish = languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            if (isSpanish)
            {
                // Textos en español
                PopularTitleText.Text = "Popular";
                UpdaterTooltipText.Text = "Instala o actualiza automáticamente todos los DLC disponibles.";
                UpdaterMainTitle.Text = "Instala Todos Los\n+105 DLC's";
                UpdaterCallToAction.Text = "¡Haz Clic Aquí!";
                UpdaterInMemory.Text = "En memoria de Anadius.";
                RepairTooltipText.Text = "Repara archivos corruptos del juego, estructura y carpetas esenciales.";

                // Textos de Support en español
                SupportTitleText.Text = "¿Te ha sido útil este ToolKit?";
                SupportMainTitle.Text = "¡Apóyame con solo $1!";
                SupportSubtitleRun.Text = "Donando SOLO $1 te convertirás en un ";
                SupportSubtitleRun2.Text = " y obtendrás:";
                Benefit1.Text = "✓ Eliminar este mensaje";
                Benefit2Run1.Text = "Modo Desarrollador (Revisa ";
                Benefit2Run2.Text = " el botón que dice ";
                Benefit3.Text = "✓ Rol en Discord";
                Benefit4.Text = "✓ Mi corazón ❤️";
                SupportCTA.Text = "¡Haz clic aquí para apoyar! ☕";

                // Mensaje de Supporter
                SupporterWelcomeText.Text = " ";
            }
            else
            {
                // Textos en inglés (por defecto)
                PopularTitleText.Text = "Popular";
                UpdaterTooltipText.Text = "Automatically installs or updates all available DLCs.";
                UpdaterMainTitle.Text = "Install All The\n+106 DLC's";
                UpdaterCallToAction.Text = "Click Here!";
                UpdaterInMemory.Text = "In memory of Anadius.";
                RepairTooltipText.Text = "Unlock all DLC's in The Sims 4.";

                // Textos de Support en inglés
                SupportTitleText.Text = "Has this ToolKit been helpful to you?";
                SupportMainTitle.Text = "Support Me with only $1!";
                SupportSubtitleRun.Text = "Donating ONLY $1 will make you a ";
                SupportSubtitleRun2.Text = " and you will gain:";
                Benefit1.Text = "✓ Removing this message";
                Benefit2Run1.Text = "Developer Mode (Check ";
                Benefit2Run2.Text = " the button that says ";
                Benefit3.Text = "✓ Discord Role";
                Benefit4.Text = "✓ My heart ❤️";
                SupportCTA.Text = "Click here to support! ☕";

                // Mensaje de Supporter
                SupporterWelcomeText.Text = "";
            }
        }

        private string GetLanguageCode()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string toolkitFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
            string iniPath = Path.Combine(toolkitFolder, "language.ini");

            string languageCode = "en-US";

            try
            {
                if (File.Exists(iniPath))
                {
                    string[] lines = File.ReadAllLines(iniPath);
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
            catch
            {
                // Si falla la lectura, nos quedamos con en-US
            }

            return languageCode;
        }

        // Evento para la tarjeta de Updater
        private void UpdaterCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Crear instancia de MainSelectionWindow
            InstallModeSelector installModeSelector = new InstallModeSelector();

            // Establecer la ventana actual como "Owner" (padre)
            installModeSelector.Owner = Window.GetWindow(this);

            // Centrar la ventana respecto al padre
            installModeSelector.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Mostrar la ventana
            installModeSelector.ShowDialog();
        }

        // Evento para la tarjeta de Repair
        private void RepairCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Crear instancia de la ventana Repair
            DLCUnlockerWindow DLCUnlockerWindow = new DLCUnlockerWindow();

            // Establecer la ventana actual como "Owner" (padre)
            DLCUnlockerWindow.Owner = Window.GetWindow(this);

            // Centrar la ventana respecto al padre
            DLCUnlockerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Mostrar la ventana
            DLCUnlockerWindow.ShowDialog();
        }

        // Nuevo evento para la tarjeta de Support
        private void SupportCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Abrir el link de Ko-fi en el navegador predeterminado
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "https://ko-fi.com/leuan",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Ko-fi link: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }



    }
}