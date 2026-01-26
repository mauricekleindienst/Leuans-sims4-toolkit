using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using File = System.IO.File;
using System.Diagnostics;

namespace ModernDesign
{
    public partial class Sims3Updater : Window
    {
        private static readonly string InstallPath = @"C:\LeuansVault\The Sims 3 Complete Collection";
        private static readonly string LeuansVaultPath = @"C:\LeuansVault";
        private static readonly string DocumentsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Electronic Arts", "The Sims 3"
        );

        private readonly HttpClient _httpClient = new HttpClient();
        private bool _isReinstall = false;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused = false;

        public Sims3Updater()
        {
            InitializeComponent();
            ApplyLanguage();
            CheckInstallation();
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                TitleText.Text = "The Sims 3 - Instalador Automático";
                SubtitleText.Text = "Colección Completa + Todos los Items de la Tienda";
                DownloadBtnText.Text = "⬇️ Descargar e Instalar";
                ResumeBtnText.Text = "▶️ Reanudar Descarga";
                PauseBtnText.Text = "⏸️ Pausar";
                CancelBtnText.Text = "❌ Cancelar";
                StoreItemsBtnText.Text = "Descargar Items de Tienda";
                CreateShortcutBtnText.Text = "Crear Acceso Directo";
                AddRegistryBtnText.Text = "Agregar Registro (REQUERIDO)";
            }
            else
            {
                TitleText.Text = "The Sims 3 - Automatic Installer";
                SubtitleText.Text = "Complete Collection + All Store Items";
                DownloadBtnText.Text = "⬇️ Download & Install";
                ResumeBtnText.Text = "▶️ Resume Download";
                PauseBtnText.Text = "⏸️ Pause";
                CancelBtnText.Text = "❌ Cancel";
                StoreItemsBtnText.Text = "Download Store Items";
                CreateShortcutBtnText.Text = "Create Desktop Shortcut";
                AddRegistryBtnText.Text = "Add Registry (REQUIRED)";
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

        private void CheckInstallation()
        {
            bool isSpanish = IsSpanishLanguage();

            if (Directory.Exists(InstallPath))
            {
                _isReinstall = true;
                DownloadBtnText.Text = isSpanish ? "🔄 Reinstalar" : "🔄 Reinstall";
                AddLog(isSpanish
                    ? "✅ Instalación existente detectada. Puedes reinstalar si lo deseas."
                    : "✅ Existing installation detected. You can reinstall if you wish.");

                ShowActionButtons();
            }
            else
            {
                // Verificar si hay descargas parciales
                if (Directory.Exists(LeuansVaultPath))
                {
                    var partialFiles = Directory.GetFiles(LeuansVaultPath, "The.Sims.3.Complete.Collection.*");
                    if (partialFiles.Length > 0)
                    {
                        AddLog(isSpanish
                            ? $"📦 Se encontraron {partialFiles.Length} archivos parciales. Usa 'Reanudar' para continuar."
                            : $"📦 Found {partialFiles.Length} partial files. Use 'Resume' to continue.");

                        ResumeBtn.Visibility = Visibility.Visible;
                    }
                }

                AddLog(isSpanish
                    ? "📦 Listo para instalar The Sims 3 Complete Collection."
                    : "📦 Ready to install The Sims 3 Complete Collection.");
            }
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                LogScrollViewer.ScrollToEnd();
            });
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            if (_isReinstall)
            {
                var result = MessageBox.Show(
                    isSpanish
                        ? "¿Estás seguro de que deseas reinstalar? Esto eliminará la instalación actual."
                        : "Are you sure you want to reinstall? This will remove the current installation.",
                    isSpanish ? "Confirmar Reinstalación" : "Confirm Reinstall",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                    return;

                AddLog(isSpanish ? "🗑️ Eliminando instalación anterior..." : "🗑️ Removing previous installation...");
                try
                {
                    Directory.Delete(InstallPath, true);
                    AddLog(isSpanish ? "✅ Instalación anterior eliminada." : "✅ Previous installation removed.");
                }
                catch (Exception ex)
                {
                    AddLog(isSpanish ? $"❌ Error al eliminar: {ex.Message}" : $"❌ Error removing: {ex.Message}");
                    return;
                }
            }

            await StartDownload();
        }

        private async void ResumeBtn_Click(object sender, RoutedEventArgs e)
        {
            await StartDownload(resume: true);
        }

        private async Task StartDownload(bool resume = false)
        {
            bool isSpanish = IsSpanishLanguage();

            _cancellationTokenSource = new CancellationTokenSource();
            _isPaused = false;

            DownloadBtn.Visibility = Visibility.Collapsed;
            ResumeBtn.Visibility = Visibility.Collapsed;
            PauseBtn.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;

            if (resume)
            {
                AddLog(isSpanish ? "▶️ Reanudando descarga..." : "▶️ Resuming download...");
            }

            await DownloadAndInstallGame(_cancellationTokenSource.Token);
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            if (!_isPaused)
            {
                _isPaused = true;
                PauseBtnText.Text = isSpanish ? "▶️ Reanudar" : "▶️ Resume";
                AddLog(isSpanish ? "⏸️ Descarga pausada." : "⏸️ Download paused.");
            }
            else
            {
                _isPaused = false;
                PauseBtnText.Text = isSpanish ? "⏸️ Pausar" : "⏸️ Pause";
                AddLog(isSpanish ? "▶️ Descarga reanudada." : "▶️ Download resumed.");
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            var result = MessageBox.Show(
                isSpanish
                    ? "¿Estás seguro de que deseas cancelar la descarga?"
                    : "Are you sure you want to cancel the download?",
                isSpanish ? "Confirmar Cancelación" : "Confirm Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _cancellationTokenSource?.Cancel();
                AddLog(isSpanish ? "❌ Descarga cancelada por el usuario." : "❌ Download cancelled by user.");

                ResetDownloadButtons();
            }
        }

        private void ResetDownloadButtons()
        {
            Dispatcher.Invoke(() =>
            {
                DownloadBtn.Visibility = Visibility.Visible;
                ResumeBtn.Visibility = Visibility.Visible;
                PauseBtn.Visibility = Visibility.Collapsed;
                CancelBtn.Visibility = Visibility.Collapsed;

                bool isSpanish = IsSpanishLanguage();
                PauseBtnText.Text = isSpanish ? "⏸️ Pausar" : "⏸️ Pause";
            });
        }
        private async Task DownloadAndInstallGame(CancellationToken cancellationToken)
        {
            bool isSpanish = IsSpanishLanguage();

            try
            {
                // Crear carpeta LeuansVault
                AddLog(isSpanish ? "📁 Creando carpeta LeuansVault..." : "📁 Creating LeuansVault folder...");
                Directory.CreateDirectory(LeuansVaultPath);

                // URLs de descarga
                var urls = new List<string>
                {
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.zip",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z01",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z02",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z03",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z04",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z05",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z06",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z07",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z08",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z09",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z10",
                    "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/The.Sims.3.Complete.Collection.z11"
                };

                // Descargar archivos
                for (int i = 0; i < urls.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        ResetDownloadButtons();
                        return;
                    }

                    string url = urls[i];
                    string fileName = Path.GetFileName(url);
                    string filePath = Path.Combine(LeuansVaultPath, fileName);

                    // Verificar si el archivo ya existe y es válido
                    if (await IsFileValidAsync(url, filePath))
                    {
                        AddLog(isSpanish
                            ? $"✅ {fileName} ya existe y es válido. Saltando descarga."
                            : $"✅ {fileName} already exists and is valid. Skipping download.");
                        continue;
                    }

                    AddLog(isSpanish
                        ? $"⬇️ Descargando {fileName} ({i + 1}/{urls.Count})..."
                        : $"⬇️ Downloading {fileName} ({i + 1}/{urls.Count})...");

                    await DownloadFileAsync(url, filePath, cancellationToken);

                    AddLog(isSpanish
                        ? $"✅ {fileName} descargado correctamente."
                        : $"✅ {fileName} downloaded successfully.");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    ResetDownloadButtons();
                    return;
                }

                AddLog(isSpanish
                    ? "📦 Extrayendo archivos... (Esto puede tomar varios minutos)"
                    : "📦 Extracting files... (This may take several minutes)");

                string zipPath = Path.Combine(LeuansVaultPath, "The.Sims.3.Complete.Collection.zip");

                // Extraer
                await ExtractMultiPartArchive(null, zipPath, LeuansVaultPath, CancellationToken.None);

                AddLog(isSpanish
                    ? "✅ Extracción completada exitosamente."
                    : "✅ Extraction completed successfully.");

                // Ofrecer limpiar archivos temporales
                await OfferCleanupTempFiles(LeuansVaultPath, "*.z*");

                AddLog(isSpanish
                    ? "🎉 ¡Instalación del juego completada!"
                    : "🎉 Game installation completed!");

                ShowActionButtons();
                ResetDownloadButtons();
            }
            catch (OperationCanceledException)
            {
                AddLog(isSpanish
                    ? "⚠️ Operación cancelada."
                    : "⚠️ Operation cancelled.");
                ResetDownloadButtons();
            }
            catch (Exception ex)
            {
                AddLog(isSpanish
                    ? $"❌ Error durante la instalación: {ex.Message}"
                    : $"❌ Error during installation: {ex.Message}");
                ResetDownloadButtons();
            }
        }

        private async Task<string> DownloadAndInstall7Zip()
        {
            bool isSpanish = IsSpanishLanguage();

            var result = MessageBox.Show(
                isSpanish
                    ? "7-Zip no está instalado. ¿Deseas descargarlo e instalarlo automáticamente?"
                    : "7-Zip is not installed. Do you want to download and install it automatically?",
                isSpanish ? "7-Zip Requerido" : "7-Zip Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
            {
                throw new Exception(isSpanish
                    ? "7-Zip es necesario para continuar."
                    : "7-Zip is required to continue.");
            }

            AddLog(isSpanish
                ? "📥 Descargando 7-Zip..."
                : "📥 Downloading 7-Zip...");

            string installerPath = Path.Combine(Path.GetTempPath(), "7z-installer.exe");
            string installerUrl = "https://www.7-zip.org/a/7z2408-x64.exe";

            await DownloadFileAsync(installerUrl, installerPath, CancellationToken.None);

            AddLog(isSpanish
                ? "⚙️ Instalando 7-Zip..."
                : "⚙️ Installing 7-Zip...");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/S", // Instalación silenciosa
                UseShellExecute = true,
                Verb = "runas" // Ejecutar como admin
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                await Task.Run(() => process.WaitForExit());
            }

            File.Delete(installerPath);

            AddLog(isSpanish
                ? "✅ 7-Zip instalado correctamente."
                : "✅ 7-Zip installed successfully.");

            return @"C:\Program Files\7-Zip\7z.exe";
        }
        private async Task ExtractMultiPartArchive(string sevenZipPath, string archivePath, string destinationPath, CancellationToken cancellationToken)
        {
            bool isSpanish = IsSpanishLanguage();

            string sevenZipExe = await Find7ZipExecutable();

            if (string.IsNullOrEmpty(sevenZipExe))
            {
                throw new Exception(isSpanish
                    ? "❌ 7-Zip no está instalado. Por favor instala 7-Zip desde https://www.7-zip.org/"
                    : "❌ 7-Zip is not installed. Please install 7-Zip from https://www.7-zip.org/");
            }

            AddLog(isSpanish
                ? $"✅ Usando 7-Zip: {sevenZipExe}"
                : $"✅ Using 7-Zip: {sevenZipExe}");

            AddLog(isSpanish
                ? "📦 Extrayendo archivos multi-parte..."
                : "📦 Extracting multi-part files...");

            Directory.CreateDirectory(destinationPath);

            var processInfo = new ProcessStartInfo
            {
                FileName = sevenZipExe,
                Arguments = $"x \"{archivePath}\" -o\"{Path.GetFullPath(destinationPath)}\" -y -bsp1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = destinationPath
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    throw new Exception(isSpanish
                        ? "No se pudo iniciar 7-Zip"
                        : "Could not start 7-Zip");
                }

                // Leer salida en tiempo real
                Task outputTask = Task.Run(async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        // Verificar cancelación
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                process.Kill();
                                AddLog(isSpanish ? "❌ Extracción cancelada." : "❌ Extraction cancelled.");
                            }
                            catch { }
                            return;
                        }

                        // Verificar pausa
                        while (_isPaused && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(500, cancellationToken);
                        }

                        string line = await process.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (line.Contains("%") || line.Contains("Extracting") || line.Contains("Everything is Ok"))
                            {
                                AddLog($"7z: {line.Trim()}");
                            }
                        }
                    }
                }, cancellationToken);

                Task errorTask = Task.Run(async () =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        string line = await process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            AddLog($"⚠️ {line}");
                        }
                    }
                }, cancellationToken);

                try
                {
                    await Task.WhenAll(outputTask, errorTask);
                    await Task.Run(() => process.WaitForExit(), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Matar el proceso si fue cancelado
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000); // Esperar máximo 5 segundos
                        }
                        catch { }
                    }
                    throw;
                }

                // Verificar si el proceso fue cancelado
                if (cancellationToken.IsCancellationRequested)
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                    }
                    throw new OperationCanceledException();
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception(isSpanish
                        ? $"Error al extraer (código {process.ExitCode}). Verifica que todos los archivos .z01, .z02, etc. estén descargados correctamente."
                        : $"Extraction error (code {process.ExitCode}). Verify that all .z01, .z02, etc. files are downloaded correctly.");
                }
            }

            AddLog(isSpanish
                ? "✅ Extracción completada exitosamente."
                : "✅ Extraction completed successfully.");
        }

        private async Task<string> Find7ZipExecutable()
        {
            // Buscar 7-Zip en ubicaciones comunes
            string[] possiblePaths = new[]
            {
        @"C:\Program Files\7-Zip\7z.exe",
        @"C:\Program Files (x86)\7-Zip\7z.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
    };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Si no se encuentra, descargar e instalar
            return await DownloadAndInstall7Zip();
        }

        private async Task<bool> IsFileValidAsync(string url, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // Obtener tamaño esperado del servidor
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                if (!response.IsSuccessStatusCode)
                    return false;

                long? expectedSize = response.Content.Headers.ContentLength;
                if (!expectedSize.HasValue)
                    return false;

                // Comparar tamaño del archivo local
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length != expectedSize.Value)
                    return false;

                // Archivo existe y tiene el tamaño correcto
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
        {
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        // Esperar si está pausado
                        while (_isPaused && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationToken);
                        }

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    }
                }
            }
        }

        private async Task OfferCleanupTempFiles(string directory, string pattern)
        {
            bool isSpanish = IsSpanishLanguage();

            var result = MessageBox.Show(
                isSpanish
                    ? "¿Deseas liberar espacio eliminando los archivos temporales de descarga?"
                    : "Do you want to free up space by deleting temporary download files?",
                isSpanish ? "Limpiar Archivos Temporales" : "Clean Temporary Files",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(directory, pattern);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            AddLog(isSpanish
                                ? $"🗑️ Eliminado: {Path.GetFileName(file)}"
                                : $"🗑️ Deleted: {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            AddLog(isSpanish
                                ? $"⚠️ No se pudo eliminar {Path.GetFileName(file)}: {ex.Message}"
                                : $"⚠️ Could not delete {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                });

                AddLog(isSpanish
                    ? "✅ Limpieza completada."
                    : "✅ Cleanup completed.");
            }
        }

        private void ShowActionButtons()
        {
            Dispatcher.Invoke(() =>
            {
                ActionButtonsPanel.Visibility = Visibility.Visible;
                StoreItemsBtn.Visibility = Visibility.Visible;
                CreateShortcutBtn.Visibility = Visibility.Visible;
                AddRegistryBtn.Visibility = Visibility.Visible;
            });
        }

        private async void StoreItemsBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();
            StoreItemsBtn.IsEnabled = false;

            // IMPORTANTE: Crear el CancellationTokenSource ANTES de usarlo
            _cancellationTokenSource = new CancellationTokenSource();
            _isPaused = false;

            // Mostrar botones de control
            PauseBtn.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;

            try
            {
                AddLog(isSpanish
                    ? "🛍️ Descargando Store Items..."
                    : "🛍️ Downloading Store Items...");

                // Crear carpeta Documents si no existe
                Directory.CreateDirectory(DocumentsPath);

                var urls = new List<string>
        {
            "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/StrisM/The.Sims.3.-.Documents.zip",
            "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/StrisM/The.Sims.3.-.Documents.z01",
            "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/StrisM/The.Sims.3.-.Documents.z02",
            "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/StrisM/The.Sims.3.-.Documents.z03",
            "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/StrisM/The.Sims.3.-.Documents.z04",
            "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/StrisM/The.Sims.3.-.Documents.z05"
        };

                // Descargar en LeuansVault
                string tempDownloadPath = Path.Combine(LeuansVaultPath, "StoreItems");
                Directory.CreateDirectory(tempDownloadPath);

                // Descargar archivos
                for (int i = 0; i < urls.Count; i++)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        AddLog(isSpanish ? "❌ Descarga cancelada." : "❌ Download cancelled.");
                        ResetDownloadButtons();
                        return;
                    }

                    string url = urls[i];
                    string fileName = Path.GetFileName(url);
                    string filePath = Path.Combine(tempDownloadPath, fileName);

                    // Verificar si el archivo ya existe y es válido
                    if (await IsFileValidAsync(url, filePath))
                    {
                        AddLog(isSpanish
                            ? $"✅ {fileName} ya existe y es válido. Saltando descarga."
                            : $"✅ {fileName} already exists and is valid. Skipping download.");
                        continue;
                    }

                    AddLog(isSpanish
                        ? $"⬇️ Descargando {fileName} ({i + 1}/{urls.Count})..."
                        : $"⬇️ Downloading {fileName} ({i + 1}/{urls.Count})...");

                    await DownloadFileAsync(url, filePath, _cancellationTokenSource.Token);

                    AddLog(isSpanish
                        ? $"✅ {fileName} descargado."
                        : $"✅ {fileName} downloaded.");
                }

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    AddLog(isSpanish ? "❌ Operación cancelada." : "❌ Operation cancelled.");
                    ResetDownloadButtons();
                    return;
                }

                // Extraer archivos usando 7-Zip
                AddLog(isSpanish
                    ? "📦 Extrayendo Store Items..."
                    : "📦 Extracting Store Items...");

                string zipPath = Path.Combine(tempDownloadPath, "The.Sims.3.-.Documents.zip");

                // Extraer en la carpeta padre de DocumentsPath para evitar duplicación
                string extractPath = Path.GetDirectoryName(DocumentsPath);

                await ExtractMultiPartArchive(null, zipPath, extractPath, _cancellationTokenSource.Token);

                AddLog(isSpanish
                    ? "✅ Store Items extraídos correctamente."
                    : "✅ Store Items extracted successfully.");

                // Ofrecer limpiar archivos temporales
                await OfferCleanupTempFiles(tempDownloadPath, "*.*");

                AddLog(isSpanish
                    ? "🎉 ¡Store Items instalados exitosamente!"
                    : "🎉 Store Items installed successfully!");
            }
            catch (OperationCanceledException)
            {
                AddLog(isSpanish
                    ? "⚠️ Operación cancelada."
                    : "⚠️ Operation cancelled.");
            }
            catch (Exception ex)
            {
                AddLog(isSpanish
                    ? $"❌ Error descargando Store Items: {ex.Message}"
                    : $"❌ Error downloading Store Items: {ex.Message}");
            }
            finally
            {
                StoreItemsBtn.IsEnabled = true;
                PauseBtn.Visibility = Visibility.Collapsed;
                CancelBtn.Visibility = Visibility.Collapsed;

                PauseBtnText.Text = isSpanish ? "⏸️ Pausar" : "⏸️ Pause";

                // Limpiar el token
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CreateShortcutBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            try
            {
                string exePath = Path.Combine(InstallPath, "Game", "Bin", "TS3W.exe");

                if (!File.Exists(exePath))
                {
                    AddLog(isSpanish
                        ? "❌ No se encontró el ejecutable del juego. Asegúrate de que el juego esté instalado."
                        : "❌ Game executable not found. Make sure the game is installed.");
                    return;
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "The Sims 3 - LeuansVault.lnk");

                // Crear shortcut usando COM dinámico
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = "The Sims 3 - LeuansVault Edition";
                shortcut.IconLocation = exePath;
                shortcut.Save();

                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);

                AddLog(isSpanish
                    ? "✅ Acceso directo creado en el escritorio."
                    : "✅ Desktop shortcut created successfully.");

                MessageBox.Show(
                    isSpanish
                        ? "¡Acceso directo creado exitosamente en tu escritorio!"
                        : "Shortcut created successfully on your desktop!",
                    isSpanish ? "Éxito" : "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                AddLog(isSpanish
                    ? $"❌ Error creando acceso directo: {ex.Message}"
                    : $"❌ Error creating shortcut: {ex.Message}");
            }
        }

        private async void AddRegistryBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();
            AddRegistryBtn.IsEnabled = false;

            try
            {
                string regUrl = "https://github.com/Johnn-sin/WindowsDevelopment/releases/download/stres/Sims3Reg.reg";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string regFilePath = Path.Combine(desktopPath, "Sims3Reg.reg");

                AddLog(isSpanish
                    ? "⬇️ Descargando archivo de registro..."
                    : "⬇️ Downloading registry file...");

                await DownloadFileAsync(regUrl, regFilePath, CancellationToken.None);

                AddLog(isSpanish
                    ? "✅ Archivo de registro descargado en el escritorio."
                    : "✅ Registry file downloaded to desktop.");

                MessageBox.Show(
                    isSpanish
                        ? $"El archivo de registro ha sido descargado en tu escritorio:\n\n{regFilePath}\n\nPor favor, haz doble clic en él para agregar los registros necesarios de Sims 3.\n\n⚠️ ESTO ES NECESARIO PARA QUE EL JUEGO FUNCIONE CORRECTAMENTE."
                        : $"The registry file has been downloaded to your desktop:\n\n{regFilePath}\n\nPlease double-click it to add the necessary Sims 3 registry entries.\n\n⚠️ THIS IS REQUIRED FOR THE GAME TO WORK PROPERLY.",
                    isSpanish ? "Archivo de Registro Descargado" : "Registry File Downloaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
            catch (Exception ex)
            {
                AddLog(isSpanish
                    ? $"❌ Error descargando archivo de registro: {ex.Message}"
                    : $"❌ Error downloading registry file: {ex.Message}");
            }
            finally
            {
                AddRegistryBtn.IsEnabled = true;
            }
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
            _cancellationTokenSource?.Cancel();
            this.Close();
        }
    }
}