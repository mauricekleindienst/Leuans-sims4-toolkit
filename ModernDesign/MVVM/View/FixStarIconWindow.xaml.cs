using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class FixStarIconWindow : Window
    {
        private string _reticulatedFilePath;
        private bool _isSpanish = false;

        public FixStarIconWindow()
        {
            InitializeComponent();
            LoadLanguage();
            ApplyLanguage();
            CheckReticulatedFile();
        }

        private void LoadLanguage()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string languagePath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");

                if (File.Exists(languagePath))
                {
                    foreach (var line in File.ReadAllLines(languagePath))
                    {
                        if (line.Trim().StartsWith("Language", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split('=');
                            if (parts.Length >= 2)
                            {
                                _isSpanish = parts[1].Trim().ToLower().Contains("es");
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void ApplyLanguage()
        {
            if (_isSpanish)
            {
                TitleText.Text = "⭐ Arreglar Estrella/Ícono Nuevo";
                SubtitleText.Text = "Elimina la estrella permanente de 'nuevo' en objetos del modo construcción";

                StatusTitle.Text = "📋 Estado";

                AutoTitle.Text = "🤖 Método Automático (Recomendado)";
                AutoDesc.Text = "Encontraremos y parchearemos automáticamente tu archivo ReticulatedSplinesView.";
                AutoFixBtn.Content = "✨ Arreglar Automáticamente";

                ManualTitle.Text = "🔧 Método Manual (Avanzado)";
                ManualDesc.Text = "Hazlo tú mismo usando un editor hexadecimal. Sigue estos pasos:";

                ManualStep1.Text = "Descarga HxD (editor hexadecimal gratuito)";
                DownloadHxDBtn.Content = "⬇️ Descargar HxD";

                ManualStep2.Text = "Ubica 'ReticulatedSplinesView' en tu carpeta de documentos de Sims 4";
                OpenFolderBtn.Content = "📁 Abrir Carpeta Sims 4";

                ManualStep3.Text = "Click derecho en el archivo → Abrir con → HxD64.exe";
                ManualStep4.Text = "Presiona Ctrl+R → Buscar '1D 10 01' → Reemplazar con '1D 10 04' → Tipo de dato: Hex-values → Reemplazar Todo";
                ManualStep5.Text = "Guarda el archivo (Ctrl+S) y reinicia tu juego";

                InstructionsTitle.Text = "📖 Cómo funciona";
                Step1Text.Text = "1. Ubicamos tu archivo 'ReticulatedSplinesView' en tu carpeta 'The Sims 4'";
                Step2Text.Text = "2. Parcheamos el archivo reemplazando los valores hex '1D 10 01' con '1D 10 04'";
                Step3Text.Text = "3. Guardamos el archivo y reinicias tu juego";
                Step4Text.Text = "4. ¡La estrella de 'nuevo' debería desaparecer!";

                CloseBtn.Content = "Cerrar";
            }
        }

        private void CheckReticulatedFile()
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] possiblePaths = new[]
                {
                    Path.Combine(docs, "Electronic Arts", "The Sims 4", "ReticulatedSplinesView"),
                    Path.Combine(docs, "Electronic Arts", "Los Sims 4", "ReticulatedSplinesView")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _reticulatedFilePath = path;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(_reticulatedFilePath))
                {
                    StatusText.Text = _isSpanish
                        ? $"✅ Archivo encontrado:\n{_reticulatedFilePath}"
                        : $"✅ File found:\n{_reticulatedFilePath}";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"));
                    AutoFixBtn.IsEnabled = true;
                }
                else
                {
                    StatusText.Text = _isSpanish
                        ? "⚠️ No se encontró el archivo ReticulatedSplinesView automáticamente.\nUsa el método manual para subirlo."
                        : "⚠️ ReticulatedSplinesView file not found automatically.\nUse the manual method to upload it.";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
                    AutoFixBtn.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = _isSpanish
                    ? $"❌ Error al buscar el archivo:\n{ex.Message}"
                    : $"❌ Error searching for file:\n{ex.Message}";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
                AutoFixBtn.IsEnabled = false;
            }
        }

        private void AutoFixBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_reticulatedFilePath) || !File.Exists(_reticulatedFilePath))
            {
                MessageBox.Show(
                    _isSpanish
                        ? "No se encontró el archivo ReticulatedSplinesView."
                        : "ReticulatedSplinesView file not found.",
                    _isSpanish ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                _isSpanish
                    ? $"¿Parchear el archivo?\n\n{_reticulatedFilePath}\n\nSe creará un respaldo automáticamente."
                    : $"Patch the file?\n\n{_reticulatedFilePath}\n\nA backup will be created automatically.",
                _isSpanish ? "Confirmar" : "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PatchFile(_reticulatedFilePath);
            }
        }

        private void DownloadHxDBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://mh-nexus.de/en/downloads.php?product=HxD20",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _isSpanish
                        ? $"No se pudo abrir el enlace:\n{ex.Message}"
                        : $"Could not open link:\n{ex.Message}",
                    _isSpanish ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] possiblePaths = new[]
                {
                    Path.Combine(docs, "Electronic Arts", "The Sims 4"),
                    Path.Combine(docs, "Electronic Arts", "Los Sims 4")
                };

                string folderToOpen = null;
                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        folderToOpen = path;
                        break;
                    }
                }

                if (folderToOpen != null)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = folderToOpen,
                        UseShellExecute = false
                    });
                }
                else
                {
                    MessageBox.Show(
                        _isSpanish
                            ? "No se encontró la carpeta de The Sims 4."
                            : "The Sims 4 folder not found.",
                        _isSpanish ? "Error" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _isSpanish
                        ? $"No se pudo abrir la carpeta:\n{ex.Message}"
                        : $"Could not open folder:\n{ex.Message}",
                    _isSpanish ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PatchFile(string filePath)
        {
            try
            {
                // Crear backup
                string backupPath = filePath + ".backup";
                File.Copy(filePath, backupPath, true);

                // Leer archivo
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Buscar y reemplazar patrón hex: 1D 10 01 -> 1D 10 04
                byte[] searchPattern = new byte[] { 0x1D, 0x10, 0x01 };
                byte[] replacePattern = new byte[] { 0x1D, 0x10, 0x04 };

                int replacements = 0;
                for (int i = 0; i <= fileBytes.Length - searchPattern.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < searchPattern.Length; j++)
                    {
                        if (fileBytes[i + j] != searchPattern[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        for (int j = 0; j < replacePattern.Length; j++)
                        {
                            fileBytes[i + j] = replacePattern[j];
                        }
                        replacements++;
                        i += searchPattern.Length - 1;
                    }
                }

                if (replacements > 0)
                {
                    // Guardar archivo parcheado
                    File.WriteAllBytes(filePath, fileBytes);

                    MessageBox.Show(
                        _isSpanish
                            ? $"✅ ¡Archivo parcheado exitosamente!\n\nSe realizaron {replacements} reemplazo(s).\n\nRespaldo creado en:\n{backupPath}\n\nReinicia tu juego para ver los cambios."
                            : $"✅ File patched successfully!\n\n{replacements} replacement(s) made.\n\nBackup created at:\n{backupPath}\n\nRestart your game to see the changes.",
                        _isSpanish ? "Éxito" : "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    StatusText.Text = _isSpanish
                        ? $"✅ Archivo parcheado exitosamente ({replacements} reemplazo(s))"
                        : $"✅ File patched successfully ({replacements} replacement(s))";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"));
                }
                else
                {
                    MessageBox.Show(
                        _isSpanish
                            ? "⚠️ No se encontraron patrones para reemplazar.\n\nEl archivo ya podría estar parcheado o no es el archivo correcto."
                            : "⚠️ No patterns found to replace.\n\nThe file might already be patched or is not the correct file.",
                        _isSpanish ? "Advertencia" : "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _isSpanish
                        ? $"❌ Error al parchear el archivo:\n{ex.Message}"
                        : $"❌ Error patching file:\n{ex.Message}",
                    _isSpanish ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
