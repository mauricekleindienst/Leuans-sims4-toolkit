using ModernDesign.MVVM.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ModernDesign
{
    public partial class InstallModeSelector : Window
    {
        public InstallModeSelector()
        {
            InitializeComponent();

            // Cargar links por defecto primero
            SetDefaultLinks();

            // Cargar links dinámicamente
            Loaded += async (s, e) => await LoadTutorialLinksAsync();

            ApplyLanguage();

            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        //  DICCIONARIO PARA ALMACENAR LOS LINKS
        private Dictionary<string, string> _tutorialLinks = new Dictionary<string, string>();
        private readonly HttpClient _httpClient = new HttpClient();

        //  MÉTODO PARA CARGAR LOS LINKS DESDE EL ARCHIVO REMOTO
        private async Task LoadTutorialLinksAsync()
        {
            try
            {
                string url = "https://zeroauno.blob.core.windows.net/leuan/Public/links.txt";
                string content = await _httpClient.GetStringAsync(url);

                // Parsear el contenido
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Contains("="))
                    {
                        var parts = trimmed.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            _tutorialLinks[key] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Si falla la carga, usar URLs por defecto
                System.Diagnostics.Debug.WriteLine($"Error loading tutorial links: {ex.Message}");
                SetDefaultLinks();
            }
        }

        //  MÉTODO PARA ESTABLECER LINKS POR DEFECTO (FALLBACK)
        private void SetDefaultLinks()
        {
            _tutorialLinks["tutorialAutomatico"] = "https://youtu.be/GeTuyL89JOM?si=siu_WW92ecFKF-df&t=72s";
            _tutorialLinks["tutorialManual"] = "https://www.youtube.com/watch?v=TF0EBobPWdc";
            _tutorialLinks["legitInstall"] = "https://www.youtube.com/watch?v=YOUR_VIDEO_ID_HERE";
            _tutorialLinks["manualInstall"] = "https://www.youtube.com/watch?v=YOUR_VIDEO_ID_HERE";
            _tutorialLinks["manualInstall2"] = "https://www.youtube.com/watch?v=YOUR_VIDEO_ID_HERE";
        }

        //  MÉTODO PARA OBTENER UN LINK (CON FALLBACK)
        private string GetTutorialLink(string key, string defaultUrl = "")
        {
            if (_tutorialLinks.ContainsKey(key))
            {
                return _tutorialLinks[key];
            }
            return defaultUrl;
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                TitleText.Text = "Elige Tu Método";
                SubtitleText.Text = "Selecciona cómo deseas instalar los DLC's";

                // Automatic
                AutomaticTitle.Text = "Automático";
                AutomaticDesc.Text = "Déjanos encargarnos de todo por ti";

                // Manual
                ManualTitle.Text = "Manual";
                ManualDesc.Text = "Control completo sobre cada detalle";
            }
            else
            {
                TitleText.Text = "Choose Your Method";
                SubtitleText.Text = "Select how you want to install the DLC's";

                // Automatic
                AutomaticTitle.Text = "Automatic";
                AutomaticDesc.Text = "Let us handle everything for you";

                // Manual
                ManualTitle.Text = "Manual";
                ManualDesc.Text = "Complete control over every detail, manual download, manual installation, but we'll guide you through.";
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

        private void AutomaticBtn_Click(object sender, MouseButtonEventArgs e)
        {
            ShowTutorialPromptLocal(new UpdaterWindow());
        }

        private void SemiAutomaticBtn_Click(object sender, MouseButtonEventArgs e)
        {
            string tutorialUrl = GetTutorialLink("tutorialManual", "https://www.youtube.com/watch?v=TF0EBobPWdc");
            ShowTutorialPrompt(tutorialUrl, new SemiAutoInstallerWindow());
        }

        private void ShowTutorialPromptLocal(Window targetWindow)
        {
            bool isSpanish = IsSpanishLanguage();

            string message = isSpanish
                ? "¿Te gustaría ver el tutorial de instalación automática?"
                : "Would you like to see the automatic installation tutorial?";

            string caption = isSpanish
                ? "Tutorial de Instalación Automática"
                : "Automatic Installation Tutorial";

            MessageBoxResult result = MessageBox.Show(
                message,
                caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Crear carpeta FAQ si no existe
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string faqFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "FAQ");

                    if (!Directory.Exists(faqFolder))
                    {
                        Directory.CreateDirectory(faqFolder);
                    }

                    string htmlPath = Path.Combine(faqFolder, "automatic_installation_tutorial.html");

                    // Crear el archivo HTML si no existe
                    if (!File.Exists(htmlPath))
                    {
                        CreateTutorialHTML(htmlPath);
                    }

                    // Abrir el HTML en el navegador predeterminado
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = htmlPath,
                        UseShellExecute = false
                    });
                }
                catch (Exception ex)
                {
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

            // SIEMPRE ABRIR LA VENTANA OBJETIVO
            OpenWindow(targetWindow);
        }

        private void CreateTutorialHTML(string filePath)
        {
            string htmlContent = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Automatic Installation Tutorial - Leuan's Sims 4 Toolkit</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600;700&display=swap');

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 40px 20px;
            color: #333;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 30px;
            box-shadow: 0 30px 80px rgba(0, 0, 0, 0.3);
            overflow: hidden;
        }

        .hero {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 80px 40px;
            text-align: center;
            color: white;
            position: relative;
            overflow: hidden;
        }

        .hero::before {
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle, rgba(255,255,255,0.1) 1px, transparent 1px);
            background-size: 50px 50px;
            animation: moveBackground 20s linear infinite;
        }

        @keyframes moveBackground {
            0% { transform: translate(0, 0); }
            100% { transform: translate(50px, 50px); }
        }

        .hero h1 {
            font-size: 3.5em;
            font-weight: 700;
            margin-bottom: 15px;
            position: relative;
            z-index: 1;
            text-shadow: 2px 2px 10px rgba(0, 0, 0, 0.3);
            letter-spacing: -1px;
        }

        .hero p {
            font-size: 1.3em;
            opacity: 0.95;
            position: relative;
            z-index: 1;
            font-weight: 300;
        }

        .language-selector {
            position: absolute;
            top: 30px;
            right: 40px;
            z-index: 10;
            display: flex;
            gap: 10px;
        }

        .language-selector a {
            background: rgba(255, 255, 255, 0.2);
            border: 2px solid rgba(255, 255, 255, 0.5);
            color: white;
            padding: 12px 25px;
            border-radius: 30px;
            text-decoration: none;
            font-weight: 600;
            transition: all 0.3s ease;
            backdrop-filter: blur(10px);
        }

        .language-selector a:hover {
            background: rgba(255, 255, 255, 0.3);
            transform: translateY(-3px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
        }

        .content {
            padding: 60px 40px;
        }

        .intro {
            text-align: center;
            margin-bottom: 60px;
            padding: 0 20px;
        }

        .intro h2 {
            font-size: 2.5em;
            color: #667eea;
            margin-bottom: 20px;
            font-weight: 700;
        }

        .intro p {
            font-size: 1.2em;
            color: #666;
            line-height: 1.8;
            max-width: 800px;
            margin: 0 auto;
        }

        .step {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 40px;
            align-items: center;
            margin-bottom: 80px;
            padding: 40px;
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            border-radius: 25px;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s ease, box-shadow 0.3s ease;
        }

        .step:hover {
            transform: translateY(-10px);
            box-shadow: 0 20px 50px rgba(0, 0, 0, 0.15);
        }

        .step:nth-child(even) {
            grid-template-columns: 1fr 1fr;
        }

        .step:nth-child(even) .step-image {
            order: 2;
        }

        .step:nth-child(even) .step-content {
            order: 1;
        }

        .step-content {
            padding: 20px;
        }

        .step-number {
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            width: 60px;
            height: 60px;
            border-radius: 50%;
            text-align: center;
            line-height: 60px;
            font-weight: 700;
            font-size: 1.8em;
            margin-bottom: 20px;
            box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
        }

        .step h3 {
            font-size: 2em;
            color: #333;
            margin-bottom: 15px;
            font-weight: 700;
        }

        .step p {
            color: #555;
            line-height: 1.9;
            font-size: 1.1em;
            margin-bottom: 15px;
        }

        .step ul {
            margin-left: 25px;
            margin-top: 15px;
        }

        .step ul li {
            color: #555;
            line-height: 2;
            font-size: 1.05em;
            margin-bottom: 8px;
        }

        .step-image {
            position: relative;
            border-radius: 20px;
            overflow: hidden;
            box-shadow: 0 15px 40px rgba(0, 0, 0, 0.2);
        }

        .step-image img {
            width: 100%;
            height: auto;
            display: block;
            border-radius: 20px;
        }

        .warning {
            background: linear-gradient(135deg, #ffeaa7 0%, #fdcb6e 100%);
            border-left: 8px solid #e17055;
            padding: 30px;
            border-radius: 15px;
            margin: 60px 0;
            box-shadow: 0 10px 30px rgba(225, 112, 85, 0.2);
        }

        .warning h3 {
            color: #d63031;
            font-size: 1.8em;
            margin-bottom: 15px;
            font-weight: 700;
        }

        .warning p {
            color: #2d3436;
            font-size: 1.15em;
            line-height: 1.8;
        }

        .success {
            background: linear-gradient(135deg, #55efc4 0%, #00b894 100%);
            color: white;
            padding: 50px;
            border-radius: 25px;
            text-align: center;
            margin-top: 60px;
            box-shadow: 0 15px 40px rgba(0, 184, 148, 0.3);
        }

        .success h2 {
            font-size: 3em;
            margin-bottom: 20px;
            font-weight: 700;
        }

        .success p {
            font-size: 1.3em;
            line-height: 1.8;
        }

        .footer {
            background: #2d3436;
            color: white;
            text-align: center;
            padding: 40px;
            font-size: 1em;
        }

        .footer a {
            color: #74b9ff;
            text-decoration: none;
            font-weight: 600;
        }

        .footer a:hover {
            text-decoration: underline;
        }

        code {
            background: rgba(102, 126, 234, 0.15);
            padding: 5px 12px;
            border-radius: 8px;
            font-family: 'Courier New', monospace;
            color: #667eea;
            font-weight: 600;
            font-size: 0.95em;
        }

        @media (max-width: 768px) {
            .step {
                grid-template-columns: 1fr;
            }

            .step:nth-child(even) .step-image {
                order: 1;
            }

            .step:nth-child(even) .step-content {
                order: 2;
            }

            .hero h1 {
                font-size: 2.5em;
            }

            .language-selector {
                position: static;
                justify-content: center;
                margin-bottom: 20px;
            }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <!-- HERO SECTION -->
        <div class=""hero"">
            <div class=""language-selector"">
                <a href=""#english"">🇬🇧 English</a>
                <a href=""#spanish"">🇪🇸 Español</a>
            </div>
            <h1>✨ Automatic Installation Tutorial</h1>
            <p>Your complete guide to installing DLCs effortlessly</p>
        </div>

        <!-- ENGLISH VERSION -->
        <div class=""content"" id=""english"">
            <div class=""intro"">
                <h2>🚀 Get Started in 5 Easy Steps</h2>
                <p>Follow this simple guide to automatically download and install all your favorite Sims 4 DLCs. No technical knowledge required!</p>
            </div>

            <!-- STEP 1 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">1</div>
                    <h3>Open Sims 4 Updater</h3>
                    <p>Launch <strong>Leuan's Sims 4 Toolkit</strong> from your desktop and click on the <code>Install All DLC's</code> button in the main menu.</p>
                    <p>A window will pop-up, making you choose between Automatic / Manual, you have to choose <strong>Automatic</strong>.</p>
                    <p>This will open the automatic installation window where you can manage all your DLCs.</p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step1_automatic.png"" alt=""Open S4 Updater"">
                </div>
            </div>

            <!-- STEP 2 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">2</div>
                    <h3>Auto-Detect Game Path</h3>
                    <p>Click the <code>Auto</code> button to automatically find your Sims 4 installation folder.</p>
                    <p>The toolkit will read your Windows Registry and detect the correct path instantly. If it fails, you can use the <code>Browse</code> button to select it manually.</p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step2_automatic.png"" alt=""Auto-Detect Game Path"">
                </div>
            </div>

            <!-- STEP 3 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">3</div>
                    <h3>Select Your DLCs</h3>
                    <p>Browse the complete DLC list and check the boxes next to the content you want to install.</p>
                    <p><strong>Quick actions:</strong></p>
                    <ul>
                        <li><code>Select All</code> - Install all 106 DLCs at once</li>
                        <li><code>Deselect All</code> - Uncheck everything</li>
                        <li><strong>Search bar</strong> - Find specific DLCs by name or code</li>
                    </ul>
                    <p>After selecting your DLC's, please press <code>Download Selected</code></p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step3_automatic.png"" alt=""Select DLCs"">
                </div>
            </div>

            <!-- WARNING -->
            <div class=""warning"">
                <h3>⚠️ Before You Start</h3>
                <p><strong>Important:</strong> Make sure you have at least <strong>50 GB</strong> of free disk space. Close Origin/EA App completely to avoid file conflicts during installation.</p>
            </div>

            <!-- STEP 4 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">4</div>
                    <h3>Wait for Completion</h3>
                    <p>Sit back and relax! The download and installation will run automatically.</p>
                    <p>Watch the <strong>Goku Kamehameha progress bar</strong> 🔥 as your DLCs install. Do not close the application until you see the completion message.</p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step4_automatic.png"" alt=""Installation Complete"">
                </div>
            </div>

            <!-- SUCCESS -->
            <div class=""success"">
                <h2>🎉 Congratulations!</h2>
                <p>Your DLCs have been successfully installed!</p>
                <p style=""margin-top: 20px; font-size: 1.4em;""><strong>💰 Check your total game value in the toolkit!</strong></p>
                <p style=""margin-top: 15px; opacity: 0.9;"">Launch The Sims 4 and enjoy your new content!</p>
            </div>
        </div>

        <!-- SPANISH VERSION -->
        <div class=""content"" id=""spanish"">
            <div class=""intro"">
                <h2>🚀 Comienza en 4 Pasos Fáciles</h2>
                <p>Sigue esta guía simple para descargar e instalar automáticamente todos tus DLCs favoritos de Los Sims 4. ¡No se requiere conocimiento técnico!</p>
            </div>

            <!-- PASO 1 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">1</div>
                    <h3>Abrir Actualizador de Sims 4</h3>
                    <p>Inicia <strong>Leuan's Sims 4 Toolkit</strong> desde tu escritorio y haz clic en el botón <code>Install All DLC's</code> en el menú principal.</p>
                    <p>Aparecerá una ventana que te hará elegir entre Automático / Manual, debes elegir <strong>Automático</strong>.</p>
                    <p>Esto abrirá la ventana de instalación automática donde puedes gestionar todos tus DLCs.</p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step1_automatic.png"" alt=""Abrir Actualizador S4"">
                </div>
            </div>

            <!-- PASO 2 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">2</div>
                    <h3>Detectar Ruta Automáticamente</h3>
                    <p>Haz clic en el botón <code>Auto</code> para encontrar automáticamente la carpeta de instalación de Los Sims 4.</p>
                    <p>El toolkit leerá el Registro de Windows y detectará la ruta correcta al instante. Si falla, puedes usar el botón <code>Browse</code> para seleccionarla manualmente.</p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step2_automatic.png"" alt=""Detectar Ruta del Juego"">
                </div>
            </div>

            <!-- PASO 3 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">3</div>
                    <h3>Seleccionar tus DLCs</h3>
                    <p>Navega por la lista completa de DLCs y marca las casillas junto al contenido que deseas instalar.</p>
                    <p><strong>Acciones rápidas:</strong></p>
                    <ul>
                        <li><code>Select All</code> - Instalar los 106 DLCs de una vez</li>
                        <li><code>Deselect All</code> - Desmarcar todo</li>
                        <li><strong>Barra de búsqueda</strong> - Encontrar DLCs específicos por nombre o código</li>
                    </ul>
                    <p>Después de seleccionar tus DLCs, presiona <code>Download Selected</code></p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step3_automatic.png"" alt=""Seleccionar DLCs"">
                </div>
            </div>

            <!-- ADVERTENCIA -->
            <div class=""warning"">
                <h3>⚠️ Antes de Comenzar</h3>
                <p><strong>Importante:</strong> Asegúrate de tener al menos <strong>50 GB</strong> de espacio libre en disco. Cierra Origin/EA App completamente para evitar conflictos de archivos durante la instalación.</p>
            </div>

            <!-- PASO 4 -->
            <div class=""step"">
                <div class=""step-content"">
                    <div class=""step-number"">4</div>
                    <h3>Esperar a que Termine</h3>
                    <p>¡Relájate y espera! La descarga e instalación se ejecutará automáticamente.</p>
                    <p>Observa la <strong>barra de progreso Kamehameha de Goku</strong> 🔥 mientras se instalan tus DLCs. No cierres la aplicación hasta que veas el mensaje de finalización.</p>
                </div>
                <div class=""step-image"">
                    <img src=""https://github.com/Leuansin/leuan-dlcs/releases/download/tutorials_imgs/step4_automatic.png"" alt=""Instalación Completa"">
                </div>
            </div>

            <!-- ÉXITO -->
            <div class=""success"">
                <h2>🎉 ¡Felicitaciones!</h2>
                <p>¡Tus DLCs se han instalado exitosamente!</p>
                <p style=""margin-top: 20px; font-size: 1.4em;""><strong>💰 ¡Revisa el valor total de tu juego en el toolkit!</strong></p>
                <p style=""margin-top: 15px; opacity: 0.9;"">¡Inicia Los Sims 4 y disfruta de tu nuevo contenido!</p>
            </div>
        </div>

        <!-- FOOTER -->
        <div class=""footer"">
            <p>Made with ❤️ by <strong>Leuan</strong> | <a href=""https://discord.gg/JYnpPt4nUu"" target=""_blank"">Discord</a></p>
            <p style=""margin-top: 10px; opacity: 0.7;"">Leuan's Sims 4 Toolkit © 2025-2026</p>
        </div>
    </div>
</body>
</html>";

            File.WriteAllText(filePath, htmlContent);
        }

        private void ShowTutorialPrompt(string tutorialUrl, Window targetWindow)
        {
            bool isSpanish = IsSpanishLanguage();

            string message = isSpanish
                ? "¿Te gustaría ver el tutorial?"
                : "Would you like to see the tutorial?";

            string caption = isSpanish
                ? "Tutorial"
                : "Tutorial";

            MessageBoxResult result = MessageBox.Show(
                message,
                caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // ABRIR YOUTUBE EN EL NAVEGADOR
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = tutorialUrl,
                        UseShellExecute = false
                    });
                }
                catch (Exception ex)
                {
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

            //  SIEMPRE ABRIR LA VENTANA OBJETIVO (después del tutorial o si dijo "No")
            OpenWindow(targetWindow);
        }

        private void ManualBtn_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWindow(new ManualInstallerWindow());
        }

        private void OpenWindow(Window targetWindow)
        {
            //  CERRAR SOLO LAS VENTANAS SECUNDARIAS (NO MainWindow)
            var windowsToClose = Application.Current.Windows
                .Cast<Window>()
                .Where(w => w != Application.Current.MainWindow &&
                           w.IsLoaded &&
                           w.GetType().Name != "MainWindow")
                .ToList();

            foreach (var window in windowsToClose)
            {
                try
                {
                    window.Close();
                }
                catch { }
            }

            //  ABRIR LA NUEVA VENTANA
            targetWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            targetWindow.Show();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
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
            this.Close();
        }
    }
}