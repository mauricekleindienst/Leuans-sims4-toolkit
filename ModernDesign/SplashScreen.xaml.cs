using Microsoft.Win32;
using ModernDesign.MVVM.View;   // Para UpdaterWindow
using ModernDesign.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Net.Http;
using System.Text;
using System.Security.Principal;

namespace ModernDesign
{
    public partial class SplashScreen : Window
    {
        // 🔹 Versión local del launcher
        private const string LocalLauncherVersion = "1.4.0";

        // 🔹 URL del archivo de versión remoto
        private const string VersionCheckUrl =
            "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/version.txt"; // Versionamiento

        // 🔹 URL donde vas a mandar al usuario a bajar el launcher nuevo
        private const string LauncherDownloadUrl =
            "https://github.com/Leuansin/leuan-dlcs/releases/download/LTK/LTK.exe"; // Launcher Actualizado

        // Download URLs (you can update versions later if needed)
        private const string Net48OfflineUrl =
            "https://go.microsoft.com/fwlink/?linkid=2088631"; // .NET 4.8 offline installer (x86/x64 ENU)


        public SplashScreen()
        {
            InitializeComponent();
            CleanUnlockerLocalFilesOnStartup();

            // 🔹 NUEVO: Ejecutar backups automáticos al inicio
            ExecuteStartupBackups();
        }

        /// <summary>
        /// Verifica si la aplicación se está ejecutando con privilegios de administrador
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ejecuta backups de savegames según configuración en preferences.ini
        /// </summary>
        private void ExecuteStartupBackups()
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] possiblePaths =
                {
                    Path.Combine(docs, "Electronic Arts", "Los Sims 4", "saves"),
                    Path.Combine(docs, "Electronic Arts", "The Sims 4", "saves")
                };

                string savesFolder = null;
                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        savesFolder = path;
                        break;
                    }
                }

                if (savesFolder != null)
                {
                    SaveGameBackupService.ExecuteBackupsOnStartup(savesFolder);

                    // Opcional: limpiar backups antiguos para no llenar el disco
                    SaveGameBackupService.CleanOldBackups(maxBackupsPerSlot: 10);
                }
            }
            catch
            {
                // No queremos romper el arranque si falla el backup
            }
        }

        /// <summary>
        /// Lee el language.ini y devuelve el código de idioma (en-US / es-ES).
        /// Si algo falla, retorna en-US por defecto.
        /// </summary>
        private string GetLanguageCode()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string toolkitFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
            string iniPath = Path.Combine(toolkitFolder, "language.ini");

            string languageCode = "en-US"; // default

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
                // si falla lectura, nos quedamos con en-US
            }

            return languageCode;
        }


        // No more telemetry.

        /// <summary>
        /// Lee profile.ini y verifica si PreloadImages está activado
        /// </summary>
        private bool ShouldPreloadImages()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string toolkitFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
                string profilePath = Path.Combine(toolkitFolder, "profile.ini");

                if (!File.Exists(profilePath))
                    return false;

                string[] lines = File.ReadAllLines(profilePath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("PreloadImages", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length >= 2)
                        {
                            string value = parts[1].Trim();
                            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            catch
            {
                // Si falla, no precargamos
            }

            return false;
        }

        private async Task<bool> IsLauncherUpToDateAsync()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Proxy = null; // por si hay proxy raro

                    string remoteText = await client.DownloadStringTaskAsync(new Uri(VersionCheckUrl));

                    if (string.IsNullOrWhiteSpace(remoteText))
                    {
                        // Si el archivo está vacío o raro, no bloqueamos el inicio
                        return false;
                    }

                    // El txt debería traer algo como "1.4.0"
                    string remoteVersion = remoteText.Trim();

                    //  Lógica tal como pediste:
                    // si EL STRING coincide, consideramos que está actualizado
                    bool isSameVersion = string.Equals(
                        LocalLauncherVersion,
                        remoteVersion,
                        StringComparison.OrdinalIgnoreCase);

                    return isSameVersion;
                }
            }
            catch
            {
                // Si no hay internet o falla algo, no matamos el launcher.
                return false;
            }
        }

        /// <summary>
        /// Se dispara cuando termina el fade-in del logo.
        /// Recién aquí empieza toda la secuencia de carga.
        /// </summary>
        private async void LogoFadeIn_Completed(object sender, EventArgs e)
        {
            await RunStartupSequenceAsync();
        }

        /// <summary>
        /// Mantiene el splash en pantalla mientras:
        /// 1) Revisa versión del launcher
        /// 2) Revisa dependencias (.NET)
        /// 3) Hace warm-up del UpdaterWindow (solo si PreloadImages = true)
        /// Luego hace fade out y abre el MainWindow.
        /// </summary>
        /// 
        public void OpenLeuanWebsite()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "https://leuan.zeroauno.com/index.html#downloads",
                    UseShellExecute = false
                });
            }
            catch
            {
                // Silently fail - don't interrupt user experience if browser fails to open
            }
        }
        private async Task RunStartupSequenceAsync()
        {
            try
            {
                // Detectamos idioma una vez para esta secuencia
                string languageCode = GetLanguageCode();
                bool isSpanish = languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

                // 1) Primero: comprobar versión del launcher
                UpdateProgress(1, isSpanish ? "Buscando actualizaciones..." : "Checking for updates...");

                bool isUpToDate = await IsLauncherUpToDateAsync();

                if (!isUpToDate)
                {
                    // Launcher desactualizado → mandar a la página de descarga y cerrar
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = LauncherDownloadUrl,
                            UseShellExecute = false
                        });
                    }
                    catch
                    {
                        // Si no puede abrir el navegador, avisamos igual
                    }

                    string msgText = isSpanish
                        ? "Hay una nueva versión de Leuan's Sims 4 Toolkit disponible.\n" +
                          "Por favor, descarga el launcher más reciente, borra este, y ejecuta el recien descargado."
                        : "A new version of Leuan's Sims 4 Toolkit is available.\n" +
                          "Please download the latest launcher, delete this one and open the new downloaded one.";

                    string msgTitle = isSpanish ? "Actualización requerida" : "Update required";

                    MessageBox.Show(
                        msgText,
                        msgTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    OpenLeuanWebsite();
                    Application.Current.Shutdown();
                    return;
                }

                // 1.5) Revisar si abrió como admin
                UpdateProgress(3, isSpanish ? "Verificando permisos..." : "Verifying permissions...");

                if (!IsRunningAsAdministrator())
                {
                    string msgText = isSpanish
                        ? "Has iniciado el ToolKit sin permisos de administrador.\n\n" +
                          "Esto no es obligatorio, pero algunas acciones (como extraer archivos a otros volúmenes o carpetas protegidas) pueden fallar por falta de permisos.\n\n" +
                          "Si experimentas errores, vuelve a ejecutar el programa como Administrador."
                        : "You have started the ToolKit without administrator permissions.\n\n" +
                          "This is not required, but some actions (such as extracting files to other volumes or protected folders) may fail due to lack of permissions.\n\n" +
                          "If you encounter errors, please restart the program as Administrator.";

                    string msgTitle = isSpanish
                        ? "Ejecutando sin permisos de Administrador"
                        : "Running without Administrator Permissions";

                    MessageBox.Show(
                        msgText,
                        msgTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Continue execution without admin
                }

                // 2) Si la versión es correcta, seguimos como antes
                UpdateProgress(5,
                    isSpanish ? "Comprobando requisitos del sistema..." : "Checking system requirements...");

                bool canContinue = await EnsureDependenciesAsync();

                // Si faltaba algo, lanzamos instaladores y cerramos la app:
                if (!canContinue)
                {
                    Application.Current.Shutdown();
                    return;
                }

                // 3) NUEVO: Solo precargar imágenes si está activado en profile.ini
                bool shouldPreload = ShouldPreloadImages();

                if (shouldPreload)
                {
                    UpdateProgress(50,
                        isSpanish ? "Preparando módulos del updater..." : "Preparing updater modules...");
                    await WarmUpUpdaterWindowAsync();
                }
                else
                {
                    UpdateProgress(50,
                        isSpanish ? "Omitiendo precarga de imágenes..." : "Skipping image preload...");
                    await Task.Delay(200); // pequeña pausa
                }

                UpdateProgress(100,
                    isSpanish ? "Casi listo..." : "Almost ready...");
                await Task.Delay(200); // pequeña pausa estética

                // 🔥 Fade out del splash
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    FillBehavior = FillBehavior.Stop
                };

                fadeOut.Completed += async (s, e) =>
                {
                    OpenNextWindow();

                    // No more telemetry.
                };

                this.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An error occurred while starting the application:\n\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
            }
        }


        /// <summary>
        /// Abre el MainWindow según el idioma configurado y cierra el splash.
        /// </summary>
        private void OpenNextWindow()
        {
            string languageCode = GetLanguageCode();

            Window nextWindow;

            if (string.Equals(languageCode, "es-ES", StringComparison.OrdinalIgnoreCase))
            {
                nextWindow = new MainWindow(); // podrías poner otro si tuvieras versión ES
            }
            else
            {
                nextWindow = new MainWindow();
            }

            nextWindow.Opacity = 0;
            nextWindow.Show();

            // Pequeño fade-in del main
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            nextWindow.BeginAnimation(Window.OpacityProperty, fadeIn);

            this.Close();
        }


        private void CleanUnlockerLocalFilesOnStartup()
        {
            try
            {
                //UnlockerService.CleanLocalUnlockerFiles();
            }
            catch
            {
                // No queremos romper el arranque si algo falla al borrar
            }
        }

        /// <summary>
        /// Actualiza la barra y el texto de carga.
        /// </summary>
        private void UpdateProgress(double value, string message)
        {
            if (value < 0) value = 0;
            if (value > 100) value = 100;

            LoadingProgressBar.Value = value;
            ProgressPercentText.Text = $"{(int)value}%";
            StatusText.Text = message;
        }

        /// <summary>
        /// Abre UpdaterWindow de forma silenciosa, fuera de la pantalla y sin mostrarlo en la barra de tareas,
        /// para que haga todo su trabajo de carga (DLCs, imágenes, autodetección, etc.)
        /// mientras todavía estamos en el splash.
        /// Luego se cierra.
        /// </summary>
        private async Task WarmUpUpdaterWindowAsync()
        {
            UpdaterWindow updater = null;

            try
            {
                updater = new UpdaterWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Opacity = 0,
                    ShowInTaskbar = false
                };

                var tcsRendered = new TaskCompletionSource<bool>();

                updater.ContentRendered += (s, e) =>
                {
                    if (!tcsRendered.Task.IsCompleted)
                        tcsRendered.TrySetResult(true);
                };

                updater.Show();

                // Esperamos a que se renderice al menos una vez
                await tcsRendered.Task;

                // margen extra para dar tiempo a que empiecen las descargas de imágenes
                await Task.Delay(500);
            }
            catch
            {
                // Si algo falla en el warm-up, no matamos la app. Simplemente seguimos.
            }
            finally
            {
                if (updater != null)
                {
                    try
                    {
                        updater.Close();
                    }
                    catch
                    {
                        // ignorar
                    }
                }
            }
        }

        /// <summary>
        /// Verifica si .NET 4.8 están instalados.
        /// Si falta algo, descarga e inicia los instaladores necesarios.
        /// Devuelve:
        ///     true  -> todo correcto, se puede continuar a la app
        ///     false -> se lanzaron instaladores; el usuario debe instalarlos y volver a abrir el programa
        /// </summary>
        private async Task<bool> EnsureDependenciesAsync()
        {
            bool hasNet48 = IsNet48OrLaterInstalled();

            // 👉 Si ya tenemos ambos, seguimos normal
            if (hasNet48)
                return true;

            string tempRoot = Path.Combine(Path.GetTempPath(), "LeuansSims4ToolkitSetup");
            if (!Directory.Exists(tempRoot))
            {
                Directory.CreateDirectory(tempRoot);
            }

            using (var client = new WebClient())
            {
                client.Proxy = null; // por si hay proxy raro

                // Si NO hay .NET 4.8, lo descargamos
                if (!hasNet48)
                {
                    UpdateProgress(15, "Downloading .NET Framework 4.8...");
                    string netPath = Path.Combine(tempRoot, "NDP48-x86-x64-AllOS-ENU.exe");

                    if (!File.Exists(netPath))
                    {
                        try
                        {
                            await client.DownloadFileTaskAsync(new Uri(Net48OfflineUrl), netPath);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Failed to download .NET Framework 4.8 installer.\n" + ex.Message, ex);
                        }
                    }

                    TryStartInstaller(netPath, ".NET Framework 4.8");
                }
            }

            MessageBox.Show(
                "Some required components have just been launched for installation (Microsoft .NET Framework 4.8).\n\n" +
                "Please complete their setup and then restart Leuan's Sims 4 Toolkit.",
                "Setup required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // No seguimos a la app en esta ejecución
            return false;
        }

        /// <summary>
        /// Comprueba en el registro si .NET Framework 4.8 o superior está instalado.
        /// Release >= 528040  -> 4.8 or later.
        /// </summary>
        private bool IsNet48OrLaterInstalled()
        {
            try
            {
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (ndpKey != null)
                    {
                        object releaseObj = ndpKey.GetValue("Release");
                        if (releaseObj is int releaseKey)
                        {
                            // 528040 = .NET Framework 4.8
                            return releaseKey >= 528040;
                        }
                    }
                }
            }
            catch
            {
                // If anything fails, assume it's NOT installed
            }

            return false;
        }


        /// <summary>
        /// Intenta lanzar un instalador .exe y lanza una excepción si falla.
        /// </summary>
        private void TryStartInstaller(string exePath, string friendlyName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start {friendlyName} installer at:\n{exePath}\n\n{ex.Message}", ex);
            }
        }
    }
}