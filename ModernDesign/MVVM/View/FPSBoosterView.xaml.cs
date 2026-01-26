using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinForms = System.Windows.Forms;

namespace ModernDesign.MVVM.View
{
    public partial class FPSBoosterView : UserControl
    {
        private string _languageCode = "en-US";
        private string _sims4Folder;
        private string _modsFolder;
        private string _screenshotsFolder;
        private string _optionsIniPath;

        private bool _cacheIssue = false;
        private bool _mergeIssue = false;
        private bool _charsIssue = false;
        private bool _modMissing = false;
        private bool _screenshotIssue = false;
        private bool _modListIssue = false; // NUEVO

        private List<string> _filesWithSpecialChars = new List<string>();
        private int _packageCount = 0;
        private long _screenshotTotalSize = 0;
        private int _pngCount = 0;
        private int _jpgCount = 0;

        private const string COMPRESSOR_URL = "https://github.com/Johnn-sin/leuansin-dlcs/releases/download/Misc/leuan-compressor.exe";
        private string _compressorPath = "";

        public FPSBoosterView()
        {
            InitializeComponent();
            InitLocalization();
            FindSims4Folder();
            FindScreenshotsFolder();
            FindOptionsIni();
        }

        private void InitLocalization()
        {
            LoadLanguageFromIni();
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            TitleText.Text = "FPS Booster";
            SubtitleText.Text = es
                ? "Analiza y optimiza el rendimiento de tu juego"
                : "Analyze and optimize your game performance";
            ScoreLabel.Text = es ? "Optimizado" : "Optimized";

            CacheTitle.Text = es ? "Caché de Miniaturas" : "Thumbnail Cache";
            MergeTitle.Text = es ? "Combinar Packages" : "Package Merging";
            CharsTitle.Text = es ? "Nombres de Archivos" : "File Names";
            ModTitle.Text = "FPS Booster Mod";
            ScreenshotTitle.Text = es ? "Tamaño de Capturas" : "Screenshot File Size";
            ModListTitle.Text = es ? "Mostrar Lista de Mods al Inicio" : "Show Mod List on Startup"; // NUEVO

            ScanButton.Content = es ? "🔍 Escanear" : "🔍 Scan Now";
            OptimizeAllButton.Content = es ? "⚡ Optimizar Todo" : "⚡ Optimize All";

            // Texto del nuevo botón para cambiar carpeta
            ChangeFolderButton.Content = es ? "Cambiar carpeta..." : "Change folder...";
        }

        private void LoadLanguageFromIni()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string iniPath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

                if (!File.Exists(iniPath)) return;

                foreach (var line in File.ReadAllLines(iniPath))
                {
                    if (line.Trim().StartsWith("Language", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('=');
                        if (parts.Length >= 2)
                            _languageCode = parts[1].Trim();
                        break;
                    }
                }

                if (_languageCode != "es-ES" && _languageCode != "en-US")
                    _languageCode = "en-US";
            }
            catch { _languageCode = "en-US"; }
        }

        private void FindSims4Folder()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Intentar español primero
            _sims4Folder = Path.Combine(docs, "Electronic Arts", "Los Sims 4");
            if (!Directory.Exists(_sims4Folder))
            {
                _sims4Folder = Path.Combine(docs, "Electronic Arts", "The Sims 4");
            }

            if (Directory.Exists(_sims4Folder))
            {
                _modsFolder = Path.Combine(_sims4Folder, "Mods");
            }
        }

        private void FindScreenshotsFolder()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string[] possiblePaths = new string[]
            {
                Path.Combine(docs, "Electronic Arts", "Los Sims 4", "Capturas de pantalla"),
                Path.Combine(docs, "Electronic Arts", "Los Sims 4", "Screenshots"),
                Path.Combine(docs, "Electronic Arts", "The Sims 4", "Capturas de pantalla"),
                Path.Combine(docs, "Electronic Arts", "The Sims 4", "Screenshots")
            };

            foreach (string path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    _screenshotsFolder = path;
                    return;
                }
            }
        }

        // NUEVO: Buscar Options.ini
        private void FindOptionsIni()
        {
            if (string.IsNullOrEmpty(_sims4Folder) || !Directory.Exists(_sims4Folder))
                return;

            string optionsPath = Path.Combine(_sims4Folder, "Options.ini");
            if (File.Exists(optionsPath))
            {
                _optionsIniPath = optionsPath;
            }
        }

        // NUEVO: cambiar manualmente la carpeta de The Sims 4
        private void ChangeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

                using (var dialog = new WinForms.FolderBrowserDialog())
                {
                    dialog.Description = es
                        ? "Selecciona la carpeta principal de The Sims 4 (donde está 'Mods' y 'localthumbcache.package')."
                        : "Select The Sims 4 main folder (where 'Mods' and 'localthumbcache.package' are).";

                    if (!string.IsNullOrEmpty(_sims4Folder) && Directory.Exists(_sims4Folder))
                        dialog.SelectedPath = _sims4Folder;

                    if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        _sims4Folder = dialog.SelectedPath;

                        // Intentar detectar carpeta Mods dentro de esa ruta
                        string modsCandidate = Path.Combine(_sims4Folder, "Mods");
                        _modsFolder = Directory.Exists(modsCandidate) ? modsCandidate : _sims4Folder;

                        // Buscar Options.ini en la nueva ruta
                        FindOptionsIni();

                        MessageBox.Show(
                            es
                                ? $"Ruta actualizada:\n{_sims4Folder}"
                                : $"Path updated:\n{_sims4Folder}",
                            "FPS Booster",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            ScanButton.IsEnabled = false;
            ScanButton.Content = es ? "⏳ Escaneando..." : "⏳ Scanning...";

            // Reset
            _cacheIssue = false;
            _mergeIssue = false;
            _charsIssue = false;
            _modMissing = false;
            _screenshotIssue = false;
            _modListIssue = false; // NUEVO
            _filesWithSpecialChars.Clear();

            await Task.Run(() => PerformScan());

            UpdateUI();

            ScanButton.IsEnabled = true;
            ScanButton.Content = es ? "🔍 Escanear" : "🔍 Scan Now";
        }

        private void PerformScan()
        {
            if (string.IsNullOrEmpty(_sims4Folder) || !Directory.Exists(_sims4Folder))
                return;

            // 1. Check localthumbcache.package
            string cachePath = Path.Combine(_sims4Folder, "localthumbcache.package");
            _cacheIssue = File.Exists(cachePath);

            // 2. Check package count (too many unmerged)
            if (Directory.Exists(_modsFolder))
            {
                var packages = Directory.GetFiles(_modsFolder, "*.package", SearchOption.AllDirectories);
                _packageCount = packages.Length;
                _mergeIssue = _packageCount > 500; // Threshold: más de 500 packages

                // 3. Check for special characters
                var allFiles = Directory.GetFiles(_modsFolder, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".package") || f.EndsWith(".ts4script"));

                var specialCharsPattern = new Regex(@"[^A-Za-z0-9_\-\.]", RegexOptions.Compiled);

                foreach (var file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    if (specialCharsPattern.IsMatch(fileName))
                    {
                        _filesWithSpecialChars.Add(file);
                    }
                }
                _charsIssue = _filesWithSpecialChars.Count > 0;


                // 4. Check FPS Booster mod
                var fpsBoosterExists = Directory.GetFiles(_modsFolder, "*fps*booster*.package", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(_modsFolder, "*leuan*fps*.package", SearchOption.AllDirectories))
                    .Any();
                _modMissing = !fpsBoosterExists;
            }

            // 5. Check Screenshot File Size
            if (!string.IsNullOrEmpty(_screenshotsFolder) && Directory.Exists(_screenshotsFolder))
            {
                _screenshotTotalSize = 0;
                _pngCount = 0;
                _jpgCount = 0;

                // Solo archivos en la raíz, sin subcarpetas
                var pngFiles = Directory.GetFiles(_screenshotsFolder, "*.png", SearchOption.TopDirectoryOnly);
                var jpgFiles = Directory.GetFiles(_screenshotsFolder, "*.jpg", SearchOption.TopDirectoryOnly);
                var jpegFiles = Directory.GetFiles(_screenshotsFolder, "*.jpeg", SearchOption.TopDirectoryOnly);

                _pngCount = pngFiles.Length;
                _jpgCount = jpgFiles.Length + jpegFiles.Length;

                // Calcular tamaño total
                foreach (var file in pngFiles.Concat(jpgFiles).Concat(jpegFiles))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        _screenshotTotalSize += fi.Length;
                    }
                    catch { }
                }

                // Marcar como issue si:
                // - Hay PNGs (no están comprimidos)
                // - O el tamaño total es mayor a 1GB
                const long ONE_GB = 1073741824; // 1GB en bytes
                _screenshotIssue = _pngCount > 0 || _screenshotTotalSize > ONE_GB;
            }

            // 6. Check Show Mod List on Startup (NUEVO)
            if (string.IsNullOrEmpty(_optionsIniPath) || !File.Exists(_optionsIniPath))
            {
                // Si no se encuentra Options.ini, marcar como issue crítico
                _modListIssue = true;
            }
            else
            {
                try
                {
                    string[] lines = File.ReadAllLines(_optionsIniPath);
                    bool found = false;

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("showmodliststartup", StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            // Buscar el valor después del '='
                            int equalsIndex = trimmed.IndexOf('=');
                            if (equalsIndex >= 0 && equalsIndex < trimmed.Length - 1)
                            {
                                string value = trimmed.Substring(equalsIndex + 1).Trim();
                                // Si el valor es "1", es un issue
                                _modListIssue = value == "1";
                            }
                            break;
                        }
                    }

                    // Si no se encontró la línea, asumir que está en 1 (default del juego)
                    if (!found)
                    {
                        _modListIssue = true;
                    }
                }
                catch
                {
                    _modListIssue = true;
                }
            }
        }

        private void UpdateUI()
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
            int issueCount = 0;

            // Cache
            if (_cacheIssue)
            {
                CacheStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                CacheDesc.Text = es ? "localthumbcache.package encontrado - Eliminar para mejorar"
                                    : "localthumbcache.package found - Delete to improve";
                CacheFixBtn.Visibility = Visibility.Visible;
                issueCount++;
            }
            else
            {
                CacheStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                CacheDesc.Text = es ? "✓ No se encontró caché - ¡Perfecto!"
                                    : "✓ No cache found - Perfect!";
                CacheFixBtn.Visibility = Visibility.Collapsed;
            }

            // Merge
            if (_mergeIssue)
            {
                MergeStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                MergeDesc.Text = es ? $"⚠ {_packageCount} packages sin combinar"
                                    : $"⚠ {_packageCount} unmerged packages";
                issueCount++;
            }
            else
            {
                MergeStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                MergeDesc.Text = es ? $"✓ {_packageCount} packages - Cantidad razonable"
                                    : $"✓ {_packageCount} packages - Reasonable amount";
            }

            // Special chars
            if (_charsIssue)
            {
                CharsStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                CharsDesc.Text = es ? $"⚠ {_filesWithSpecialChars.Count} archivos con caracteres especiales"
                                    : $"⚠ {_filesWithSpecialChars.Count} files with special characters";
                CharsFixBtn.Visibility = Visibility.Visible;
                issueCount++;
            }
            else
            {
                CharsStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                CharsDesc.Text = es ? "✓ Todos los nombres de archivo son válidos"
                                    : "✓ All file names are valid";
                CharsFixBtn.Visibility = Visibility.Collapsed;
            }

            // FPS Mod
            if (_modMissing)
            {
                ModStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                ModDesc.Text = es ? "FPS Booster de Leuan no instalado"
                                  : "Leuan's FPS Booster not installed";
                ModDownloadBtn.Visibility = Visibility.Visible;
                issueCount++;
            }
            else
            {
                ModStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                ModDesc.Text = es ? "✓ FPS Booster instalado"
                                  : "✓ FPS Booster installed";
                ModDownloadBtn.Visibility = Visibility.Collapsed;
            }

            // Screenshot File Size
            if (_screenshotIssue)
            {
                ScreenshotStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                string sizeText = FormatFileSize(_screenshotTotalSize);
                const long ONE_GB = 1073741824;

                if (_pngCount > 0 && _screenshotTotalSize > ONE_GB)
                {
                    ScreenshotDesc.Text = es
                        ? $"⚠ {_pngCount} PNG sin comprimir + {sizeText} total"
                        : $"⚠ {_pngCount} uncompressed PNG + {sizeText} total";
                }
                else if (_pngCount > 0)
                {
                    ScreenshotDesc.Text = es
                        ? $"⚠ {_pngCount} PNG sin comprimir ({sizeText})"
                        : $"⚠ {_pngCount} uncompressed PNG ({sizeText})";
                }
                else
                {
                    ScreenshotDesc.Text = es
                        ? $"⚠ {sizeText} - Considera borrar capturas antiguas"
                        : $"⚠ {sizeText} - Consider deleting old screenshots";
                }

                ScreenshotBoost.Text = es ? "Espacio de disco" : "Disk Space";
                ScreenshotBoost.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ScreenshotFixBtn.Visibility = Visibility.Visible;
                issueCount++;
            }
            else
            {
                ScreenshotStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                string sizeText = FormatFileSize(_screenshotTotalSize);
                ScreenshotDesc.Text = es
                    ? $"✓ {_jpgCount} JPG optimizados ({sizeText})"
                    : $"✓ {_jpgCount} optimized JPG ({sizeText})";
                ScreenshotBoost.Text = "+5%";
                ScreenshotBoost.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                ScreenshotFixBtn.Visibility = Visibility.Collapsed;
            }

            // Show Mod List on Startup (NUEVO)
            if (_modListIssue)
            {
                ModListStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                if (string.IsNullOrEmpty(_optionsIniPath) || !File.Exists(_optionsIniPath))
                {
                    ModListDesc.Text = es
                        ? "⚠ Options.ini no encontrado"
                        : "⚠ Options.ini not found";
                }
                else
                {
                    ModListDesc.Text = es
                        ? "⚠ Lista de mods se muestra al inicio - Ralentiza carga 80%"
                        : "⚠ Mod list shows on startup - Slows loading 80%";
                }

                ModListFixBtn.Visibility = Visibility.Visible;
                issueCount++;
            }
            else
            {
                ModListStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                ModListDesc.Text = es
                    ? "✓ Lista de mods desactivada al inicio"
                    : "✓ Mod list disabled on startup";
                ModListFixBtn.Visibility = Visibility.Collapsed;
            }

            // Calculate score
            // CRÍTICO: Si ModList está activado, el score máximo es 50%
            int maxScore = _modListIssue ? 50 : 100;

            // Ahora son 6 items, pero ModList es crítico
            // Si ModList está mal, restamos 50 puntos de entrada
            // Los otros 5 items valen 10 puntos cada uno
            int score = maxScore;

            if (_cacheIssue) score -= 10;
            if (_mergeIssue) score -= 10;
            if (_charsIssue) score -= 10;
            if (_modMissing) score -= 10;
            if (_screenshotIssue) score -= 10;

            if (score < 0) score = 0;

            UpdateScoreDisplay(score);

            // Enable optimize button if there are issues
            OptimizeAllButton.IsEnabled = issueCount > 0;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void UpdateScoreDisplay(int score)
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            ScoreText.Text = $"{score}%";

            Color scoreColor;
            string label;

            if (score >= 80)
            {
                scoreColor = (Color)ColorConverter.ConvertFromString("#22C55E");
                label = es ? "Excelente" : "Excellent";
            }
            else if (score >= 60)
            {
                scoreColor = (Color)ColorConverter.ConvertFromString("#84CC16");
                label = es ? "Bueno" : "Good";
            }
            else if (score >= 40)
            {
                scoreColor = (Color)ColorConverter.ConvertFromString("#F59E0B");
                label = es ? "Regular" : "Fair";
            }
            else
            {
                scoreColor = (Color)ColorConverter.ConvertFromString("#EF4444");
                label = es ? "Necesita Mejoras" : "Needs Work";
            }

            ScoreColor.Color = scoreColor;
            ScoreLabel.Text = label;

            // Animate the ring
            const double circumference = 314.0; // valor grande para simular la circunferencia
            double dashValue = (score / 100.0) * circumference;
            double gapValue = Math.Max(circumference - dashValue, 0.0);

            ScoreRing.StrokeDashArray = new DoubleCollection { dashValue, gapValue };
        }

        private void CacheFixBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cachePath = Path.Combine(_sims4Folder, "localthumbcache.package");
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                    _cacheIssue = false;
                    UpdateUI();

                    bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                    MessageBox.Show(
                        es ? "Caché eliminada correctamente. El juego la regenerará al iniciar."
                           : "Cache deleted successfully. The game will regenerate it on startup.",
                        "FPS Booster",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MergeInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            string message = es
                ? "Para combinar tus packages, usa herramientas como:\n\n" +
                  "• Sims 4 Mod Folder Merger\n" +
                  "• Sims 4 Studio (Batch Fix)\n\n" +
                  "Esto reducirá drásticamente los tiempos de carga."
                : "To merge your packages, use tools like:\n\n" +
                  "• Sims 4 Mod Folder Merger\n" +
                  "• Sims 4 Studio (Batch Fix)\n\n" +
                  "This will drastically reduce loading times.";

            MessageBox.Show(message, "Package Merging", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CharsFixBtn_Click(object sender, RoutedEventArgs e)
        {
            bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(_modsFolder) || !Directory.Exists(_modsFolder))
            {
                MessageBox.Show(
                    es ? "No se pudo encontrar la carpeta Mods." : "Could not find Mods folder.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Abrir el Renombrador de Mods directamente
            var renamerWindow = new ModRenamerWindow(_modsFolder);
            renamerWindow.ShowDialog();

            // Re-escanear después de cerrar la ventana
            ScanButton_Click(null, null);
        }

        private async void ModDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(_modsFolder) || !Directory.Exists(_modsFolder))
                {
                    MessageBox.Show(
                        es ? "No se pudo encontrar la carpeta Mods." : "Could not find Mods folder.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                string destinationPath = Path.Combine(_modsFolder, "leuanfps.package");

                // Descargar el archivo
                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync("https://github.com/Johnn-sin/leuansin-dlcs/releases/download/FPSBooster/leuanfps.package");
                    File.WriteAllBytes(destinationPath, data);
                }

                _modMissing = false;
                UpdateUI();

                MessageBox.Show(
                    es ? $"FPS Booster instalado correctamente en:\n{destinationPath}"
                       : $"FPS Booster installed successfully at:\n{destinationPath}",
                    "FPS Booster",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                MessageBox.Show(
                    $"{(es ? "Error descargando el mod: " : "Error downloading mod: ")}{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ScreenshotFixBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

                // Si no se detectó la carpeta, pedir al usuario que la seleccione
                if (string.IsNullOrEmpty(_screenshotsFolder) || !Directory.Exists(_screenshotsFolder))
                {
                    using (var dialog = new WinForms.FolderBrowserDialog())
                    {
                        dialog.Description = es
                            ? "Selecciona la carpeta de capturas de pantalla de The Sims 4"
                            : "Select The Sims 4 Screenshots folder";

                        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                        {
                            _screenshotsFolder = dialog.SelectedPath;
                        }
                        else
                        {
                            return; // Usuario canceló
                        }
                    }
                }

                const long ONE_GB = 1073741824;

                // Si hay más de 1GB solo de JPGs, sugerir borrar
                if (_pngCount == 0 && _screenshotTotalSize > ONE_GB)
                {
                    var result = MessageBox.Show(
                        es ? $"Tienes {FormatFileSize(_screenshotTotalSize)} de capturas.\n\n" +
                             "Se recomienda borrar capturas antiguas para liberar espacio.\n\n" +
                             "¿Quieres abrir la carpeta de capturas para borrarlas manualmente?"
                           : $"You have {FormatFileSize(_screenshotTotalSize)} of screenshots.\n\n" +
                             "It's recommended to delete old screenshots to free up space.\n\n" +
                             "Do you want to open the screenshots folder to delete them manually?",
                        "FPS Booster",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = _screenshotsFolder,
                            UseShellExecute = false
                        });
                    }
                    return;
                }

                // Si hay PNGs, ofrecer comprimir
                if (_pngCount > 0)
                {
                    var result = MessageBox.Show(
                        es ? $"Se comprimirán {_pngCount} capturas PNG a JPG.\n\n" +
                             "Esto puede tomar un tiempo.\n\n¿Continuar?"
                           : $"This will compress {_pngCount} PNG screenshots to JPG.\n\n" +
                             "This may take a while.\n\nContinue?",
                        "FPS Booster",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    // Preguntar si quiere eliminar los originales
                    var deleteOriginals = MessageBox.Show(
                        es ? "¿Te gustaría eliminar permanentemente las fotos PNG originales después de comprimirlas?\n\n" +
                             "• SÍ: Las fotos PNG originales serán eliminadas después de la compresión\n" +
                             "• NO: Las fotos PNG originales se mantendrán como respaldo"
                           : "Would you like to permanently delete the original PNG photos after compressing them?\n\n" +
                             "• YES: Original PNG photos will be deleted after compression\n" +
                             "• NO: Original PNG photos will be kept as backup",
                        es ? "Eliminar Originales" : "Delete Originals",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    // Descargar compresor
                    await DownloadCompressor();

                    if (string.IsNullOrEmpty(_compressorPath) || !File.Exists(_compressorPath))
                    {
                        MessageBox.Show(
                            es ? "Compresor no disponible." : "Compressor not available.",
                            es ? "Error" : "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // Construir argumentos según la elección del usuario
                    string arguments = deleteOriginals == MessageBoxResult.Yes
                        ? "-in ./ -out ./ -q 80 -deletefinal"
                        : "-in ./ -out ./ -q 80";

                    bool compressionSuccess = false;

                    await Task.Run(() =>
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = _compressorPath,
                            Arguments = arguments,
                            WorkingDirectory = _screenshotsFolder,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Process process = Process.Start(psi);
                        process.WaitForExit();

                        // Si el proceso terminó con código 0, fue exitoso
                        compressionSuccess = process.ExitCode == 0;
                    });

                    // Eliminar el .exe después de la compresión exitosa
                    if (compressionSuccess && File.Exists(_compressorPath))
                    {
                        try
                        {
                            File.Delete(_compressorPath);
                            _compressorPath = ""; // Resetear la ruta
                        }
                        catch
                        {
                            // Si no se puede eliminar, no es crítico
                        }
                    }

                    // Re-escanear
                    await Task.Run(() => PerformScan());
                    UpdateUI();

                    string successMessage = deleteOriginals == MessageBoxResult.Yes
                        ? (es ? "¡Todas las fotos comprimidas exitosamente!\n\nLas fotos originales han sido eliminadas."
                              : "All photos compressed successfully!\n\nOriginal photos have been deleted.")
                        : (es ? "¡Todas las fotos comprimidas exitosamente!\n\nLas fotos originales se mantuvieron como respaldo."
                              : "All photos compressed successfully!\n\nOriginal photos were kept as backup.");

                    MessageBox.Show(
                        successMessage,
                        es ? "Éxito" : "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                MessageBox.Show(
                    $"{(es ? "Error: " : "Error: ")}{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // NUEVO: Fix Show Mod List on Startup
        private async void ModListFixBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

                // Si no se encontró Options.ini, pedir al usuario que seleccione la carpeta
                if (string.IsNullOrEmpty(_optionsIniPath) || !File.Exists(_optionsIniPath))
                {
                    using (var dialog = new WinForms.FolderBrowserDialog())
                    {
                        dialog.Description = es
                            ? "Selecciona la carpeta de documentos de The Sims 4 (donde está Options.ini)"
                            : "Select The Sims 4 documents folder (where Options.ini is located)";

                        if (!string.IsNullOrEmpty(_sims4Folder) && Directory.Exists(_sims4Folder))
                            dialog.SelectedPath = _sims4Folder;

                        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                        {
                            _sims4Folder = dialog.SelectedPath;
                            _optionsIniPath = Path.Combine(_sims4Folder, "Options.ini");

                            if (!File.Exists(_optionsIniPath))
                            {
                                MessageBox.Show(
                                    es ? "No se encontró Options.ini en la carpeta seleccionada."
                                       : "Options.ini not found in the selected folder.",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }
                        }
                        else
                        {
                            return; // Usuario canceló
                        }
                    }
                }

                // Leer el archivo
                string[] lines = File.ReadAllLines(_optionsIniPath);
                bool found = false;
                List<string> newLines = new List<string>();

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("showmodliststartup", StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        // Reemplazar con valor 0
                        newLines.Add("showmodliststartup = 0");
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }

                // Si no se encontró la línea, agregarla al final
                if (!found)
                {
                    newLines.Add("showmodliststartup = 0");
                }

                // Escribir el archivo
                File.WriteAllLines(_optionsIniPath, newLines);

                _modListIssue = false;
                UpdateUI();

                MessageBox.Show(
                    es ? "¡Configuración actualizada!\n\nLa lista de mods ya no se mostrará al iniciar el juego.\nEsto mejorará significativamente los tiempos de carga."
                       : "Configuration updated!\n\nMod list will no longer show on game startup.\nThis will significantly improve loading times.",
                    "FPS Booster",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                MessageBox.Show(
                    $"{(es ? "Error: " : "Error: ")}{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DownloadCompressor()
        {
            try
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(_compressorPath) && File.Exists(_compressorPath))
                    return;

                _compressorPath = Path.Combine(_screenshotsFolder, "leuan-compressor.exe");

                if (File.Exists(_compressorPath))
                    return;

                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync(COMPRESSOR_URL);
                    File.WriteAllBytes(_compressorPath, data);
                }
            }
            catch (Exception ex)
            {
                bool es = _languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                MessageBox.Show(
                    $"{(es ? "Error descargando compresor: " : "Error downloading compressor: ")}{ex.Message}",
                    es ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OptimizeAllButton_Click(object sender, RoutedEventArgs e)
        {
            // Fix mod list first (CRÍTICO)
            if (_modListIssue)
                ModListFixBtn_Click(null, null);

            // Fix cache
            if (_cacheIssue)
                CacheFixBtn_Click(null, null);

            // Fix special chars - Abrir renombrador
            if (_charsIssue)
                CharsFixBtn_Click(null, null);

            // Fix screenshots
            if (_screenshotIssue)
                ScreenshotFixBtn_Click(null, null);

            // Show info for merge and mod
            if (_mergeIssue)
                MergeInfoBtn_Click(null, null);

            if (_modMissing)
                ModDownloadBtn_Click(null, null);
        }

    }
}