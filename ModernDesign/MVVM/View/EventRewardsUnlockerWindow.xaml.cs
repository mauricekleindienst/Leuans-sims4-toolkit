using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Forms;
using ModernDesign.Localization;

namespace ModernDesign.MVVM.View
{
    public partial class EventRewardsWindow : Window
    {
        private string _modsPath = "";
        private const string DownloadUrl = "https://github.com/Johnn-sin/leuansin-dlcs/releases/download/FOMO/FOMO_by_Seijan.zip";

        public EventRewardsWindow()
        {
            InitializeComponent();
            ApplyLanguage();
            TryAutoDetectModsFolder();
        }

        private void ApplyLanguage()
        {
            bool es = LanguageManager.IsSpanish;

            Title = es ? "Leuan | Desbloqueador de Recompensas de Eventos" : "Leuan | Event Rewards Unlocker";
            HeaderTitle.Text = es ? "🎁  Desbloqueador de Recompensas" : "🎁  Event Rewards Unlocker";
            InfoTitle.Text = es ? "Desbloquea Objetos de Eventos" : "Unlock Unique Event Items";
            DescriptionText.Text = es
                ? "Esta herramienta instala un mod que modifica los headers del juego para desbloquear todas las recompensas de eventos limitados en Los Sims 4.\n\nEste mod fue creado por Seijan. ¡Todos los créditos son para ellos por ayudar a la comunidad a desbloquear todo!"
                : "This tool installs a mod that modifies game headers to unlock all limited-time event rewards in The Sims 4.\n\nThis mod was created by Seijan. All credits for this utility go to them for helping the community unlock everything!";

            PatreonBtn.Content = es ? "Visita el Patreon oficial de Seijan" : "Visit Seijan's official Patreon";
            FolderStepTitle.Text = es ? "Selecciona tu carpeta de Mods" : "Select your Mods Folder";
            FolderStepDesc.Text = es ? "Elige la carpeta ubicada en Documentos/Electronic Arts/The Sims 4/Mods" : "Choose the folder located in Documents/Electronic Arts/The Sims 4/Mods";
            ModsPathText.Text = es ? "📂 Haz clic en 'Buscar' para seleccionar tu carpeta de Mods..." : "📂 Click 'Browse' to select your Mods folder...";
            BrowseBtn.Content = es ? "Buscar" : "Browse";
            UnlockBtn.Content = es ? "🚀  ¡Desbloquear todas las recompensas!" : "🚀  Unlock All Event Rewards!";
            StatusLabel.Text = es ? "¡Instalado correctamente! Reinicia tu juego." : "Successfully installed! Restart your game.";
        }

        private void TryAutoDetectModsFolder()
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] basePaths = {
                    Path.Combine(docs, "Electronic Arts", "The Sims 4"),
                    Path.Combine(docs, "Electronic Arts", "Los Sims 4")
                };

                foreach (var path in basePaths)
                {
                    string mods = Path.Combine(path, "Mods");
                    if (Directory.Exists(mods))
                    {
                        _modsPath = mods;
                        ModsPathText.Text = $"📂 {_modsPath}";
                        ModsPathText.Foreground = System.Windows.Media.Brushes.White;
                        UnlockBtn.IsEnabled = true;
                        break;
                    }
                }
            }
            catch { }
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = LanguageManager.IsSpanish ? "Selecciona tu carpeta de Mods de Los Sims 4" : "Select your Sims 4 Mods folder";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _modsPath = dialog.SelectedPath;
                    ModsPathText.Text = $"📂 {_modsPath}";
                    ModsPathText.Foreground = System.Windows.Media.Brushes.White;
                    UnlockBtn.IsEnabled = true;
                }
            }
        }

        private async void UnlockBtn_Click(object sender, RoutedEventArgs e)
        {
            UnlockBtn.IsEnabled = false;
            string originalContent = UnlockBtn.Content.ToString();
            UnlockBtn.Content = LanguageManager.IsSpanish ? "Descargando..." : "Downloading...";

            try
            {
                string tempZip = Path.Combine(Path.GetTempPath(), "FOMO_Seijan.zip");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(DownloadUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempZip, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                UnlockBtn.Content = LanguageManager.IsSpanish ? "Extrayendo..." : "Extracting...";

                await Task.Run(() =>
                {
                    using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // Solo extraemos archivos .package
                            if (entry.FullName.EndsWith(".package", StringComparison.OrdinalIgnoreCase))
                            {
                                // FLATTEN: Quitamos cualquier carpeta intermedia y guardamos directo en Mods
                                string destinationPath = Path.Combine(_modsPath, entry.Name);
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                });

                StatusLabel.Visibility = Visibility.Visible;
                UnlockBtn.Content = LanguageManager.IsSpanish ? "¡Listo!" : "Done!";

                System.Windows.MessageBox.Show(
                    LanguageManager.IsSpanish ? "¡Recompensas desbloqueadas! Los archivos se instalaron directamente en tu carpeta Mods." : "Rewards unlocked! Files were installed directly into your Mods folder.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UnlockBtn.IsEnabled = true;
                UnlockBtn.Content = originalContent;
            }
        }

        private void PatreonBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.patreon.com/posts/bg-fomo-all-in-3-120588981") { UseShellExecute = true });
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) => NavigationClose();
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => NavigationClose();

        private void NavigationClose()
        {
            var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };
            fadeOut.Completed += (s, args) => this.Close();
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }
    }
}