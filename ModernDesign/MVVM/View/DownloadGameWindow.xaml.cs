using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ModernDesign.MVVM.View
{
    public partial class DownloadGameWindow : Window
    {
        // URLs de las 7 partes del juego
        private static readonly List<string> GAME_PARTS_URLS = new List<string>
        {
            "https://leuan.zeroauno.com/sims4-toolkit/sims4fullgame.html"
        };

        public DownloadGameWindow()
        {
            InitializeComponent();
            LoadLanguage();

            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;

        }

        private void LoadLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                HeaderTitle.Text = "📦 Descargar The Sims 4 - Juego Completo";
                HeaderSubtitle.Text = "Juego base completo + todas las actualizaciones (Versión Crackeada)";

                InfoTitle.Text = "📋 ¿Qué Incluye?";
                Info1.Text = "✓ The Sims 4 Juego Base (Última Versión)";
                Info2.Text = "✓ Todas las Actualizaciones y Parches Oficiales";
                Info3.Text = "✓ Pre-configurado y Listo para Jugar";
                Info4.Text = "✓ No Requiere Origin/EA App";
                Info5.Text = "✓ Versión Portable (No Necesita Instalación)";

                PartsTitle.Text = "📥 Partes de Descarga (7 archivos):";
                PartsInfo.Text = "Necesitas descargar TODAS las 7 partes para extraer el juego:";

                ReqTitle.Text = "💾 Requisitos:";
                Req1.Text = "• Tamaño Total de Descarga: ~70 GB (7 partes)";
                Req2.Text = "• Espacio en Disco Requerido: ~100 GB (después de extraer)";
                Req3.Text = "• Conexión a Internet Estable Recomendada";
                Req4.Text = "• Tiempo Estimado: 3-8 horas (dependiendo de tu velocidad)";

                ExtractTitle.Text = "📂 Cómo Extraer:";
                ExtractInstructions.Text =
                    "1. Descarga TODAS las 7 partes en la MISMA carpeta\n" +
                    "2. Haz clic derecho en 'The Sims 4.zip' (Parte 1)\n" +
                    "3. Selecciona 'Extraer aquí' o 'Extraer en The Sims 4\\'\n" +
                    "4. Espera a que se complete la extracción\n" +
                    "5. ¡Ejecuta el juego desde la carpeta extraída!";

                WarningTitle.Text = "⚠️ Aviso Importante";
                WarningText.Text = "Esta es una versión crackeada/portable de The Sims 4. " +
                                   "Después de descargar y extraer, podrás instalar todos los DLCs usando este ToolKit. " +
                                   "Asegúrate de tener suficiente espacio en disco antes de continuar.";

                TipTitle.Text = "💡 ¿Sabías que...?";
                TipText.Text = "¡Puedes saltarte el proceso de descarga y extraer los archivos directamente a tu escritorio!";
                SeeHowText.Text = "¡Mira cómo!";

                DownloadBtn.Content = "⬇️ Descargar Todas las Partes";
                CancelBtn.Content = "Cancelar";
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

        private void SeeHowLink_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            string message = isSpanish
                ? "✨ BENEFICIO EXCLUSIVO PARA SUPPORTERS ✨\n\n" +
                  "Los Supporters tienen acceso a un cloud premium, donde nosotros almacenamos los .zips, " +
                  "teniéndolos \"predescargados\".\n\n" +
                  "Tú, al acceder a este drive, podrás extraerlos directamente a tu escritorio, " +
                  "saltándote el paso de descargarlos completamente y ahorrándote infinidad de tiempo!\n\n" +
                  "💎 ¿Quieres convertirte en Supporter?\n" +
                  "Visita: https://ko-fi.com/leuan"
                : "✨ EXCLUSIVE BENEFIT FOR SUPPORTERS ✨\n\n" +
                  "Supporters have access to a premium cloud, where we store the .zips, " +
                  "having them \"pre-downloaded\".\n\n" +
                  "By accessing this drive, you can extract them directly to your desktop, " +
                  "skipping the download step completely and saving you tons of time!\n\n" +
                  "💎 Want to become a Supporter?\n" +
                  "Visit: https://ko-fi.com/leuan";

            string title = isSpanish ? "💎 Beneficio Premium" : "💎 Premium Benefit";

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            try
            {
                // Deshabilitar botón mientras se abren los links
                DownloadBtn.IsEnabled = false;
                DownloadBtn.Content = isSpanish ? "⏳ Abriendo enlaces..." : "⏳ Opening links...";

                // Abrir todas las 7 URLs en el navegador con un pequeño delay entre cada una
                foreach (var url in GAME_PARTS_URLS)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = url,
                        UseShellExecute = false
                    });

                    // Pequeño delay para no saturar el navegador
                    await Task.Delay(500);
                }

                // Restaurar botón
                DownloadBtn.Content = isSpanish ? "⬇️ Descargar Todas las Partes" : "⬇️ Download All Parts";
                DownloadBtn.IsEnabled = true;

                // Mostrar mensaje de confirmación
                string message = isSpanish
                    ? " Se abrió en tu navegador para descargar todas las partes.\n\n" +
                      "IMPORTANTE:\n" +
                      "1. Descarga TODAS las 7 partes en la MISMA carpeta\n" +
                      "2. Espera a que todas terminen de descargar\n" +
                      "3. Haz clic derecho en 'The Sims 4.zip' (Parte 1)\n" +
                      "4. Selecciona 'Extraer aquí'\n" +
                      "5. Ejecuta el juego desde la carpeta extraída\n" +
                      "6. Vuelve a este ToolKit para instalar DLCs\n\n" +
                      "¿Estas listo?"
                    : " Website opened in your browser to download all parts.\n\n" +
                      "IMPORTANT:\n" +
                      "1. Download ALL 7 parts to the SAME folder\n" +
                      "2. Wait for all downloads to complete\n" +
                      "3. Right-click on 'The Sims 4.zip' (Part 1)\n" +
                      "4. Select 'Extract Here'\n" +
                      "5. Run the game from the extracted folder\n" +
                      "6. Return to this ToolKit to install DLCs\n\n" +
                      "Are you done?";

                string title = isSpanish ? "Descargas Iniciadas" : "Downloads Started";

                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    this.Close();
                }
                else
                {
                    // Cerrar todo
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                // Restaurar botón en caso de error
                DownloadBtn.Content = isSpanish ? "⬇️ Descargar Todas las Partes" : "⬇️ Download All Parts";
                DownloadBtn.IsEnabled = true;

                string errorMsg = isSpanish
                    ? $"Error al abrir los enlaces de descarga:\n{ex.Message}"
                    : $"Error opening download links:\n{ex.Message}";

                MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }


        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = IsSpanishLanguage();

            string message = isSpanish
                ? "¿Estás seguro de que deseas cancelar?\n\nSin el juego base no podrás usar el ToolKit."
                : "Are you sure you want to cancel?\n\nWithout the base game you won't be able to use the ToolKit.";

            string title = isSpanish ? "Confirmar Cancelación" : "Confirm Cancellation";

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }
}