using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using System.Web.Script.Serialization;
using System.IO.Compression;

namespace ModernDesign.MVVM.View
{
    public partial class RepairLoggerWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly HttpClient _httpClient = new HttpClient();
        private string _simsPath = "";
        private Dictionary<string, string> _officialHashes = new Dictionary<string, string>();
        private HashSet<string> _excludedFolders = new HashSet<string>();
        private bool _scanConfigured = false;
        private bool _includeCrackedFiles = false;
        private List<string> _corruptedCrackedFiles = new List<string>();

        private const string OFFICIAL_DATABASE_URL = "https://raw.githubusercontent.com/Leuansin/Leuans-sims4-toolkit/refs/heads/main/SHA256-Getter/leuan_steam_database.json";

        private int _totalFiles = 0;
        private int _scannedFiles = 0;
        private int _correctFiles = 0;
        private int _corruptFiles = 0;

        public RepairLoggerWindow()
        {
            InitializeComponent();
            ApplyLanguage();
            Loaded += RepairLoggerWindow_Loaded;
        }

        private void MainBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                try
                {
                    this.DragMove();
                }
                catch { }
            }
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderText.Text = "🔍 Verificador de Integridad del Juego";
                SubHeaderText.Text = "Compara archivos con la base de datos oficial de Steam";
                PathLabelText.Text = "Ubicación de The Sims 4";
                BrowseBtn.Content = "Buscar";
                CancelBtn.Content = "❌ Cancelar";
                StartBtn.Content = "🔍 Iniciar Escaneo";
                SpeedLabel.Text = "Velocidad:";
                EtaLabel.Text = "ETA:";
                ConfigureBtn.Content = "⚙️ Configurar Escaneo (Seleccionar DLCs)";
                IncludeCrackedCheckBox.Content = "Incluir archivos Game-Cracked en el escaneo";

            }
            else
            {
                HeaderText.Text = "🔍 Game Integrity Checker";
                SubHeaderText.Text = "Compare files with official Steam database";
                PathLabelText.Text = "The Sims 4 install location";
                BrowseBtn.Content = "Browse";
                CancelBtn.Content = "❌ Cancel";
                StartBtn.Content = "🔍 Start Scan";
                SpeedLabel.Text = "Speed:";
                EtaLabel.Text = "ETA:";
                ConfigureBtn.Content = "⚙️ Configure Scan (Select DLCs)";
                IncludeCrackedCheckBox.Content = "Include Game-Cracked files in scan";

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

        private static string GetLocalImagePath(string dlcId)
        {
            try
            {
                if (string.IsNullOrEmpty(dlcId))
                    return string.Empty;

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "dlc_images");

                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                // Determinar extensión (.jpg o .png)
                string extension = dlcId == "SP81" ? ".png" : ".jpg";
                string fileName = dlcId + extension;
                string localPath = Path.Combine(cacheDir, fileName);

                return localPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting local image path: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task CheckAndDownloadMissingImages(bool isSpanish)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "dlc_images");
                string imageBaseUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/";

                var allImageFileNames = new List<string>
        {
            "EP01.jpg", "EP02.jpg", "EP03.jpg", "EP04.jpg", "EP05.jpg", "EP06.jpg", "EP07.jpg",
            "EP08.jpg", "EP09.jpg", "EP10.jpg", "EP11.jpg", "EP12.jpg", "EP13.jpg", "EP14.jpg",
            "EP15.jpg", "EP16.jpg", "EP17.jpg", "EP18.jpg", "EP19.jpg", "EP20.jpg",
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

                var missingImages = new List<string>();
                foreach (var fileName in allImageFileNames)
                {
                    string localPath = Path.Combine(cacheDir, fileName);
                    if (!File.Exists(localPath))
                    {
                        missingImages.Add(fileName);
                    }
                }

                if (missingImages.Count == 0)
                {
                    Debug.WriteLine(" All DLC images are cached locally.");
                    return;
                }

                Debug.WriteLine($"📥 Need to download {missingImages.Count} missing DLC images...");

                ImageDownloadWindow downloadWindow = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    downloadWindow = new ImageDownloadWindow(missingImages.Count, isSpanish);
                    downloadWindow.Owner = this;
                    downloadWindow.Show();
                });

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

                                client.DownloadFile(imageUrl, localPath);
                                downloaded++;

                                Dispatcher.Invoke(() =>
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

                await Dispatcher.InvokeAsync(() =>
                {
                    downloadWindow?.Complete(isSpanish);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in CheckAndDownloadMissingImages: {ex.Message}");
            }
        }
        private async void RepairLoggerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();
            StatusText.Text = isSpanish ? "  (Buscando automáticamente...)" : "  (Searching automatically...)";

            await AutoDetectSimsPath();

            //  NUEVO: Verificar si debe cargar imágenes de DLC
            if (ShouldLoadDLCImages())
            {
                await CheckAndDownloadMissingImages(isSpanish);
            }
        }

        private async Task AutoDetectSimsPath()
        {
            bool isSpanish = IsSpanishLanguage();

            await Task.Run(() =>
            {
                var commonPaths = new[]
                {
                    @"C:\Program Files\EA Games\The Sims 4",
                    @"C:\Program Files (x86)\EA Games\The Sims 4",
                    @"C:\Program Files\Origin Games\The Sims 4",
                    @"C:\Program Files (x86)\Origin Games\The Sims 4",
                    @"C:\Program Files (x86)\Steam\steamapps\common\The Sims 4",
                    @"D:\Games\The Sims 4",
                    @"D:\SteamLibrary\Steam\steamapps\common\The Sims 4",
                    @"D:\Origin Games\The Sims 4",
                    @"D:\Steam\steamapps\common\The Sims 4",
                    @"D:\The Sims 4",
                    @"E:\Games\The Sims 4",
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

                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = isSpanish
                        ? "  (No encontrado - seleccionar manualmente)"
                        : "  (Not found - select manually)";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));

                    AddLog(isSpanish
                        ? "⚠️ No se pudo detectar automáticamente la carpeta de The Sims 4."
                        : "⚠️ Could not auto-detect The Sims 4 folder.");
                    AddLog(isSpanish
                        ? "Por favor, selecciónala manualmente usando el botón 'Buscar'."
                        : "Please select it manually using the 'Browse' button.");
                });
            });
        }

        private void SetSimsPath(string path, bool autoDetected = false)
        {
            bool isSpanish = IsSpanishLanguage();
            _simsPath = path;
            PathTextBlock.Text = path;
            PathTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC"));

            StatusText.Text = autoDetected
                ? (isSpanish ? "  (✓ Auto-detectado)" : "  (✓ Auto-detected)")
                : (isSpanish ? "  (✓ Seleccionado)" : "  (✓ Selected)");
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E"));

            // Mostrar botón de configuración
            ConfigureBtn.Visibility = Visibility.Visible;

            // Deshabilitar StartBtn hasta que se configure
            StartBtn.IsEnabled = false;

            AddLog(isSpanish
                ? $" Carpeta de The Sims 4 detectada: {path}"
                : $" The Sims 4 folder detected: {path}");

            AddLog(isSpanish
                ? "⚠️ Debes configurar el escaneo antes de iniciar."
                : "⚠️ You must configure the scan before starting.");

            // Show the cracked files checkbox
            IncludeCrackedCheckBox.Visibility = Visibility.Visible;
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = isSpanish
                    ? "Selecciona la carpeta de instalación de The Sims 4"
                    : "Select The Sims 4 install folder",
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
                        isSpanish
                            ? "La carpeta seleccionada no parece ser una instalación válida de The Sims 4.\n\n" +
                              "Por favor selecciona la carpeta que contiene las subcarpetas 'Game' y 'Data'."
                            : "The selected folder does not look like a valid The Sims 4 installation.\n\n" +
                              "Please select the folder that contains the 'Game' and 'Data' subfolders.",
                        isSpanish ? "Ruta inválida" : "Invalid path",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void ConfigureBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectorWindow = new DLCSelectorWindow
            {
                Owner = this
            };

            if (selectorWindow.ShowDialog() == true)
            {
                _excludedFolders = selectorWindow.ExcludedFolders;
                _scanConfigured = true;
                StartBtn.IsEnabled = true;

                bool isSpanish = IsSpanishLanguage();
                int excludedCount = _excludedFolders.Count;

                AddLog(isSpanish
                    ? $" Configuración guardada. {excludedCount} carpeta(s) excluida(s) del escaneo."
                    : $" Configuration saved. {excludedCount} folder(s) excluded from scan.");

                ConfigureBtn.Content = isSpanish
                    ? $"⚙️ Reconfigurar Escaneo ({excludedCount} excluidas)"
                    : $"⚙️ Reconfigure Scan ({excludedCount} excluded)";
            }
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_simsPath))
                return;

            StartBtn.IsEnabled = false;
            BrowseBtn.IsEnabled = false;
            ConfigureBtn.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _includeCrackedFiles = IncludeCrackedCheckBox.IsChecked == true;

                await StartIntegrityCheckAsync();
            }
            catch (OperationCanceledException)
            {
                bool isSpanish = IsSpanishLanguage();
                AddLog(isSpanish ? "❌ Escaneo cancelado por el usuario." : "❌ Scan cancelled by user.");
            }
            catch (Exception ex)
            {
                bool isSpanish = IsSpanishLanguage();
                AddLog(isSpanish ? $"❌ Error: {ex.Message}" : $"❌ Error: {ex.Message}");
                MessageBox.Show(
                    isSpanish
                        ? $"Error durante el escaneo:\n\n{ex.Message}"
                        : $"Error during scan:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                StartBtn.IsEnabled = _scanConfigured;
                BrowseBtn.IsEnabled = true;
                ConfigureBtn.IsEnabled = true;
                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async Task StartIntegrityCheckAsync()
        {
            bool isSpanish = IsSpanishLanguage();

            if (!_scanConfigured)
            {
                MessageBox.Show(
                    isSpanish
                        ? "Debes configurar el escaneo primero usando el botón de configuración."
                        : "You must configure the scan first using the configuration button.",
                    isSpanish ? "Configuración Requerida" : "Configuration Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            AddLog(isSpanish
                ? "\n🔍 Iniciando verificación de integridad..."
                : "\n🔍 Starting integrity check...");

            // Step 1: Download official database
            AddLog(isSpanish
                ? "📥 Descargando base de datos oficial de Steam..."
                : "📥 Downloading official Steam database...");

            await DownloadOfficialDatabaseAsync();

            AddLog(isSpanish
                ? $"✅ Base de datos descargada. Total de archivos oficiales: {_officialHashes.Count}"
                : $"✅ Database downloaded. Total official files: {_officialHashes.Count}");

            // Step 2: Prepare verification
            AddLog(isSpanish
                ? "\n🔎 Preparando verificación contra base de datos oficial..."
                : "\n🔎 Preparing verification against official database...");

            var corruptedFiles = new List<string>();
            _corruptedCrackedFiles.Clear();
            var sw = Stopwatch.StartNew();

            // Step 3: Verify each file from official database
            var missingFiles = new List<string>();

            // Filtrar archivos de la base de datos según carpetas seleccionadas
            var filteredOfficialHashes = _officialHashes
                .Where(kvp =>
                {
                    var parts = kvp.Key.Split('/');
                    if (parts.Length == 0) return false;

                    var rootFolder = parts[0];

                    // Si está en la lista de excluidos, no verificar
                    // NUEVO: Si es Game-Cracked, solo verificar si está habilitado
                    if (rootFolder == "Game-Cracked")
                    {
                        return _includeCrackedFiles;
                    }

                    return !_excludedFolders.Contains(rootFolder);
                })
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            _totalFiles = filteredOfficialHashes.Count;
            _scannedFiles = 0;
            _correctFiles = 0;
            _corruptFiles = 0;

            AddLog(isSpanish
                ? $"📊 Total de archivos a verificar (según base de datos): {_totalFiles}"
                : $"📊 Total files to verify (from database): {_totalFiles}");

            foreach (var officialEntry in filteredOfficialHashes)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                var normalizedPath = officialEntry.Key;
                var officialHash = officialEntry.Value;
                var localPath = Path.Combine(_simsPath, normalizedPath.Replace("/", "\\"));

                UpdateCurrentFile(normalizedPath);
                _scannedFiles++;

                // Verificar si el archivo existe localmente
                if (!File.Exists(localPath))
                {
                    _corruptFiles++;
                    missingFiles.Add(normalizedPath);

                    // NUEVO: Separar archivos corruptos de Game-Cracked
                    if (normalizedPath.StartsWith("Game-Cracked/"))
                    {
                        _corruptedCrackedFiles.Add(normalizedPath);
                    }
                    else
                    {
                        corruptedFiles.Add(normalizedPath);
                    }

                    AddLog(isSpanish
                        ? $"❌ {normalizedPath} | FALTA ARCHIVO"
                        : $"❌ {normalizedPath} | MISSING FILE");
                }
                else
                {
                    // El archivo existe, verificar hash
                    var localHash = await Task.Run(() => CalculateSHA256(localPath));

                    if (localHash.Equals(officialHash, StringComparison.OrdinalIgnoreCase))
                    {
                        _correctFiles++;
                        AddLog(isSpanish
                            ? $"✅ {normalizedPath}"
                            : $"✅ {normalizedPath}");
                    }
                    else
                    {
                        _corruptFiles++;

                        // NUEVO: Separar archivos corruptos de Game-Cracked
                        if (normalizedPath.StartsWith("Game-Cracked/"))
                        {
                            _corruptedCrackedFiles.Add(normalizedPath);
                        }
                        else
                        {
                            corruptedFiles.Add(normalizedPath);
                        }

                        AddLog(isSpanish
                            ? $"❌ {normalizedPath} | HASH INCORRECTO"
                            : $"❌ {normalizedPath} | INCORRECT HASH");
                    }
                }

                UpdateProgress(_scannedFiles, _totalFiles);
                UpdateStats();
            }

            if (missingFiles.Count > 0)
            {
                AddLog(isSpanish
                    ? $"\n⚠️ Se detectaron {missingFiles.Count} archivo(s) faltante(s)"
                    : $"\n⚠️ Detected {missingFiles.Count} missing file(s)");
            }

            sw.Stop();

            // Step 4: Summary
            AddLog(isSpanish
                ? $"\n📊 Escaneo completado en {sw.Elapsed.TotalSeconds:F2} segundos"
                : $"\n📊 Scan completed in {sw.Elapsed.TotalSeconds:F2} seconds");
            AddLog(isSpanish
                ? $"   Total: {_totalFiles} | Correctos: {_correctFiles} | Corruptos: {_corruptFiles}"
                : $"   Total: {_totalFiles} | Valid: {_correctFiles} | Corrupt: {_corruptFiles}");

            // NUEVO: Mostrar estadísticas de Game-Cracked si se escaneó
            if (_includeCrackedFiles && _corruptedCrackedFiles.Count > 0)
            {
                AddLog(isSpanish
                    ? $"   Game-Cracked corruptos: {_corruptedCrackedFiles.Count}"
                    : $"   Game-Cracked corrupt: {_corruptedCrackedFiles.Count}");
            }

            // Step 5: Repair corrupted files
            int totalCorrupt = corruptedFiles.Count + _corruptedCrackedFiles.Count;

            if (totalCorrupt > 0)
            {
                var result = MessageBox.Show(
                    isSpanish
                        ? $"Se encontraron {totalCorrupt} archivo(s) corrupto(s).\n\n¿Deseas repararlos automáticamente descargándolos desde el servidor oficial?"
                        : $"Found {totalCorrupt} corrupt file(s).\n\nDo you want to automatically repair them by downloading from the official server?",
                    isSpanish ? "Archivos Corruptos Detectados" : "Corrupt Files Detected",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await RepairCorruptedFilesAsync(corruptedFiles);
                }
            }
            else
            {
                MessageBox.Show(
                    isSpanish
                        ? "✅ ¡Todos los archivos están correctos!\n\nTu instalación de The Sims 4 está actualizada y sin errores."
                        : "✅ All files are valid!\n\nYour The Sims 4 installation is up-to-date and error-free.",
                    isSpanish ? "Verificación Completada" : "Verification Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        private async Task DownloadOfficialDatabaseAsync()
        {
            var jsonContent = await _httpClient.GetStringAsync(OFFICIAL_DATABASE_URL);

            var serializer = new JavaScriptSerializer();
            var jsonObj = serializer.Deserialize<Dictionary<string, object>>(jsonContent);

            _officialHashes.Clear();

            if (jsonObj != null && jsonObj.ContainsKey("files"))
            {
                var filesObj = jsonObj["files"] as Dictionary<string, object>;
                if (filesObj != null)
                {
                    foreach (var kvp in filesObj)
                    {
                        // Normalizar la ruta: convertir backslashes a forward slashes
                        var normalizedKey = kvp.Key.Replace("\\", "/");
                        _officialHashes[normalizedKey] = kvp.Value.ToString().ToLowerInvariant();
                    }
                }
            }
        }

        private string CalculateSHA256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private async Task RepairCorruptedFilesAsync(List<string> corruptedFiles)
        {
            bool isSpanish = IsSpanishLanguage();

            // Separar archivos corruptos en DLCs y archivos base
            var corruptedDLCs = new HashSet<string>();
            var corruptedBaseFiles = new List<string>();

            foreach (var file in corruptedFiles)
            {
                var parts = file.Split('/');
                if (parts.Length == 0) continue;

                var rootFolder = parts[0];

                // Verificar si es un DLC (EP, GP, SP, FP)
                if (rootFolder.StartsWith("EP") || rootFolder.StartsWith("GP") ||
                    rootFolder.StartsWith("SP") || rootFolder.StartsWith("FP"))
                {
                    corruptedDLCs.Add(rootFolder);
                }
                else
                {
                    // Es un archivo base (Data, Game, Support, etc)
                    corruptedBaseFiles.Add(file);
                }
            }

            AddLog(isSpanish
                ? $"\n📊 Análisis de archivos corruptos:"
                : $"\n📊 Corrupt files analysis:");
            AddLog(isSpanish
                ? $"   DLCs corruptos: {corruptedDLCs.Count}"
                : $"   Corrupt DLCs: {corruptedDLCs.Count}");
            AddLog(isSpanish
                ? $"   Archivos base corruptos: {corruptedBaseFiles.Count}"
                : $"   Corrupt base files: {corruptedBaseFiles.Count}");

            //  NUEVO: Manejar DLCs corruptos
            if (corruptedDLCs.Count > 0)
            {
                var dlcList = string.Join(", ", corruptedDLCs.OrderBy(x => x));

                var result = MessageBox.Show(
                    isSpanish
                        ? $"Se detectaron {corruptedDLCs.Count} DLC(s) corrupto(s):\n\n{dlcList}\n\n" +
                          "¿Deseas abrir el Updater para descargarlos automáticamente?\n\n" +
                          "Los DLCs corruptos ya estarán seleccionados."
                        : $"Detected {corruptedDLCs.Count} corrupt DLC(s):\n\n{dlcList}\n\n" +
                          "Do you want to open the Updater to download them automatically?\n\n" +
                          "Corrupt DLCs will be pre-selected.",
                    isSpanish ? "DLCs Corruptos Detectados" : "Corrupt DLCs Detected",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    OpenUpdaterWithSelectedDLCs(corruptedDLCs.ToList());
                }
            }

            //  NUEVO: Manejar archivos base corruptos
            if (corruptedBaseFiles.Count > 0)
            {
                var result = MessageBox.Show(
                    isSpanish
                        ? $"Se detectaron {corruptedBaseFiles.Count} archivo(s) base corrupto(s).\n\n" +
                          "¿Deseas repararlos descargando el paquete de reparación desde GitHub?"
                        : $"Detected {corruptedBaseFiles.Count} corrupt base file(s).\n\n" +
                          "Do you want to repair them by downloading the repair package from GitHub?",
                    isSpanish ? "Archivos Base Corruptos" : "Corrupt Base Files",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await RepairBaseFilesAsync(corruptedBaseFiles);
                }
            }

            if (corruptedDLCs.Count == 0 && corruptedBaseFiles.Count == 0)
            {
                MessageBox.Show(
                    isSpanish
                        ? "No se detectaron archivos corruptos."
                        : "No corrupt files detected.",
                    isSpanish ? "Verificación Completada" : "Verification Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void OpenUpdaterWithSelectedDLCs(List<string> corruptedDLCCodes)
        {
            try
            {
                bool isSpanish = IsSpanishLanguage();

                AddLog(isSpanish
                    ? $"\n🔄 Abriendo Updater con {corruptedDLCCodes.Count} DLC(s) seleccionado(s)..."
                    : $"\n🔄 Opening Updater with {corruptedDLCCodes.Count} DLC(s) selected...");

                // Guardar lista de DLCs corruptos en un archivo temporal
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string tempDir = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
                string corruptDLCsFile = Path.Combine(tempDir, "corrupt_dlcs.txt");

                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                File.WriteAllLines(corruptDLCsFile, corruptedDLCCodes);

                AddLog(isSpanish
                    ? " Lista de DLCs corruptos guardada temporalmente."
                    : " Corrupt DLCs list saved temporarily.");

                // Abrir UpdaterWindow
                var updaterWindow = new UpdaterWindow();
                updaterWindow.Show();

                // Cerrar esta ventana
                this.Close();
            }
            catch (Exception ex)
            {
                bool isSpanish = IsSpanishLanguage();
                AddLog(isSpanish
                    ? $"❌ Error al abrir Updater: {ex.Message}"
                    : $"❌ Error opening Updater: {ex.Message}");
            }
        }

        private async Task RepairBaseFilesAsync(List<string> corruptedBaseFiles)
        {
            bool isSpanish = IsSpanishLanguage();

            AddLog(isSpanish
                ? $"\n🔧 Iniciando reparación de {corruptedBaseFiles.Count} archivo(s) base..."
                : $"\n🔧 Starting repair of {corruptedBaseFiles.Count} base file(s)...");

            var filesToDownload = new HashSet<string>();
            var processedFolders = new HashSet<string>();

            foreach (var normalizedPath in corruptedBaseFiles)
            {
                var parts = normalizedPath.Split('/');
                if (parts.Length < 2) continue;

                string rootFolder = parts[0];
                string subFolder = parts.Length > 1 ? parts[1] : "";
                string fileName = parts.Length > 2 ? parts[parts.Length - 1].ToLower() : "";

                if (rootFolder == "Game")
                {
                    string gameKey = "Game-Bin";
                    if (!processedFolders.Contains(gameKey))
                    {
                        processedFolders.Add(gameKey);
                        string zipUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/Game/Game-Bin.zip";
                        filesToDownload.Add(zipUrl);

                        AddLog(isSpanish
                            ? $"📦 Detectado archivo corrupto en Game. Se descargará: Game-Bin.zip"
                            : $"📦 Detected corrupt file in Game. Will download: Game-Bin.zip");
                    }
                }
                else if (rootFolder == "__Installer")
                {
                    string installerKey = "__Installer-FullFolder";
                    if (!processedFolders.Contains(installerKey))
                    {
                        processedFolders.Add(installerKey);
                        string zipUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/__Installer/__Installer-FullFolder.zip";
                        filesToDownload.Add(zipUrl);

                        AddLog(isSpanish
                            ? $"📦 Detectado archivo corrupto en __Installer. Se descargará: __Installer-FullFolder.zip"
                            : $"📦 Detected corrupt file in __Installer. Will download: __Installer-FullFolder.zip");
                    }
                }
                else if (rootFolder == "Support")
                {
                    string supportKey = "Support";
                    if (!processedFolders.Contains(supportKey))
                    {
                        processedFolders.Add(supportKey);
                        string zipUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/Support/Support.zip";
                        filesToDownload.Add(zipUrl);

                        AddLog(isSpanish
                            ? $"📦 Detectado archivo corrupto en Support. Se descargará: Support.zip"
                            : $"📦 Detected corrupt file in Support. Will download: Support.zip");
                    }
                }
                else if (rootFolder == "Data" && (subFolder == "Shared" || subFolder == "Simulation"))
                {
                    string folderKey = $"{rootFolder}/{subFolder}";
                    if (!processedFolders.Contains(folderKey))
                    {
                        processedFolders.Add(folderKey);
                        string zipUrl = $"https://github.com/Leuansin/leuan-dlcs/releases/download/{rootFolder}/{rootFolder}-{subFolder}.zip";
                        filesToDownload.Add(zipUrl);

                        AddLog(isSpanish
                            ? $"📦 Se descargará carpeta completa: {rootFolder}/{subFolder}"
                            : $"📦 Will download complete folder: {rootFolder}/{subFolder}");
                    }
                }
                else if (rootFolder == "Data" && subFolder == "Client" && fileName.Contains("magalog"))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string zipFileName = $"{rootFolder}-{subFolder}-{fileNameWithoutExt}.zip";
                    string zipUrl = $"https://github.com/Leuansin/leuan-dlcs/releases/download/{rootFolder}/{zipFileName}";

                    if (!filesToDownload.Contains(zipUrl))
                    {
                        filesToDownload.Add(zipUrl);
                        AddLog(isSpanish
                            ? $"📦 Se descargará: {zipFileName}"
                            : $"📦 Will download: {zipFileName}");
                    }
                }
                else if (rootFolder == "Data" && subFolder == "Client" && fileName.Contains("string"))
                {
                    string stringsKey = "Data-Client-Strings";
                    if (!processedFolders.Contains(stringsKey))
                    {
                        processedFolders.Add(stringsKey);
                        string zipUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/Data/Data-Client-Strings.zip";
                        filesToDownload.Add(zipUrl);

                        AddLog(isSpanish
                            ? $"📦 Detectado String corrupto. Se descargará: Data-Client-Strings.zip"
                            : $"📦 Detected corrupt String. Will download: Data-Client-Strings.zip");
                    }
                }
                else if (rootFolder == "Data" && subFolder == "Client" && fileName.Contains("resource"))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string zipFileName = $"{rootFolder}-{subFolder}-{fileNameWithoutExt}.zip";
                    string zipUrl = $"https://github.com/Leuansin/leuan-dlcs/releases/download/{rootFolder}/{zipFileName}";

                    if (!filesToDownload.Contains(zipUrl))
                    {
                        filesToDownload.Add(zipUrl);
                        AddLog(isSpanish
                            ? $"📦 Se descargará: {zipFileName}"
                            : $"📦 Will download: {zipFileName}");
                    }
                }
                // ========== NUEVO: Detectar archivos corruptos en Delta/EP, Delta/GP, Delta/SP, Delta/FP ==========
                else if (rootFolder == "Delta" && (subFolder.StartsWith("EP") || subFolder.StartsWith("GP") ||
                                                    subFolder.StartsWith("SP") || subFolder.StartsWith("FP")))
                {
                    string dlcKey = $"Delta/{subFolder}";
                    if (!processedFolders.Contains(dlcKey))
                    {
                        processedFolders.Add(dlcKey);
                        string zipUrl = $"https://github.com/Leuansin/leuan-dlcs/releases/download/Delta/{subFolder}.zip";
                        filesToDownload.Add(zipUrl);
                        AddLog(isSpanish
                            ? $"📦 Se descargará DLC completo desde Delta: {subFolder}"
                            : $"📦 Will download complete DLC from Delta: {subFolder}");
                    }
                }
                else
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(normalizedPath.Replace("/", "\\"));
                    string zipFileName = string.Join("-", parts.Take(parts.Length - 1)) + "-" + fileNameWithoutExt + ".zip";
                    string zipUrl = $"https://github.com/Leuansin/leuan-dlcs/releases/download/{rootFolder}/{zipFileName}";

                    if (!filesToDownload.Contains(zipUrl))
                    {
                        filesToDownload.Add(zipUrl);
                    }
                }
            }

            AddLog(isSpanish
                ? $"\n📊 Total de archivos a descargar: {filesToDownload.Count}"
                : $"\n📊 Total files to download: {filesToDownload.Count}");

            int repairedCount = 0;
            int failedCount = 0;

            string tempDir = Path.Combine(Path.GetTempPath(), "Sims4_BaseRepair");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            int currentIndex = 0;
            foreach (var downloadUrl in filesToDownload)
            {
                currentIndex++;
                string zipFileName = Path.GetFileName(downloadUrl);
                string zipPath = Path.Combine(tempDir, zipFileName);

                try
                {
                    AddLog(isSpanish
                        ? $"\n[{currentIndex}/{filesToDownload.Count}] Descargando: {zipFileName}"
                        : $"\n[{currentIndex}/{filesToDownload.Count}] Downloading: {zipFileName}");

                    AddLog(isSpanish
                        ? $"📥 URL: {downloadUrl}"
                        : $"📥 URL: {downloadUrl}");

                    using (var client = new System.Net.WebClient())
                    {
                        await client.DownloadFileTaskAsync(downloadUrl, zipPath);
                    }

                    AddLog(isSpanish
                        ? "✅ Descarga completada."
                        : "✅ Download completed.");

                    AddLog(isSpanish
                        ? "📂 Extrayendo en carpeta raíz del juego..."
                        : "📂 Extracting to game root folder...");

                    var extractedFiles = new List<string>();
                    using (var archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            string destinationPath = Path.Combine(_simsPath, entry.FullName);

                            string directoryPath = Path.GetDirectoryName(destinationPath);
                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                entry.ExtractToFile(destinationPath, overwrite: true);
                                extractedFiles.Add(destinationPath);
                            }
                        }
                    }

                    AddLog(isSpanish
                        ? "✅ Extracción completada."
                        : "✅ Extraction completed.");

                    VerifyAndMoveExtractedFiles(zipFileName, extractedFiles, isSpanish);

                    repairedCount++;

                    try
                    {
                        File.Delete(zipPath);
                    }
                    catch { }
                }
                catch (System.Net.WebException webEx)
                {
                    failedCount++;
                    AddLog(isSpanish
                        ? $"❌ Error de descarga: {webEx.Message}"
                        : $"❌ Download error: {webEx.Message}");
                    AddLog(isSpanish
                        ? $"   URL: {downloadUrl}"
                        : $"   URL: {downloadUrl}");
                    AddLog(isSpanish
                        ? "   Posiblemente el archivo no existe en GitHub Releases."
                        : "   The file may not exist in GitHub Releases.");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    AddLog(isSpanish
                        ? $"❌ Error: {ex.Message}"
                        : $"❌ Error: {ex.Message}");
                }
            }

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }

            AddLog(isSpanish
                ? $"\n📊 Reparación completada: {repairedCount} exitosos, {failedCount} fallidos"
                : $"\n📊 Repair completed: {repairedCount} successful, {failedCount} failed");

            AddLog(isSpanish
                ? "\n🔍 Verificando archivos reparados..."
                : "\n🔍 Verifying repaired files...");

            int verifiedCount = 0;
            int verificationFailedCount = 0;

            foreach (var normalizedPath in corruptedBaseFiles)
            {
                var localPath = Path.Combine(_simsPath, normalizedPath.Replace("/", "\\"));

                if (File.Exists(localPath))
                {
                    var localHash = await Task.Run(() => CalculateSHA256(localPath));
                    if (_officialHashes.ContainsKey(normalizedPath))
                    {
                        var officialHash = _officialHashes[normalizedPath];
                        if (localHash.Equals(officialHash, StringComparison.OrdinalIgnoreCase))
                        {
                            verifiedCount++;
                        }
                        else
                        {
                            verificationFailedCount++;
                        }
                    }
                }
                else
                {
                    verificationFailedCount++;
                }
            }

            AddLog(isSpanish
                ? $"✅ Verificación: {verifiedCount} correctos, {verificationFailedCount} fallidos"
                : $"✅ Verification: {verifiedCount} correct, {verificationFailedCount} failed");

            MessageBox.Show(
                isSpanish
                    ? $"Reparación de archivos base completada:\n\n" +
                      $"📥 Descargas: {repairedCount} exitosas, {failedCount} fallidas\n" +
                      $"✅ Verificación: {verifiedCount} correctos, {verificationFailedCount} fallidos\n\n" +
                      "Se recomienda ejecutar el escaneo nuevamente para verificar."
                    : $"Base files repair completed:\n\n" +
                      $"📥 Downloads: {repairedCount} successful, {failedCount} failed\n" +
                      $"✅ Verification: {verifiedCount} correct, {verificationFailedCount} failed\n\n" +
                      "It is recommended to run the scan again to verify.",
                isSpanish ? "Reparación Completada" : "Repair Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void VerifyAndMoveExtractedFiles(string zipFileName, List<string> extractedFiles, bool isSpanish)
        {
            try
            {
                string zipNameWithoutExt = Path.GetFileNameWithoutExtension(zipFileName);
                var parts = zipNameWithoutExt.Split('-');

                if (parts.Length < 2)
                    return;

                string expectedSubPath = "";

                // Casos especiales
                if (zipNameWithoutExt == "Game-Bin")
                    expectedSubPath = "Game/Bin";
                else if (zipNameWithoutExt == "__Installer-FullFolder")
                    expectedSubPath = "__Installer";
                else if (zipNameWithoutExt == "Support")
                    expectedSubPath = "Support";
                else if (zipNameWithoutExt.StartsWith("Data-Client-Magalog"))
                    expectedSubPath = "Data/Client";
                else if (zipNameWithoutExt.StartsWith("Data-Client-Strings"))
                    expectedSubPath = "Data/Client";
                else if (zipNameWithoutExt.StartsWith("Data-Client-Resources"))
                    expectedSubPath = "Data/Client";
                else if (zipNameWithoutExt.StartsWith("Data-Shared"))
                    expectedSubPath = "Data/Shared";
                else if (zipNameWithoutExt.StartsWith("Data-Simulation"))
                    expectedSubPath = "Data/Simulation";
                else
                {
                    var pathParts = new List<string>();
                    for (int i = 0; i < parts.Length - 1; i++)
                        pathParts.Add(parts[i]);
                    expectedSubPath = string.Join("/", pathParts);
                }

                if (string.IsNullOrEmpty(expectedSubPath))
                    return;

                string expectedPath = Path.Combine(_simsPath, expectedSubPath.Replace("/", "\\"));

                AddLog(isSpanish
                    ? $"   Ruta esperada: {expectedSubPath}"
                    : $"   Expected path: {expectedSubPath}");

                int movedCount = 0;

                // Primero verificar si ya se extrajo correctamente
                bool allFilesCorrect = true;
                foreach (var extractedFile in extractedFiles)
                {
                    string fileDirectory = Path.GetDirectoryName(extractedFile);

                    // Si algún archivo está en la raíz, necesitamos mover
                    if (fileDirectory.Equals(_simsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        allFilesCorrect = false;
                        break;
                    }
                }

                // Si todos los archivos ya están bien ubicados, no hacer nada
                if (allFilesCorrect)
                {
                    AddLog(isSpanish
                        ? "   ✅ Archivos ya están en ubicación correcta"
                        : "   ✅ Files already in correct location");
                    return;
                }

                // Mover archivos que están en la raíz
                foreach (var extractedFile in extractedFiles)
                {
                    string fileName = Path.GetFileName(extractedFile);
                    string fileDirectory = Path.GetDirectoryName(extractedFile);

                    if (fileDirectory.Equals(_simsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string correctPath = Path.Combine(expectedPath, fileName);

                        if (!Directory.Exists(expectedPath))
                            Directory.CreateDirectory(expectedPath);

                        try
                        {
                            if (File.Exists(correctPath))
                                File.Delete(correctPath);

                            File.Move(extractedFile, correctPath);
                            movedCount++;

                            AddLog(isSpanish
                                ? $"   ↪ Movido: {fileName} → {expectedSubPath}/"
                                : $"   ↪ Moved: {fileName} → {expectedSubPath}/");
                        }
                        catch (Exception ex)
                        {
                            AddLog(isSpanish
                                ? $"   ⚠️ Error al mover {fileName}: {ex.Message}"
                                : $"   ⚠️ Error moving {fileName}: {ex.Message}");
                        }
                    }
                }

                if (movedCount > 0)
                {
                    AddLog(isSpanish
                        ? $"   ✅ {movedCount} archivo(s) reubicado(s)"
                        : $"   ✅ {movedCount} file(s) relocated");
                }
            }
            catch (Exception ex)
            {
                AddLog(isSpanish
                    ? $"   ⚠️ Error en verificación: {ex.Message}"
                    : $"   ⚠️ Verification error: {ex.Message}");
            }
        }

        private async Task RepairGameCrackedFilesAsync()
        {
            bool isSpanish = IsSpanishLanguage();

            AddLog(isSpanish
                ? $"\n🔧 Iniciando reparación de archivos Game-Cracked..."
                : $"\n🔧 Starting Game-Cracked files repair...");

            string crackedZipUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/latestupdateandcrack/LatestUpdateAndCrack.zip";
            string tempDir = Path.Combine(Path.GetTempPath(), "Sims4_CrackedRepair");
            string zipPath = Path.Combine(tempDir, "LatestUpdateAndCrack.zip");

            try
            {
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                AddLog(isSpanish
                    ? "📥 Descargando LatestUpdateAndCrack.zip..."
                    : "📥 Downloading LatestUpdateAndCrack.zip...");

                // Descargar el archivo ZIP
                await DownloadFileWithProgressAsync(crackedZipUrl, zipPath, "LatestUpdateAndCrack.zip", 1, 1);

                AddLog(isSpanish
                    ? "✅ Descarga completada."
                    : "✅ Download completed.");

                AddLog(isSpanish
                    ? "📂 Extrayendo en carpeta raíz del juego..."
                    : "📂 Extracting to game root folder...");

                // Extraer ZIP en la carpeta raíz del juego (con sobrescritura)
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(_simsPath, entry.FullName);

                        string directoryPath = Path.GetDirectoryName(destinationPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }

 
                // Verificar archivos reparados
                AddLog(isSpanish
                    ? "\n🔍 Verificando archivos Game-Cracked reparados..."
                    : "\n🔍 Verifying repaired Game-Cracked files...");

                int verifiedCount = 0;
                int verificationFailedCount = 0;

                foreach (var normalizedPath in _corruptedCrackedFiles)
                {
                    var localPath = Path.Combine(_simsPath, normalizedPath.Replace("/", "\\"));

                    if (File.Exists(localPath))
                    {
                        var localHash = await Task.Run(() => CalculateSHA256(localPath));
                        if (_officialHashes.ContainsKey(normalizedPath))
                        {
                            var officialHash = _officialHashes[normalizedPath];
                            if (localHash.Equals(officialHash, StringComparison.OrdinalIgnoreCase))
                            {
                                verifiedCount++;
                            }
                            else
                            {
                                verificationFailedCount++;
                            }
                        }
                    }
                    else
                    {
                        verificationFailedCount++;
                    }
                }

                AddLog(isSpanish
                    ? $"✅ Verificación: {verifiedCount} correctos, {verificationFailedCount} fallidos"
                    : $"✅ Verification: {verifiedCount} correct, {verificationFailedCount} failed");

                MessageBox.Show(
                    isSpanish
                        ? $"Reparación de Game-Cracked completada:\n\n" +
                          $"✅ Verificación: {verifiedCount} correctos, {verificationFailedCount} fallidos\n\n" +
                          "Se recomienda ejecutar el escaneo nuevamente para verificar."
                        : $"Game-Cracked repair completed:\n\n" +
                          $"✅ Verification: {verifiedCount} correct, {verificationFailedCount} failed\n\n" +
                          "It is recommended to run the scan again to verify.",
                    isSpanish ? "Reparación Completada" : "Repair Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AddLog(isSpanish
                    ? $"❌ Error durante la reparación: {ex.Message}"
                    : $"❌ Error during repair: {ex.Message}");

                MessageBox.Show(
                    isSpanish
                        ? $"Error durante la reparación de Game-Cracked:\n\n{ex.Message}"
                        : $"Error during Game-Cracked repair:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DownloadFileWithProgressAsync(string url, string destinationPath, string fileName, int currentIndex, int totalCount)
        {
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var buffer = new byte[81920];
                long totalRead = 0;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var sw = Stopwatch.StartNew();
                    long lastBytesRead = 0;

                    int read;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read, _cancellationTokenSource.Token);
                        totalRead += read;

                        if (sw.ElapsedMilliseconds >= 500)
                        {
                            UpdateDownloadProgress(totalRead, totalBytes, totalRead - lastBytesRead, sw.Elapsed.TotalSeconds, fileName, currentIndex, totalCount);
                            lastBytesRead = totalRead;
                            sw.Restart();
                        }
                    }

                    UpdateDownloadProgress(totalBytes, totalBytes, 0, 0, fileName, currentIndex, totalCount);
                }
            }
        }

        private void UpdateCurrentFile(string fileName)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentFileText.Text = $"({Path.GetFileName(fileName)})";
            });
        }

        private void UpdateProgress(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                if (total > 0)
                {
                    double percent = (current * 100.0) / total;
                    ProgressPercent.Text = $"{percent:F1}%";

                    double totalWidth = ProgressPanel.ActualWidth > 0 ? ProgressPanel.ActualWidth : 700;
                    ProgressBar.Width = (percent / 100.0) * totalWidth;
                }
            });
        }

        private void UpdateStats()
        {
            Dispatcher.Invoke(() =>
            {
                bool isSpanish = IsSpanishLanguage();
                StatsText.Text = isSpanish
                    ? $"Archivos: {_scannedFiles}/{_totalFiles} | Correctos: {_correctFiles} | Corruptos: {_corruptFiles}"
                    : $"Files: {_scannedFiles}/{_totalFiles} | Valid: {_correctFiles} | Corrupt: {_corruptFiles}";
            });
        }

        private void UpdateDownloadProgress(long bytesRead, long totalBytes, long bytesSinceLast, double secondsElapsed, string fileName, int currentIndex, int totalCount)
        {
            Dispatcher.Invoke(() =>
            {
                bool isSpanish = IsSpanishLanguage();

                ProgressText.Text = isSpanish
                    ? $"Descargando {fileName}... ({currentIndex}/{totalCount})"
                    : $"Downloading {fileName}... ({currentIndex}/{totalCount})";

                if (totalBytes > 0)
                {
                    double percent = (bytesRead * 100.0) / totalBytes;
                    ProgressPercent.Text = $"{percent:F0}%";

                    double totalWidth = ProgressPanel.ActualWidth > 0 ? ProgressPanel.ActualWidth : 700;
                    ProgressBar.Width = (percent / 100.0) * totalWidth;

                    if (secondsElapsed > 0 && bytesSinceLast > 0)
                    {
                        double speedMBps = (bytesSinceLast / secondsElapsed) / (1024 * 1024);
                        SpeedText.Text = $"{speedMBps:F2} MB/s";

                        long remainingBytes = totalBytes - bytesRead;
                        if (speedMBps > 0)
                        {
                            double remainingSeconds = remainingBytes / (speedMBps * 1024 * 1024);
                            var eta = TimeSpan.FromSeconds(remainingSeconds);
                            EtaText.Text = $"{eta:mm\\:ss}";
                        }
                    }
                }
            });
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                LogScroller.ScrollToEnd();
            });
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            this.Close();
        }
    }
}