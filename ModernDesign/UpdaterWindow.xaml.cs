using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Microsoft.Win32;
using ModernDesign.MVVM.View;
using Newtonsoft.Json;

namespace ModernDesign.MVVM.View
{
    public partial class UpdaterWindow : Window
    {
        private List<CheckBox> _allCheckBoxes = new List<CheckBox>();
        private string _simsPath = "";
        private readonly List<DLCInfo> _dlcList;
        private readonly string _tempFolder;

        // NUEVO: DLC's Dinámicos
        private const string DLC_DATABASE_URL = "https://raw.githubusercontent.com/Leuansin/Leuans-sims4-toolkit/refs/heads/main/Misc/Listirijilla.json";
        private static List<DLCInfo> _dlcListCache = null;

        // NUEVO: Rutas para Goku y Kamehameha
        public string GokuPoseImagePath { get; set; }
        public string KamehamehaGifPath { get; set; }

        // NUEVO: Sistema de gestión de hilos mejorado
        private readonly ThreadManager _threadManager = new ThreadManager();
        private readonly DownloadEngine _downloadEngine;
        private readonly Extractor _extractor;
        private readonly Logger _logger;
        private readonly SevenZipFinder _sevenZipFinder;

        // NUEVO: Sistema de cola de descargas
        private readonly DownloadQueue _downloadQueue;
        private CancellationTokenSource _globalCancellation;

        // NUEVO: Pausa y Cancelar
        private bool _isPaused = false;
        private readonly object _pauseLock = new object();
        public static bool IsPaused { get; private set; } = false;

        // NUEVO: URL del archivo de links en GitHub
        private const string DLC_LINKS_URL = "https://raw.githubusercontent.com/Leuansin/Leuans-sims4-toolkit/refs/heads/main/Misc/Listirijilla.txt";
        private static Dictionary<string, List<string>> _dlcLinksCache = null;

        public UpdaterWindow()
        {
            InitializeComponent();

            //  NUEVO: Configurar DataContext para binding de imágenes
            this.DataContext = this;
            //  NUEVO: Descargar imágenes de Goku y Kamehameha
            Task.Run(() => DownloadGokuAssets());
            //  PROTECCIÓN GLOBAL contra crashes
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>

            {
                Debug.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            };

            Dispatcher.UnhandledException += (s, e) =>
            {
                Debug.WriteLine($"Dispatcher exception: {e.Exception.Message}");
                e.Handled = true;
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue running.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };

            // NUEVO: Inicializar sistemas mejorados
            _logger = new Logger(LogTextBox);
            _downloadEngine = new DownloadEngine(_logger);
            _extractor = new Extractor(_logger);
            _sevenZipFinder = new SevenZipFinder(_logger);
            _downloadQueue = new DownloadQueue(2, _logger); // Max 2 descargas simultáneas

            // Temp folder
            _tempFolder = Path.Combine(Path.GetTempPath(), "LeuansSims4Toolkit");
            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
                try
                {
                    var di = new DirectoryInfo(_tempFolder);
                    di.Attributes |= FileAttributes.Hidden;
                }
                catch { }
            }

            // Cargar DLC list
            _dlcList = GetDLCList();
            PopulateDLCList();

            // NUEVO: Cargar DLCs corruptos si existen
            LoadCorruptDLCsIfExists();

            // NUEVO: Detectar y limpiar archivos huérfanos al inicio
            Loaded += async (s, e) =>
            {
                await AutoDetectSimsPath();
                UpdateUnlockerStatus();
                await CheckAndCleanOrphanedFilesOnStartup();
            };

            //  CHECK IF DLC IMAGES ARE DISABLED
            if (!ShouldLoadDLCImages())
            {
                bool isSpanish = IsSpanishLanguage();
                string message = isSpanish
                    ? "Las imágenes de DLC están actualmente desactivadas.\n\n" +
                      "¿Deseas activarlas?\n\n" +
                      "⚠️ Advertencia: Activar las imágenes puede consumir hasta 1GB de RAM."
                    : "DLC images are currently disabled.\n\n" +
                      "Would you like to enable them?\n\n" +
                      "⚠️ Warning: Enabling images may consume up to 1GB of RAM.";

                string title = isSpanish ? "¿Activar Imágenes de DLC?" : "Enable DLC Images?";

                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SetLoadDLCImages(true);

                    // NUEVO: Verificar y descargar SOLO las imágenes faltantes
                    Task.Run(async () =>
                    {
                        await CheckAndDownloadMissingImages(isSpanish);
                    });

                    string infoMessage = isSpanish
                        ? "Las imágenes de DLC se cargarán siempre de ahora en adelante.\n\n" +
                          "Si deseas desactivar esta opción, puedes ir a \"Settings\" y desactivarla desde ahí."
                        : "DLC images will now always load from now on.\n\n" +
                          "If you want to disable this option, you can go to \"Settings\" and turn it off there.";

                    string infoTitle = isSpanish ? "Imágenes Activadas" : "Images Enabled";
                    MessageBox.Show(infoMessage, infoTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                //  Si las imágenes YA están activadas, verificar si faltan algunas
                Task.Run(async () =>
                {
                    await CheckAndDownloadMissingImages(IsSpanishLanguage());
                });
            }

            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }


        // ================================================================
        //                    DESCARGA DE ASSETS DE GOKU
        // ================================================================
        private async Task DownloadGokuAssets()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string qolFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "qol");

                // Crear carpeta si no existe
                if (!Directory.Exists(qolFolder))
                {
                    Directory.CreateDirectory(qolFolder);
                }

                string gokuPosePath = Path.Combine(qolFolder, "gokupose.png");
                string kamehamehaPath = Path.Combine(qolFolder, "kamehameha.gif");

                // URLs de GitHub (ajusta según tu repositorio)
                string gokuPoseUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/gokupose.png";
                string kamehamehaUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/kameha-gif.gif";

                using (var client = new WebClient())
                {
                    // Descargar Goku Pose si no existe
                    if (!File.Exists(gokuPosePath))
                    {
                        await client.DownloadFileTaskAsync(new Uri(gokuPoseUrl), gokuPosePath);
                        Debug.WriteLine(" Downloaded gokupose.png");
                    }

                    // Descargar Kamehameha GIF si no existe
                    if (!File.Exists(kamehamehaPath))
                    {
                        await client.DownloadFileTaskAsync(new Uri(kamehamehaUrl), kamehamehaPath);
                        Debug.WriteLine(" Downloaded kamehameha.gif");
                    }
                }

                // Actualizar rutas en el UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    GokuPoseImagePath = gokuPosePath;
                    KamehamehaGifPath = kamehamehaPath;

                    // Forzar actualización de bindings
                    if (GokuImage != null)
                        GokuImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(gokuPosePath));
                    if (KamehamehaBeam != null)
                        KamehamehaBeam.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(kamehamehaPath));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error downloading Goku assets: {ex.Message}");
            }
        }
        private void LoadCorruptDLCsIfExists()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string corruptDLCsFile = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "corrupt_dlcs.txt");

                if (!File.Exists(corruptDLCsFile))
                    return;

                var corruptDLCs = File.ReadAllLines(corruptDLCsFile).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                if (corruptDLCs.Count == 0)
                    return;

                bool isSpanish = IsSpanishLanguage();

                // Mostrar mensaje
                MessageBox.Show(
                    isSpanish
                        ? $"Se detectaron {corruptDLCs.Count} DLC(s) corrupto(s) desde el verificador de integridad.\n\n" +
                          "Los DLCs corruptos han sido seleccionados automáticamente.\n\n" +
                          "Haz clic en 'Descargar' para repararlos."
                        : $"Detected {corruptDLCs.Count} corrupt DLC(s) from the integrity checker.\n\n" +
                          "Corrupt DLCs have been automatically selected.\n\n" +
                          "Click 'Download' to repair them.",
                    isSpanish ? "DLCs Corruptos Detectados" : "Corrupt DLCs Detected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Seleccionar DLCs corruptos en la UI
                Dispatcher.InvokeAsync(() =>
                {
                    foreach (var checkbox in DLCList.Children.OfType<CheckBox>())
                    {
                        string dlcId = checkbox.Tag as string;
                        if (corruptDLCs.Contains(dlcId))
                        {
                            checkbox.IsChecked = true;
                        }
                    }
                });

                // Eliminar archivo temporal
                File.Delete(corruptDLCsFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading corrupt DLCs: {ex.Message}");
            }
        }

        // ================================================================
        //                    CLEANUP DE ARCHIVOS HUÉRFANOS
        // ================================================================
        private async Task CheckAndCleanOrphanedFilesOnStartup()
        {
            try
            {
                if (!Directory.Exists(_tempFolder))
                    return;

                var allFiles = Directory.GetFiles(_tempFolder);
                var orphanedFiles = allFiles
                    .Where(f => !Path.GetFileName(f).EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (orphanedFiles.Count == 0)
                    return;

                Debug.WriteLine($"🔍 Orphaned files detected: {orphanedFiles.Count}");
                foreach (var file in orphanedFiles)
                {
                    Debug.WriteLine($"  - {Path.GetFileName(file)}");
                }

                bool isSpanish = IsSpanishLanguage();
                string fileList = string.Join("\n", orphanedFiles.Select(f => $"• {Path.GetFileName(f)}"));

                string message = isSpanish
                    ? $"⚠️ ADVERTENCIA CRÍTICA ⚠️\n\n" +
                      $"Se detectaron {orphanedFiles.Count} archivo(s) corrupto(s) en la carpeta temporal:\n\n" +
                      $"{fileList}\n\n" +
                      "Estos archivos pueden causar CRASHES si intentas descargar.\n\n" +
                      "¿Deseas eliminarlos automáticamente?\n\n" +
                      "• SÍ: Eliminar archivos corruptos (RECOMENDADO)\n" +
                      "• NO: Mantener archivos (CAUSARÁ ERRORES)"
                    : $"⚠️ CRITICAL WARNING ⚠️\n\n" +
                      $"Detected {orphanedFiles.Count} corrupted file(s) in temporary folder:\n\n" +
                      $"{fileList}\n\n" +
                      "These files WILL cause CRASHES if you try to download.\n\n" +
                      "Do you want to delete them automatically?\n\n" +
                      "• YES: Delete corrupted files (RECOMMENDED)\n" +
                      "• NO: Keep files (WILL CAUSE ERRORS)";

                string title = isSpanish ? "⚠️ Archivos Corruptos Detectados" : "⚠️ Corrupted Files Detected";

                var result = await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                );

                if (result == MessageBoxResult.Yes)
                {
                    int deletedCount = 0;
                    var failedFiles = new List<string>();

                    foreach (var file in orphanedFiles)
                    {
                        try
                        {
                            if (File.Exists(file))
                            {
                                File.SetAttributes(file, FileAttributes.Normal);
                                File.Delete(file);
                                deletedCount++;
                                Debug.WriteLine($" Deleted orphaned file: {Path.GetFileName(file)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"❌ Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                            failedFiles.Add(Path.GetFileName(file));
                        }
                    }

                    string resultMessage = isSpanish
                        ? $" Limpieza completada:\n\n" +
                          $"• Archivos eliminados: {deletedCount}\n" +
                          $"• Archivos fallidos: {failedFiles.Count}\n\n" +
                          (failedFiles.Count > 0
                              ? $"⚠️ No se pudieron eliminar:\n{string.Join("\n", failedFiles.Select(f => $"• {f}"))}\n\n" +
                                "Cierra Origin/EA App completamente y reinicia la aplicación."
                              : " Ahora puedes descargar sin problemas.")
                        : $" Cleanup completed:\n\n" +
                          $"• Files deleted: {deletedCount}\n" +
                          $"• Failed files: {failedFiles.Count}\n\n" +
                          (failedFiles.Count > 0
                              ? $"⚠️ Could not delete:\n{string.Join("\n", failedFiles.Select(f => $"• {f}"))}\n\n" +
                                "Close Origin/EA App completely and restart the application."
                              : " You can now download without issues.");

                    string resultTitle = isSpanish ? "Resultado de Limpieza" : "Cleanup Result";

                    await Dispatcher.InvokeAsync(() =>
                        MessageBox.Show(resultMessage, resultTitle, MessageBoxButton.OK,
                            failedFiles.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information)
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during startup orphaned files check: {ex.Message}");
            }
        }

        // ================================================================
        //                    DETECCIÓN AUTOMÁTICA DE RUTA
        // ================================================================
        private async Task AutoDetectSimsPath()
        {
            StatusText.Text = "  (Searching automatically...)";

            await Task.Run(() =>
            {
                var commonPaths = new[]
                {
                    @"C:\Program Files\EA Games\The Sims 4",
                    @"C:\Program Files (x86)\EA Games\The Sims 4",
                    @"C:\Program Files\Origin Games\The Sims 4",
                    @"C:\Program Files (x86)\Origin Games\The Sims 4",
                    @"D:\Games\The Sims 4",
                    @"D:\Origin Games\The Sims 4",
                    @"D:\The Sims 4",
                    @"E:\Games\The Sims 4",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EA Games", "The Sims 4"),
                };

                foreach (var path in commonPaths)
                {
                    var exePath = Path.Combine(path, "Game", "Bin", "TS4_x64.exe");
                    if (File.Exists(exePath))
                    {
                        var rootPath = Directory.GetParent(Directory.GetParent(exePath).FullName).FullName;
                        rootPath = Directory.GetParent(rootPath).FullName;
                        Dispatcher.Invoke(() => SetSimsPath(rootPath, true));
                        return;
                    }
                }

                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Maxis\The Sims 4"))
                    {
                        if (key != null)
                        {
                            var installDir = key.GetValue("Install Dir") as string;
                            if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                            {
                                Dispatcher.Invoke(() => SetSimsPath(installDir, true));
                                return;
                            }
                        }
                    }
                }
                catch { }

                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "  (Not found - select manually)";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                });
            });
        }

        private void SetSimsPath(string path, bool autoDetected = false)
        {
            _simsPath = path;
            PathTextBlock.Text = path;
            PathTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));

            StatusText.Text = autoDetected ? "  (✓ Auto-detected)" : "  (✓ Selected)";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));

            foreach (CheckBox cb in DLCList.Children) cb.IsEnabled = true;
            SelectAllBtn.IsEnabled = true;
            DeselectAllBtn.IsEnabled = true;

            ApplyInstalledFlags();
            UpdateSelectionCount();
            UpdateTotalValue();
        }

        // ================================================================
        //                    BOTÓN DE DESCARGA PRINCIPAL
        // ================================================================
        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            //  Verificar si ya hay descargas en progreso
            if (_threadManager.IsDownloading)
            {
                bool isSpanish = IsSpanishLanguage();
                string message = isSpanish
                    ? "⚠️ Ya hay una descarga en progreso.\n\nPor favor espera a que termine."
                    : "⚠️ A download is already in progress.\n\nPlease wait for it to finish.";
                string title = isSpanish ? "Descarga en Progreso" : "Download in Progress";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //  Verificar conexión a internet
            if (!NetworkChecker.IsOnline())
            {
                bool isSpanish = IsSpanishLanguage();
                string message = isSpanish
                    ? "❌ No hay conexión a internet.\n\nPor favor verifica tu conexión y vuelve a intentar."
                    : "❌ No internet connection.\n\nPlease check your connection and try again.";
                string title = isSpanish ? "Sin Conexión" : "No Connection";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //  Verificar espacio en disco
            var hasSpace = DiskChecker.CheckDiskSpace(_simsPath, 50);
            if (!hasSpace.Item1)
            {
                bool isSpanish = IsSpanishLanguage();
                string message = isSpanish
                    ? $"❌ Espacio insuficiente en disco.\n\nEspacio libre: {hasSpace.Item2} GB\nRequerido: 50 GB\n\n" +
                      "Por favor libera espacio y vuelve a intentar."
                    : $"❌ Insufficient disk space.\n\nFree space: {hasSpace.Item2} GB\nRequired: 50 GB\n\n" +
                      "Please free up space and try again.";
                string title = isSpanish ? "Disco Lleno" : "Disk Full";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedCheckboxes = DLCList.Children.OfType<CheckBox>().Where(cb => cb.IsChecked == true).ToList();
            var selectedDLCs = selectedCheckboxes
                .Select(cb => _dlcList.First(d => d.Id == (string)cb.Tag))
                .ToList();

            //  Manejar Offline Mode
            var offlineModeDLC = selectedDLCs.FirstOrDefault(dlc => dlc.IsOfflineMode);
            if (offlineModeDLC != null)
            {
                bool isSpanish = IsSpanishLanguage();
                string message = isSpanish
                    ? "¿Tienes una copia legítima del juego?\n\n" +
                      "(Esto significa si descargaste tu juego base desde EA/Steam/Origin)\n\n" +
                      "Presiona \"Sí\" si tienes una copia legítima.\n" +
                      "Presiona \"No\" si tienes una versión crackeada y portable"
                    : "Do you have a legit copy of the game?\n\n" +
                      "(This means if you downloaded your base game from EA/Steam/Origin)\n\n" +
                      "Press \"Yes\" if you have a legit copy.\n" +
                      "Press \"No\" if you have a cracked and portable version";
                string title = isSpanish ? "Tipo de Instalación" : "Installation Type";
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    offlineModeDLC.GetType().GetProperty("Url").SetValue(offlineModeDLC,
                        "https://github.com/Leuansin/leuan-dlcs/releases/download/latestupdateandcrack/LatestUpdateAndCrack.zip");
                }
                else
                {
                    var warningWindow = new OfflineWarningWindow { Owner = this };
                    bool? warningResult = warningWindow.ShowDialog();
                    if (warningResult == true && warningWindow.UserConfirmed)
                    {
                        offlineModeDLC.GetType().GetProperty("Url").SetValue(offlineModeDLC,
                            "https://github.com/Leuansin/leuan-dlcs/releases/download/latestupdateandcrack/LatestUpdateAndCrack.zip");
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // CORRECCIÓN: Seleccionar si el estado NO es "Installed" (permite Incomplete y Corrupted)
            var selected = selectedDLCs.Where(dlc => IsDlcInstalled(dlc).Item2 != "Installed").ToList();

            if (!selected.Any())
            {
                MessageBox.Show(
                    "All selected DLCs are already installed.\nNothing to download.",
                    "Nothing to do",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Iniciar proceso
            _globalCancellation = new CancellationTokenSource();
            _threadManager.IsDownloading = true;

            // UI: Taskbar y Botones
            TaskbarInfo.ProgressState = TaskbarItemProgressState.Paused; // Amarillo
            DownloadBtn.Visibility = Visibility.Collapsed;
            UninstallBtn.Visibility = Visibility.Collapsed;
            PauseBtn.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;

            SelectAllBtn.IsEnabled = false;
            DeselectAllBtn.IsEnabled = false;
            foreach (CheckBox cb in DLCList.Children) cb.IsEnabled = false;

            _isPaused = false;
            IsPaused = false;
            ProgressPanel.Visibility = Visibility.Visible;
            LogTextBox.Clear();

            _logger.Log("=== Starting DLC Installation ===");
            _logger.Log($"Total DLCs to install: {selected.Count}");

            var errors = new List<string>();
            int completedCount = 0;

            try
            {
                foreach (var dlc in selected)
                {
                    if (_globalCancellation.Token.IsCancellationRequested)
                    {
                        _logger.Log("Installation cancelled by user");
                        break;
                    }

                    try
                    {
                        completedCount++;
                        UpdateProgress(dlc.Name, completedCount, selected.Count, 0);
                        await DownloadAndExtractDLC(dlc, completedCount, selected.Count);
                        _logger.Log($" Successfully installed: {dlc.Name}");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        errors.Add($"{dlc.Id} - {dlc.Name}: {ex.Message}");
                        _logger.Log($"❌ Failed to install {dlc.Name}: {ex.Message}");
                    }
                }

                if (errors.Count == 0 && !_globalCancellation.Token.IsCancellationRequested)
                {
                    WriteGameDirToProfile(_simsPath);
                    decimal totalSaved = selected.Sum(dlc => dlc.Price);
                    MessageBox.Show(
                        $"Successfully downloaded {selected.Count} DLC(s)!\n💰 You've just saved ${totalSaved:F2} USD!",
                        "Download completed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (errors.Count > 0)
                {
                    MessageBox.Show("Completed with errors. Check the log.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                bool isSpanish = IsSpanishLanguage();
                MessageBox.Show(isSpanish ? "Descargas canceladas." : "Downloads cancelled.",
                    isSpanish ? "Cancelado" : "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Log($"❌ Critical error: {ex.Message}");
            }
            finally
            {
                // 🔥 CORRECCIÓN: Esto garantiza que los botones vuelvan siempre
                _threadManager.IsDownloading = false;
                _globalCancellation?.Dispose();
                _globalCancellation = null;

                TaskbarInfo.ProgressState = TaskbarItemProgressState.None;
                TaskbarInfo.ProgressValue = 0;

                await Dispatcher.InvokeAsync(() =>
                {
                    // Restaurar visibilidad
                    DownloadBtn.Visibility = Visibility.Visible;
                    UninstallBtn.Visibility = Visibility.Visible;
                    PauseBtn.Visibility = Visibility.Collapsed;
                    CancelBtn.Visibility = Visibility.Collapsed;
                    PauseBtn.Content = "⏸️  Pause";

                    // Habilitar elementos
                    foreach (CheckBox cb in DLCList.Children) cb.IsEnabled = true;
                    SelectAllBtn.IsEnabled = true;
                    DeselectAllBtn.IsEnabled = true;

                    _isPaused = false;
                    IsPaused = false;

                    ApplyInstalledFlags(); // Refrescar visuales
                    UpdateSelectionCount(); // Refrescar botones habilitados
                    ProgressPanel.Visibility = Visibility.Collapsed;
                });

                _threadManager.CleanupTemporaryFiles(_tempFolder);
            }
        }

        // ================================================================
        //                    DESCARGA Y EXTRACCIÓN MEJORADA
        // ================================================================
        private async Task DownloadAndExtractDLC(DLCInfo dlc, int currentIndex, int totalCount)
        {
            //  Detectar si es multiparte
            if (dlc.IsMultipart)
            {
                await DownloadMultipartDLC(dlc, currentIndex, totalCount);
            }
            else
            {
                await DownloadSingleDLC(dlc, currentIndex, totalCount);
            }
        }

        // ================================================================
        //                    DESINSTALACIÓN DE DLCs
        // ================================================================
        private async void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedDLCs = DLCList.Children.OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => _dlcList.First(d => d.Id == (string)cb.Tag))
                .ToList();

            if (!selectedDLCs.Any())
            {
                MessageBox.Show(
                    "No DLCs selected for uninstallation.",
                    "Nothing to uninstall",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            bool isSpanish = IsSpanishLanguage();
            string message = isSpanish
                ? $"¿Estás seguro de que deseas desinstalar {selectedDLCs.Count} DLC(s)?\n\nEsta acción no se puede deshacer."
                : $"Are you sure you want to uninstall {selectedDLCs.Count} DLC(s)?\n\nThis action cannot be undone.";
            string title = isSpanish ? "Confirmar Desinstalación" : "Confirm Uninstallation";

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            UninstallBtn.IsEnabled = false;
            DownloadBtn.IsEnabled = false;
            LogTextBox.Clear();

            _logger.Log("=== Starting DLC Uninstallation ===");
            _logger.Log($"Total DLCs to uninstall: {selectedDLCs.Count}");

            int uninstalled = 0;
            var errors = new List<string>();

            foreach (var dlc in selectedDLCs)
            {
                try
                {
                    _logger.Log($"Uninstalling: {dlc.Name}");

                    string rootDlcFolder = Path.Combine(_simsPath, dlc.Id);
                    string installerDlcFolder = Path.Combine(_simsPath, "__Installer", "DLC", dlc.Id);

                    if (Directory.Exists(rootDlcFolder))
                    {
                        Directory.Delete(rootDlcFolder, true);
                        _logger.Log($"   Deleted root folder: {dlc.Id}");
                    }

                    if (Directory.Exists(installerDlcFolder))
                    {
                        Directory.Delete(installerDlcFolder, true);
                        _logger.Log($"   Deleted installer folder: __Installer/DLC/{dlc.Id}");
                    }

                    uninstalled++;
                    _logger.Log($" Successfully uninstalled: {dlc.Name}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{dlc.Id} - {dlc.Name}: {ex.Message}");
                    _logger.Log($"❌ Failed to uninstall {dlc.Name}: {ex.Message}");
                }
            }

            ApplyInstalledFlags();
            UpdateSelectionCount();

            if (errors.Count == 0)
            {
                MessageBox.Show(
                    isSpanish
                        ? $" Se desinstaló exitosamente {uninstalled} DLC(s)."
                        : $" Successfully uninstalled {uninstalled} DLC(s).",
                    isSpanish ? "Desinstalación Completa" : "Uninstallation Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                var errorText = string.Join(Environment.NewLine + " - ", errors);
                MessageBox.Show(
                    isSpanish
                        ? $"Se desinstaló {uninstalled} DLC(s), pero {errors.Count} fallaron:\n\n - {errorText}"
                        : $"Uninstalled {uninstalled} DLC(s), but {errors.Count} failed:\n\n - {errorText}",
                    isSpanish ? "Completado con Errores" : "Completed with Errors",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            UninstallBtn.IsEnabled = true;
            UpdateSelectionCount();
        }

        // ================================================================
        //                    PAUSAR/REANUDAR/CANCELAR
        // ================================================================
        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            lock (_pauseLock)
            {
                _isPaused = !_isPaused;
                IsPaused = _isPaused;

                if (_isPaused)
                {
                    PauseBtn.Content = "▶️  Resume";
                    TaskbarInfo.ProgressState = TaskbarItemProgressState.Error;

                    _logger.Log("⏸️ Download paused by user");
                }
                else
                {
                    PauseBtn.Content = "⏸️  Pause";
                    TaskbarInfo.ProgressState = TaskbarItemProgressState.Paused;

                    _logger.Log("▶️ Download resumed by user");
                }
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();
            string message = isSpanish
                ? "¿Estás seguro de que deseas cancelar todas las descargas?\n\nEsto detendrá el proceso actual."
                : "Are you sure you want to cancel all downloads?\n\nThis will stop the current process.";
            string title = isSpanish ? "Confirmar Cancelación" : "Confirm Cancellation";

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _logger.Log("❌ User cancelled downloads");
                _globalCancellation?.Cancel();

                // RESETEAR BARRA DE TAREAS
                TaskbarInfo.ProgressState = TaskbarItemProgressState.None;
                TaskbarInfo.ProgressValue = 0;

                //  NUEVO: Restaurar botones inmediatamente
                Dispatcher.InvokeAsync(() =>
                {
                    DownloadBtn.Visibility = Visibility.Visible;
                    UninstallBtn.Visibility = Visibility.Visible;
                    PauseBtn.Visibility = Visibility.Collapsed;
                    CancelBtn.Visibility = Visibility.Collapsed;
                    PauseBtn.Content = "⏸️  Pause";

                    SelectAllBtn.IsEnabled = true;
                    DeselectAllBtn.IsEnabled = true;
                    foreach (CheckBox cb in DLCList.Children) cb.IsEnabled = true;
                    UpdateSelectionCount();

                    _isPaused = false;
                    IsPaused = false;
                });
            }
        }

        //  NUEVO: Método para DLCs normales (1 archivo)
        private async Task DownloadSingleDLC(DLCInfo dlc, int currentIndex, int totalCount)
        {
            var tempFile = Path.Combine(_tempFolder, $"{dlc.Id}_{DateTime.Now.Ticks}.zip");

            try
            {
                // Validar URL
                if (string.IsNullOrEmpty(dlc.Url))
                {
                    throw new Exception("URL missing");
                }

                // Descargar con progreso
                _logger.Log($"[{currentIndex}/{totalCount}] Downloading: {dlc.Name}");
                UpdateProgress(dlc.Name, currentIndex, totalCount, 0);

                var downloadSuccess = await _downloadEngine.DownloadWithProgress(
                    dlc.Url,
                    tempFile,
                    dlc.Name,
                    (percent, speedMBps, eta) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            UpdateProgress(dlc.Name, currentIndex, totalCount, percent, speedMBps, eta);
                        });
                    },
                    _globalCancellation.Token
                );

                if (!downloadSuccess.Item1)
                {
                    throw new Exception(downloadSuccess.Item2);
                }

                // Validar ZIP antes de extraer
                if (!IsZipFileValid(tempFile))
                {
                    throw new InvalidDataException($"Downloaded ZIP file is corrupted: {dlc.Name}");
                }

                // Extraer
                _logger.Log($"[{currentIndex}/{totalCount}] Extracting: {dlc.Name}");
                UpdateProgress(dlc.Name, currentIndex, totalCount, 100, null, null, "Extracting");

                var extractSuccess = await Task.Run(() => _extractor.ExtractZip(tempFile, _simsPath));

                if (!extractSuccess.Item1)
                {
                    throw new Exception(extractSuccess.Item2);
                }

                _logger.Log($"[{currentIndex}/{totalCount}]  Completed: {dlc.Name}");
            }
            finally
            {
                // Limpiar archivo temporal
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { }
                }
            }
        }

        //  NUEVO: Método para DLCs multiparte (múltiples archivos)
        private async Task DownloadMultipartDLC(DLCInfo dlc, int currentIndex, int totalCount)
        {
            var tempFiles = new List<string>();

            try
            {
                int totalParts = dlc.Urls.Count;
                long totalBytesDownloaded = 0;
                long totalBytesExpected = 0;

                //  Descargar todas las partes
                for (int partIndex = 0; partIndex < totalParts; partIndex++)
                {
                    // Verificar cancelación
                    if (_globalCancellation.Token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("Download cancelled by user");
                    }

                    string partUrl = dlc.Urls[partIndex];
                    string tempFile = Path.Combine(_tempFolder, $"{dlc.Id}_Part{partIndex + 1}_{DateTime.Now.Ticks}.zip");
                    tempFiles.Add(tempFile);

                    int partNumber = partIndex + 1;
                    _logger.Log($"[{currentIndex}/{totalCount}] Downloading: {dlc.Name} - Part {partNumber}/{totalParts}");

                    // Descargar parte con progreso global
                    var downloadSuccess = await _downloadEngine.DownloadWithProgress(
                        partUrl,
                        tempFile,
                        $"{dlc.Name} - Part {partNumber}/{totalParts}",
                        (percent, speedMBps, eta) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                // Calcular progreso global considerando todas las partes
                                double partWeight = 100.0 / totalParts;
                                double globalPercent = (partIndex * partWeight) + (percent * partWeight / 100.0);

                                UpdateProgress(
                                    $"{dlc.Name} (Part {partNumber}/{totalParts})",
                                    currentIndex,
                                    totalCount,
                                    globalPercent,
                                    speedMBps,
                                    eta
                                );
                            });
                        },
                        _globalCancellation.Token
                    );

                    if (!downloadSuccess.Item1)
                    {
                        throw new Exception($"Failed to download Part {partNumber}: {downloadSuccess.Item2}");
                    }

                    // Validar ZIP de esta parte
                    if (!IsZipFileValid(tempFile))
                    {
                        throw new InvalidDataException($"Corrupted ZIP: {dlc.Name} Part {partNumber}");
                    }

                    _logger.Log($"[{currentIndex}/{totalCount}]  Downloaded: {dlc.Name} - Part {partNumber}/{totalParts}");
                }

                //  Extraer todas las partes en orden
                _logger.Log($"[{currentIndex}/{totalCount}] Extracting all parts: {dlc.Name}");
                UpdateProgress(dlc.Name, currentIndex, totalCount, 100, null, null, "Extracting all parts");

                for (int partIndex = 0; partIndex < totalParts; partIndex++)
                {
                    string tempFile = tempFiles[partIndex];
                    int partNumber = partIndex + 1;

                    _logger.Log($"[{currentIndex}/{totalCount}] Extracting: {dlc.Name} - Part {partNumber}/{totalParts}");

                    var extractSuccess = await Task.Run(() => _extractor.ExtractZip(tempFile, _simsPath));

                    if (!extractSuccess.Item1)
                    {
                        throw new Exception($"Failed to extract Part {partNumber}: {extractSuccess.Item2}");
                    }
                }

                _logger.Log($"[{currentIndex}/{totalCount}]  Completed: {dlc.Name} (All {totalParts} parts installed)");
            }
            catch (OperationCanceledException)
            {
                _logger.Log($"[{currentIndex}/{totalCount}] ❌ Cancelled: {dlc.Name}");
                throw; // Re-lanzar para que el handler principal lo capture
            }
            catch (Exception ex)
            {
                _logger.Log($"[{currentIndex}/{totalCount}] ❌ Failed: {dlc.Name} - {ex.Message}");
                throw;
            }
            finally
            {
                //  Limpiar TODOS los archivos temporales (incluso si se canceló)
                foreach (var tempFile in tempFiles)
                {
                    if (File.Exists(tempFile))
                    {
                        try
                        {
                            File.Delete(tempFile);
                            _logger.Log($"Cleaned temp file: {Path.GetFileName(tempFile)}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to delete temp file {tempFile}: {ex.Message}");
                        }
                    }
                }
            }
        }

        // ================================================================
        //                    ACTUALIZACIÓN DE PROGRESO UI
        // ================================================================
        private void UpdateProgress(string dlcName, int current, int total, double percent,
            double? speedMBps = null, TimeSpan? eta = null, string phase = "Downloading")
        {
            ProgressText.Text = $"{phase} {dlcName}... ({current}/{total})";
            ProgressPercent.Text = $"{percent:F0}%";

            // ACTUALIZAR PROGRESO EN LA BARRA DE TAREAS (Valor entre 0.0 y 1.0)
            TaskbarInfo.ProgressValue = percent / 100.0;

            //  NUEVO: Animar el Kamehameha según el progreso
            if (KamehamehaBeam != null && ProgressPanel.ActualWidth > 0)
            {
                // Calcular ancho máximo disponible (ancho total - espacio de Goku - márgenes)
                double maxWidth = ProgressPanel.ActualWidth - 60; // 55 (margen) + 5 (padding)

                // Calcular ancho del Kamehameha según el porcentaje
                double kamehamehaWidth = (percent / 100.0) * maxWidth;

                // Aplicar ancho mínimo para que se vea algo incluso al 1%
                if (percent > 0 && kamehamehaWidth < 20)
                    kamehamehaWidth = 20;

                KamehamehaBeam.Width = kamehamehaWidth;
            }

            if (speedMBps.HasValue)
                SpeedText.Text = $"{speedMBps.Value:F2} MB/s";

            if (eta.HasValue)
                EtaText.Text = $"{eta.Value:mm\\:ss}";

            InstalledCountText.Text = $"{current}/{total}";
        }

        // ================================================================
        //                    VALIDACIÓN DE ZIP
        // ================================================================
        private bool IsZipFileValid(string zipPath)
        {
            try
            {
                if (!File.Exists(zipPath))
                    return false;

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    var count = archive.Entries.Count;
                    return count > 0;
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ================================================================
        //                    DETECCIÓN DE DLC INSTALADOS
        // ================================================================
        private (bool, string) IsDlcInstalled(DLCInfo dlc)
        {
            if (string.IsNullOrEmpty(_simsPath) || dlc == null || string.IsNullOrWhiteSpace(dlc.Id))
                return (false, "Not Installed");

            try
            {
                string rootDlcFolder = Path.Combine(_simsPath, dlc.Id);
                string installerDlcFolder = Path.Combine(_simsPath, "__Installer", "DLC", dlc.Id);

                bool rootExists = Directory.Exists(rootDlcFolder);
                bool installerExists = Directory.Exists(installerDlcFolder);

                // Si no existe en ninguna carpeta
                if (!rootExists && !installerExists)
                    return (false, "Not Installed");

                // Si existe en una pero no en la otra
                if (rootExists && !installerExists)
                    return (true, "Incomplete");

                if (!rootExists && installerExists)
                    return (false, "Corrupted");

                //  NUEVO: Comparar SOLO los nombres de archivos (sin rutas ni subcarpetas)
                var rootFiles = Directory.GetFiles(rootDlcFolder, "*.*", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileName(f).ToLowerInvariant())
                    .OrderBy(f => f)
                    .ToList();

                var installerFiles = Directory.GetFiles(installerDlcFolder, "*.*", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileName(f).ToLowerInvariant())
                    .OrderBy(f => f)
                    .ToList();

                // Verificar si tienen exactamente los mismos archivos (solo por nombre)
                if (!rootFiles.SequenceEqual(installerFiles))
                    return (true, "Installed");

                return (true, "Installed");
            }
            catch
            {
                return (false, "Error");
            }
        }

        private void ApplyInstalledFlags()
        {
            if (string.IsNullOrEmpty(_simsPath))
                return;

            foreach (CheckBox cb in DLCList.Children)
            {
                var id = cb.Tag as string;
                var dlc = _dlcList.FirstOrDefault(d => d.Id == id);
                if (dlc == null)
                    continue;

                var (isInstalled, status) = IsDlcInstalled(dlc);

                if (status == "Installed")
                {
                    cb.Content = BuildDlcContent(dlc, "Installed");
                    cb.Opacity = 0.6;
                    cb.ToolTip = dlc.Description + "\n\nStatus: Installed.";
                    cb.IsChecked = true;
                    cb.IsEnabled = false; // No permitir descargar si ya está instalado
                }
                else if (status == "Corrupted")
                {
                    cb.Content = BuildDlcContent(dlc, "Corrupted");
                    cb.Opacity = 1.0;
                    cb.ToolTip = dlc.Description + "\n\nStatus: Corrupted - Please Reinstall this DLC to fix it.";
                    cb.IsChecked = false;
                    cb.IsEnabled = true; // Permitir re-descargar
                }
                else if (status == "Incomplete")
                {
                    cb.Content = BuildDlcContent(dlc, "Incomplete");
                    cb.Opacity = 1.0;
                    cb.ToolTip = dlc.Description + "\n\nStatus: Incomplete - Please Reinstall this DLC to fix it.";
                    cb.IsChecked = false;
                    cb.IsEnabled = true; // Permitir re-descargar
                }
                else
                {
                    cb.Content = BuildDlcContent(dlc, "Not Installed");
                    cb.Opacity = 1.0;
                    cb.ToolTip = dlc.Description;
                    cb.IsChecked = false;
                    cb.IsEnabled = true;
                }
            }

            UpdateTotalValue();
        }

        private object BuildDlcContent(DLCInfo dlc, string status)
        {
            var nameText = new TextBlock
            {
                Text = dlc.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (status == "Not Installed")
            {
                return nameText;
            }

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(nameText);

            if (status == "Installed")
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "  • Installed",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                });
            }
            else if (status == "Corrupted")
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "  • Corrupted",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                });
            }
            else if (status == "Incomplete")
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "  • Incomplete",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF9A44")),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                });
            }

            return panel;
        }

        // ================================================================
        //                    HELPERS Y UTILIDADES
        // ================================================================
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

        private object BuildDlcContent(DLCInfo dlc, bool installed)
        {
            var nameText = new TextBlock
            {
                Text = dlc.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!installed)
            {
                return nameText;
            }

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(nameText);
            panel.Children.Add(new TextBlock
            {
                Text = "  • Installed",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            });

            return panel;
        }

        private static bool ShouldLoadDLCImages()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string profilePath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "Profile.ini");

                if (!File.Exists(profilePath))
                    return false;

                var lines = File.ReadAllLines(profilePath);
                bool inMiscSection = false;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (trimmed == "[Misc]")
                    {
                        inMiscSection = true;
                        continue;
                    }

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        inMiscSection = false;
                        continue;
                    }

                    if (inMiscSection && trimmed.StartsWith("LoadDLCImages"))
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length == 2)
                        {
                            var value = parts[1].Trim().ToLower();
                            return value == "true";
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

        private static ImageDownloadWindow _downloadWindow;
        private static int _totalImagesToDownload = 0;
        private static int _imagesDownloaded = 0;
        private static object _downloadLock = new object();

        private static string GetLocalImagePath(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return string.Empty;

                // Directorio de caché de imágenes
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "dlc_images");

                // Crear directorio si no existe
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                // Extraer nombre del archivo de la URL
                string fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                string localPath = Path.Combine(cacheDir, fileName);

                //  SOLO devolver la ruta (la descarga se hace en otro lado)
                return localPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting local image path: {ex.Message}");
                return string.Empty;
            }
        }

        private static async Task CheckAndDownloadMissingImages(bool isSpanish)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "dlc_images");
                string imageBaseUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/";

                // Lista de TODAS las imágenes que deberían existir
                var allImageFileNames = new List<string>
        {
            "EP01.jpg", "EP02.jpg", "EP03.jpg", "EP04.jpg", "EP05.jpg", "EP06.jpg", "EP07.jpg",
            "EP08.jpg", "EP09.jpg", "EP10.jpg", "EP11.jpg", "EP12.jpg", "EP13.jpg", "EP14.jpg",
            "EP15.jpg", "EP16.jpg", "EP17.jpg", "EP18.jpg", "EP19.jpg", "EP20.jpg",
            "EP21.jpg",
            "GP01.jpg", "GP02.jpg", "GP03.jpg", "GP04.jpg", "GP05.jpg", "GP06.jpg",
            "GP07.jpg", "GP08.jpg", "GP09.jpg", "GP10.jpg", "GP11.jpg", "GP12.jpg",
            "SP01.jpg", "SP02.jpg", "SP03.jpg", "SP04.jpg", "SP05.jpg", "SP06.jpg",
            "SP07.jpg", "SP08.jpg", "SP09.jpg", "SP10.jpg", "SP11.jpg", "SP12.jpg",
            "SP13.jpg", "SP14.jpg", "SP15.jpg", "SP16.jpg", "SP17.jpg", "SP18.jpg",
            "SP20.jpg", "SP21.jpg", "SP22.jpg", "SP23.jpg", "SP24.jpg", "SP25.jpg",
            "SP26.jpg", "SP28.jpg", "SP29.jpg", "SP30.jpg", "SP31.jpg", "SP32.jpg",
            "SP33.jpg", "SP34.jpg", "SP35.jpg", "SP36.jpg", "SP37.jpg", "SP38.jpg",
            "SP39.jpg", "SP40.jpg", "SP41.jpg", "SP42.jpg", "SP43.jpg", "SP44.jpg",
            "SP45.jpg", "SP46.jpg", "SP47.jpg", "SP48.jpg", "SP49.jpg", "SP50.jpg",
            "SP51.jpg", "SP52.jpg", "SP53.jpg", "SP54.jpg", "SP55.jpg", "SP56.jpg",
            "SP57.jpg", "SP58.jpg", "SP59.jpg", "SP60.jpg", "SP61.jpg", "SP62.jpg",
            "SP63.jpg", "SP64.jpg", "SP65.jpg", "SP66.jpg", "SP67.jpg", "SP68.jpg",
            "SP69.jpg", "SP70.jpg", "SP71.jpg", "SP72.jpg", "SP73.jpg", "SP74.jpg",
            "SP81.png", "FP01.jpg"
        };

                //  Verificar cuáles imágenes FALTAN
                var missingImages = new List<string>();
                foreach (var fileName in allImageFileNames)
                {
                    string localPath = Path.Combine(cacheDir, fileName);
                    if (!File.Exists(localPath))
                    {
                        missingImages.Add(fileName);
                        Debug.WriteLine($"❌ Missing image: {fileName}");
                    }
                    else
                    {
                        Debug.WriteLine($" Found cached image: {fileName}");
                    }
                }

                //  Si NO faltan imágenes, no hacer nada
                if (missingImages.Count == 0)
                {
                    Debug.WriteLine(" All images are already cached locally. No download needed.");
                    return;
                }

                Debug.WriteLine($"📥 Need to download {missingImages.Count} missing images...");

                //  Mostrar ventana de progreso SOLO si hay imágenes faltantes
                ImageDownloadWindow downloadWindow = null;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    downloadWindow = new ImageDownloadWindow(missingImages.Count, isSpanish);
                    downloadWindow.Show();
                });

                //  Descargar SOLO las imágenes faltantes
                int downloaded = 0;
                await Task.Run(() =>
                {
                    using (var client = new System.Net.WebClient())
                    {
                        foreach (var fileName in missingImages)
                        {
                            try
                            {
                                string imageUrl = imageBaseUrl + fileName;
                                string localPath = Path.Combine(cacheDir, fileName);

                                // Descargar imagen
                                client.DownloadFile(imageUrl, localPath);
                                downloaded++;

                                Debug.WriteLine($" Downloaded: {fileName} ({downloaded}/{missingImages.Count})");

                                // Actualizar ventana de progreso
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    downloadWindow?.UpdateProgress(downloaded, fileName, isSpanish);
                                });
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"❌ Failed to download {fileName}: {ex.Message}");
                            }
                        }
                    }
                });

                // Completar y cerrar ventana
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    downloadWindow?.Complete(isSpanish);
                });

                Debug.WriteLine($" Download complete! {downloaded}/{missingImages.Count} images downloaded.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in CheckAndDownloadMissingImages: {ex.Message}");
            }
        }

        private static void DownloadImageToCache(string imageUrl, string localPath, bool isSpanish)
        {
            try
            {
                //  Si ya existe, no descargar
                if (File.Exists(localPath))
                {
                    Debug.WriteLine($" Skipping download (already exists): {Path.GetFileName(localPath)}");
                    return;
                }

                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(imageUrl, localPath);

                    lock (_downloadLock)
                    {
                        _imagesDownloaded++;
                    }

                    // Actualizar ventana de progreso
                    if (_downloadWindow != null)
                    {
                        _downloadWindow.UpdateProgress(_imagesDownloaded, Path.GetFileName(localPath), isSpanish);
                    }

                    Debug.WriteLine($" Downloaded image: {Path.GetFileName(localPath)} ({_imagesDownloaded}/{_totalImagesToDownload})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to download image {imageUrl}: {ex.Message}");
            }
        }

        private static void SetLoadDLCImages(bool value)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string profilePath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "Profile.ini");

                if (!File.Exists(profilePath))
                    return;

                var lines = File.ReadAllLines(profilePath).ToList();
                bool inMiscSection = false;
                bool keyFound = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    var trimmed = lines[i].Trim();

                    if (trimmed == "[Misc]")
                    {
                        inMiscSection = true;
                        continue;
                    }

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        if (inMiscSection && !keyFound)
                        {
                            lines.Insert(i, $"LoadDLCImages = {value.ToString().ToLower()}");
                            keyFound = true;
                        }
                        inMiscSection = false;
                        continue;
                    }

                    if (inMiscSection && trimmed.StartsWith("LoadDLCImages"))
                    {
                        lines[i] = $"LoadDLCImages = {value.ToString().ToLower()}";
                        keyFound = true;
                        break;
                    }
                }

                if (inMiscSection && !keyFound)
                {
                    lines.Add($"LoadDLCImages = {value.ToString().ToLower()}");
                }

                File.WriteAllLines(profilePath, lines);
            }
            catch { }
        }

        private void WriteGameDirToProfile(string gameDir)
        {
            try
            {
                var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var profileFolder = Path.Combine(appDataRoaming, "Leuan's - Sims 4 ToolKit");
                var profilePath = Path.Combine(profileFolder, "Profile.ini");

                if (!Directory.Exists(profileFolder))
                {
                    Directory.CreateDirectory(profileFolder);
                }

                List<string> lines = new List<string>();
                bool gameDirSectionExists = false;
                bool gameDirKeyExists = false;

                if (File.Exists(profilePath))
                {
                    lines = File.ReadAllLines(profilePath).ToList();

                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].Trim() == "[GameDir]")
                        {
                            gameDirSectionExists = true;

                            for (int j = i + 1; j < lines.Count; j++)
                            {
                                if (lines[j].Trim().StartsWith("["))
                                    break;

                                if (lines[j].Trim().StartsWith("GameDir="))
                                {
                                    lines[j] = $"GameDir={gameDir}";
                                    gameDirKeyExists = true;
                                    break;
                                }
                            }

                            if (!gameDirKeyExists)
                            {
                                lines.Insert(i + 1, $"GameDir={gameDir}");
                                gameDirKeyExists = true;
                            }
                            break;
                        }
                    }
                }

                if (!gameDirSectionExists)
                {
                    lines.Add("");
                    lines.Add("[GameDir]");
                    lines.Add($"GameDir={gameDir}");
                }

                File.WriteAllLines(profilePath, lines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing GameDir to Profile.ini: {ex.Message}");
            }
        }
        private void UpdateTotalValue()
        {
            bool isSpanish = IsSpanishLanguage();

            if (string.IsNullOrEmpty(_simsPath))
            {
                TotalValueText.Text = isSpanish
                    ? "💰 Tu juego vale: $0.00 USD"
                    : "💰 Your game is worth: $0.00 USD";
                return;
            }

            decimal totalValue = _dlcList
                .Where(dlc => IsDlcInstalled(dlc).Item1) //  Usar .Item1 para obtener el bool
                .Sum(dlc => dlc.Price);

            TotalValueText.Text = isSpanish
                ? $"💰 Tu juego vale: ${totalValue:F2} USD"
                : $"💰 Your game is worth: ${totalValue:F2} USD";
        }

        private void UpdateUnlockerStatus()
        {
            try
            {
                if (UnlockerService.IsUnlockerInstalled(out var clientName))
                {
                    UnlockerStatusText.Text = $"DLC Unlocker: Installed ({clientName})";
                    UnlockerStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                }
                else
                {
                    UnlockerStatusText.Text = "DLC Unlocker: Not installed";
                    UnlockerStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F97373"));
                }
            }
            catch
            {
                UnlockerStatusText.Text = "DLC Unlocker: Status unknown";
                UnlockerStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
            }
        }

        // ================================================================
        //                    POBLACIÓN DE DLC LIST
        // ================================================================
        private void PopulateDLCList()
        {
            foreach (var dlc in _dlcList)
            {
                var checkBox = new CheckBox
                {
                    Content = BuildDlcContent(dlc, installed: false),
                    ToolTip = dlc.Description,
                    Tag = dlc.Id,
                    DataContext = dlc,
                    Style = (Style)FindResource("DLCCheckBox"),
                    IsEnabled = false
                };
                checkBox.Checked += (s, e) => UpdateSelectionCount();
                checkBox.Unchecked += (s, e) => UpdateSelectionCount();
                DLCList.Children.Add(checkBox);
                _allCheckBoxes.Add(checkBox);
            }
        }

        private void UpdateSelectionCount()
        {
            var selectedCheckboxes = DLCList.Children.OfType<CheckBox>().Where(cb => cb.IsChecked == true).ToList();
            int count = selectedCheckboxes.Count;
            CountText.Text = $" ({count} selected)";

            if (string.IsNullOrEmpty(_simsPath))
            {
                DownloadBtn.IsEnabled = false;
                UninstallBtn.IsEnabled = false;
                return;
            }

            // Habilitar Descarga: Si hay alguno seleccionado que NO esté "Installed"
            bool hasUninstalledSelected = selectedCheckboxes.Any(cb => {
                var dlc = cb.DataContext as DLCInfo;
                return IsDlcInstalled(dlc).Item2 != "Installed";
            });

            // Habilitar Desinstalación: Si hay alguno seleccionado que sea Installed, Corrupted o Incomplete
            bool hasRemovableSelected = selectedCheckboxes.Any(cb => {
                var dlc = cb.DataContext as DLCInfo;
                string status = IsDlcInstalled(dlc).Item2;
                return status == "Installed" || status == "Corrupted" || status == "Incomplete";
            });

            DownloadBtn.IsEnabled = hasUninstalledSelected;
            UninstallBtn.IsEnabled = hasRemovableSelected;
        }

        // ================================================================
        //                    EVENT HANDLERS
        // ================================================================
        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower().Trim();

            SearchPlaceholder.Visibility = string.IsNullOrEmpty(searchText)
                ? Visibility.Visible
                : Visibility.Collapsed;

            foreach (var checkBox in _allCheckBoxes)
            {
                var dlc = checkBox.DataContext as DLCInfo;
                if (dlc == null)
                {
                    checkBox.Visibility = Visibility.Visible;
                    continue;
                }

                bool matches = string.IsNullOrEmpty(searchText) ||
                               dlc.Name.ToLower().Contains(searchText) ||
                               dlc.Description.ToLower().Contains(searchText) ||
                               dlc.Id.ToLower().Contains(searchText);

                checkBox.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select The Sims 4 install folder",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var exePath = Path.Combine(dialog.SelectedPath, "Game", "Bin", "TS4_x64.exe");
                if (File.Exists(exePath) || Directory.Exists(Path.Combine(dialog.SelectedPath, "Data")))
                {
                    SetSimsPath(dialog.SelectedPath);
                }
                else
                {
                    MessageBox.Show(
                        "The selected folder does not look like a valid The Sims 4 installation.\n\n" +
                        "Please select the folder that contains the 'Game' and 'Data' subfolders.",
                        "Invalid path",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private async void AutoDetectBtn_Click(object sender, RoutedEventArgs e)
        {
            AutoDetectBtn.IsEnabled = false;

            await Task.Run(() =>
            {
                try
                {
                    //  Leer desde el registro (igual que el .bat)
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Maxis\The Sims 4"))
                    {
                        if (key != null)
                        {
                            var installDir = key.GetValue("Install Dir") as string;

                            if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    SetSimsPath(installDir, true);

                                    //  Abrir carpeta en el explorador (como hace el .bat)
                                    try
                                    {
                                        Process.Start("explorer.exe", installDir);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Failed to open folder: {ex.Message}");
                                    }
                                });
                                return;
                            }
                        }
                    }

                    // Si no se encontró en el registro
                    Dispatcher.Invoke(() =>
                    {
                        bool isSpanish = IsSpanishLanguage();
                        string message = isSpanish
                            ? "No se pudo encontrar The Sims 4 en el registro de Windows.\n\n" +
                              "Por favor, selecciona la carpeta manualmente usando el botón 'Browse'."
                            : "Could not find The Sims 4 in Windows Registry.\n\n" +
                              "Please select the folder manually using the 'Browse' button.";
                        string title = isSpanish ? "No Encontrado" : "Not Found";

                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

                        StatusText.Text = "  (Not found - select manually)";
                        StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading registry: {ex.Message}");

                    Dispatcher.Invoke(() =>
                    {
                        bool isSpanish = IsSpanishLanguage();
                        string message = isSpanish
                            ? $"Error al leer el registro:\n\n{ex.Message}"
                            : $"Error reading registry:\n\n{ex.Message}";
                        string title = isSpanish ? "Error" : "Error";

                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });

            AutoDetectBtn.IsEnabled = true;
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in DLCList.Children)
            {
                if (cb.Visibility == Visibility.Visible)
                {
                    cb.IsChecked = true;
                }
            }
        }

        private void DeselectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in DLCList.Children)
            {
                if (cb.Visibility == Visibility.Visible)
                {
                    cb.IsChecked = false;
                }
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var dlc = fe?.DataContext as DLCInfo;
            if (dlc == null)
                return;

            var infoWindow = new DlcInfoWindow(dlc)
            {
                Owner = this
            };

            infoWindow.ShowDialog();
        }

        private void TutorialBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tutorialWindow = new TutorialMainWindow();
                tutorialWindow.Owner = this;
                tutorialWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                tutorialWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                bool isSpanish = IsSpanishLanguage();
                MessageBox.Show(
                    isSpanish
                        ? $"No se pudo abrir el tutorial: {ex.Message}"
                        : $"Could not open tutorial: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow == null || mainWindow == this)
                {
                    this.Close();
                    return;
                }

                var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };

                fadeOut.Completed += (s, args) =>
                {
                    try
                    {
                        this.Hide();
                        mainWindow.Opacity = 0;
                        mainWindow.Show();

                        var fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(200) };
                        fadeIn.Completed += (s2, args2) =>
                        {
                            try { this.Close(); } catch { }
                        };
                        mainWindow.BeginAnimation(Window.OpacityProperty, fadeIn);
                    }
                    catch
                    {
                        try { this.Close(); } catch { }
                    }
                };

                this.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
            catch
            {
                try { this.Close(); } catch { }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow == null || mainWindow == this)
                {
                    this.Close();
                    return;
                }

                var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };

                fadeOut.Completed += (s, args) =>
                {
                    try
                    {
                        this.Hide();
                        mainWindow.Opacity = 0;
                        mainWindow.Show();

                        var fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(200) };
                        fadeIn.Completed += (s2, args2) =>
                        {
                            try { this.Close(); } catch { }
                        };
                        mainWindow.BeginAnimation(Window.OpacityProperty, fadeIn);
                    }
                    catch
                    {
                        try { this.Close(); } catch { }
                    }
                };

                this.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
            catch
            {
                try { this.Close(); } catch { }
            }
        }

        private static async Task DownloadAllImagesWithProgress(List<string> imageUrls, bool isSpanish)
        {
            if (imageUrls.Count == 0)
                return;

            // Mostrar ventana de progreso
            _downloadWindow = new ImageDownloadWindow(imageUrls.Count, isSpanish);
            _downloadWindow.Show();

            await Task.Run(() =>
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "dlc_images");
                string imageBaseUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/";

                foreach (var imageUrl in imageUrls)
                {
                    if (string.IsNullOrEmpty(imageUrl))
                        continue;

                    string fileName = Path.GetFileName(new Uri(imageBaseUrl + imageUrl).LocalPath);
                    string localPath = Path.Combine(cacheDir, fileName);

                    DownloadImageToCache(imageBaseUrl + imageUrl, localPath, isSpanish);
                }
            });

            // Completar y cerrar ventana
            _downloadWindow.Complete(isSpanish);
        }

        // ================================================================
        //                    DLC LIST (MANTENER TU LISTA ORIGINAL)
        // ================================================================
        public static List<DLCInfo> GetDLCList()
        {
            // Si ya cargamos la lista, no la volvemos a descargar
            if (_dlcListCache != null)
                return _dlcListCache;

            bool shouldLoadImages = ShouldLoadDLCImages();
            string imageBaseUrl = shouldLoadImages
                ? "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/"
                : string.Empty;

            try
            {
                // Descargar el JSON desde GitHub
                using (var client = new WebClient())
                {
                    var json = client.DownloadString(DLC_DATABASE_URL);
                    var database = Newtonsoft.Json.JsonConvert.DeserializeObject<DLCDatabase>(json);

                    var dlcList = new List<DLCInfo>();

                    foreach (var entry in database.dlcs)
                    {
                        // Si tiene 1 URL (DLC normal)
                        if (entry.urls.Count == 1)
                        {
                            dlcList.Add(new DLCInfo(
                                entry.id,
                                entry.name,
                                entry.description,
                                entry.urls[0],
                                shouldLoadImages ? GetLocalImagePath(imageBaseUrl + entry.imageFileName) : string.Empty,
                                entry.isOfflineMode,
                                entry.price
                            ));
                        }
                        // Si tiene múltiples URLs (DLC multiparte)
                        else if (entry.urls.Count > 1)
                        {
                            dlcList.Add(new DLCInfo(
                                entry.id,
                                entry.name,
                                entry.description,
                                entry.urls,
                                shouldLoadImages ? GetLocalImagePath(imageBaseUrl + entry.imageFileName) : string.Empty,
                                entry.isOfflineMode,
                                entry.price
                            ));
                        }
                    }

                    _dlcListCache = dlcList;
                    return dlcList;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading DLC database: {ex.Message}");

                // Fallback: retornar lista vacía
                return new List<DLCInfo>();
            }
        }

    }

    // ================================================================
    //                    CLASES AUXILIARES
    // ================================================================
    public class DLCInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Soporta múltiples URLs para DLCs multiparte
        public List<string> Urls { get; set; }
        public string Url { get; set; } // Mantener para compatibilidad con DLCs normales

        public string ImageUrl { get; set; }
        public string WikiUrl { get; set; } 
        public bool IsOfflineMode { get; set; }
        public decimal Price { get; set; }

        // Constructor para DLCs normales (1 URL)
        public DLCInfo(string id, string name, string desc, string url, string img, bool offline, decimal price)
        {
            Id = id;
            Name = name;
            Description = desc;
            Url = url;
            Urls = null;
            ImageUrl = img;
            WikiUrl = null;
            IsOfflineMode = offline;
            Price = price;
        }

        // Constructor para DLCs normales
        public DLCInfo(string id, string name, string desc, string url, string img, string wikiUrl, bool offline, decimal price)
        {
            Id = id;
            Name = name;
            Description = desc;
            Url = url;
            Urls = null;
            ImageUrl = img;
            WikiUrl = wikiUrl; 
            IsOfflineMode = offline;
            Price = price;
        }

        //  Constructor para DLCs multiparte (múltiples URLs)
        public DLCInfo(string id, string name, string desc, List<string> urls, string img, bool offline, decimal price)
        {
            Id = id;
            Name = name;
            Description = desc;
            Url = null;
            Urls = urls;
            ImageUrl = img;
            WikiUrl = null; 
            IsOfflineMode = offline;
            Price = price;
        }

        // Constructor para DLCs multiparte CON WikiUrl
        public DLCInfo(string id, string name, string desc, List<string> urls, string img, string wikiUrl, bool offline, decimal price)
        {
            Id = id;
            Name = name;
            Description = desc;
            Url = null;
            Urls = urls;
            ImageUrl = img;
            WikiUrl = wikiUrl; 
            IsOfflineMode = offline;
            Price = price;
        }

        // Helper para saber si es multiparte
        public bool IsMultipart => Urls != null && Urls.Count > 1;

        // Obtener URL principal (para compatibilidad)
        public string GetPrimaryUrl()
        {
            if (IsMultipart)
                return Urls[0];
            return Url;
        }
    }
}

// ================================================================
//                    THREAD MANAGER
// ================================================================
public class ThreadManager
{
    public bool IsDownloading { get; set; }
    private List<Task> _activeTasks = new List<Task>();

    public void AddTask(Task task)
    {
        _activeTasks.Add(task);
    }

    public void CleanupTemporaryFiles(string tempFolder)
    {
        try
        {
            if (!Directory.Exists(tempFolder))
                return;

            var patterns = new[] { "*.tmp", "*.part", "*.download" };
            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.GetFiles(tempFolder, pattern))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }
}

// ================================================================
//                    DOWNLOAD ENGINE
// ================================================================
public class DownloadEngine
{
    private readonly Logger _logger;
    private readonly HttpClient _httpClient;

    public DownloadEngine(Logger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Leuans-Sims4-Toolkit/4.0");
    }

    public async Task<(bool, string)> DownloadWithProgress(
        string url,
        string outputPath,
        string dlcName,
        Action<double, double?, TimeSpan?> progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log($"Starting download: {dlcName}");

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;
                var buffer = new byte[8192];
                var stopwatch = Stopwatch.StartNew();

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        //  NUEVO: Verificar si está pausado
                        while (UpdaterWindow.IsPaused)
                        {
                            await Task.Delay(100, cancellationToken);
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        downloadedBytes += bytesRead;

                        if (stopwatch.ElapsedMilliseconds >= 500)
                        {
                            var percent = totalBytes > 0 ? (downloadedBytes * 100.0 / totalBytes) : 0;
                            var speedMBps = (downloadedBytes / (1024.0 * 1024.0)) / stopwatch.Elapsed.TotalSeconds;
                            var remainingBytes = totalBytes - downloadedBytes;
                            var eta = speedMBps > 0 ? TimeSpan.FromSeconds(remainingBytes / (speedMBps * 1024 * 1024)) : (TimeSpan?)null;

                            progressCallback?.Invoke(percent, speedMBps, eta);
                            stopwatch.Restart();
                        }
                    }
                }

                _logger.Log($" Download completed: {dlcName}");
                return (true, "OK");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"Download cancelled: {dlcName}");
            return (false, "Cancelled");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ Download failed: {dlcName} - {ex.Message}");
            return (false, ex.Message);
        }
    }
}

// ================================================================
//                    EXTRACTOR
// ================================================================
public class Extractor
{
    private readonly Logger _logger;

    public Extractor(Logger logger)
    {
        _logger = logger;
    }

    public (bool, string) ExtractZip(string zipPath, string destinationPath)
    {
        try
        {
            _logger.Log($"Extracting: {Path.GetFileName(zipPath)}");

            if (!File.Exists(zipPath))
                return (false, "ZIP file not found");

            Directory.CreateDirectory(destinationPath);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var destPath = Path.Combine(destinationPath, entry.FullName);
                    var destDir = Path.GetDirectoryName(destPath);

                    Directory.CreateDirectory(destDir);

                    try
                    {
                        entry.ExtractToFile(destPath, overwrite: true);
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                        entry.ExtractToFile(destPath, overwrite: true);
                    }
                }
            }

            _logger.Log($" Extraction completed");
            return (true, "OK");
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ Extraction failed: {ex.Message}");
            return (false, ex.Message);
        }
    }
}

// ================================================================
//                    LOGGER
// ================================================================
public class Logger
{
    private readonly TextBox _textBox;

    public Logger(TextBox textBox)
    {
        _textBox = textBox;
    }

    public void Log(string message)
    {
        if (_textBox == null)
            return;

        _textBox.Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _textBox.AppendText($"[{timestamp}] {message}\n");
            _textBox.ScrollToEnd();
        });
    }
}

// ================================================================
//                    NETWORK CHECKER
// ================================================================
public static class NetworkChecker
{
    public static bool IsOnline()
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                client.Connect("8.8.8.8", 53);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}

// ================================================================
//                    DISK CHECKER
// ================================================================
public static class DiskChecker
{
    public static (bool, long) CheckDiskSpace(string path, long requiredGB)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(path));
            var freeGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            return (freeGB >= requiredGB, freeGB);
        }
        catch
        {
            return (true, 0);
        }
    }
}

// ================================================================
//                    7ZIP FINDER
// ================================================================
public class SevenZipFinder
{
    private readonly Logger _logger;

    public SevenZipFinder(Logger logger)
    {
        _logger = logger;
    }

    public string Find()
    {
        var locations = new[]
        {
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe"
        };

        foreach (var loc in locations)
        {
            if (File.Exists(loc))
            {
                _logger.Log($"Found 7-Zip at: {loc}");
                return loc;
            }
        }

        _logger.Log("7-Zip not found");
        return null;
    }
}

// ================================================================
//                    DOWNLOAD QUEUE
// ================================================================
public class DownloadQueue
{
    private readonly int _maxConcurrent;
    private readonly Logger _logger;

    public DownloadQueue(int maxConcurrent, Logger logger)
    {
        _maxConcurrent = maxConcurrent;
        _logger = logger;
    }
}

// ================================================================
//                    DLC DATABASE CLASSES
// ================================================================
public class DLCDatabase
{
    public List<DLCEntry> dlcs { get; set; }
}

public class DLCEntry
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public List<string> urls { get; set; }
    public string imageFileName { get; set; }
    public decimal price { get; set; }
    public bool isOfflineMode { get; set; }
}