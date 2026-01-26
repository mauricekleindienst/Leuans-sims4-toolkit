using ModernDesign.MVVM;
using ModernDesign.MVVM.View;
using ModernDesign.MVVM.ViewModel;
using ModernDesign.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ModernDesign
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _cleanerTimer;
        private DispatcherTimer _ramMonitorTimer;
        private DispatcherTimer _tooltipTimer;
        private readonly Random _rng = new Random();
        private bool _ramWarningShown = false;

        private bool isChatbotOpen = false;
        private List<ChatbotResponse> chatbotResponses = new List<ChatbotResponse>();
        private bool chatbotEnabled = true;
        private string chatbotFolder = "";
        private string chatbotFilePath = "";

        // Clase interna para respuestas del chatbot
        private class ChatbotResponse
        {
            public List<string> Keywords { get; set; } = new List<string>();
            public string ResponseES { get; set; }
            public string ResponseEN { get; set; }
            public string Action { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Inicializar rutas del chatbot
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            chatbotFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "chatbot");
            chatbotFilePath = Path.Combine(chatbotFolder, "chatbot.txt");

            // Verificar si el chatbot está habilitado
            CheckChatbotEnabled();

            if (chatbotEnabled)
            {
                // Crear archivo chatbot.txt si no existe
                EnsureChatbotFileExists();

                // Descargar video del robot si no existe
                EnsureRobotVideoExists();

                // Cargar respuestas del chatbot
                LoadChatbotResponsesFromLocal();

                // Mostrar tooltip inicial después de 1 segundo
                _tooltipTimer = new DispatcherTimer();
                _tooltipTimer.Interval = TimeSpan.FromSeconds(1);
                _tooltipTimer.Tick += ShowInitialTooltip;
                _tooltipTimer.Start();
            }
            else
            {
                // Ocultar completamente el chatbot
                ChatbotContainer.Visibility = Visibility.Collapsed;
            }

            StartCleanerTimer();
            StartRamMonitorTimer();

            this.Closed += MainWindow_Closed;
        }

        private void CheckChatbotEnabled()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string toolkitFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
            string profilePath = Path.Combine(toolkitFolder, "profile.ini");

            chatbotEnabled = true; // Por defecto habilitado

            try
            {
                if (File.Exists(profilePath))
                {
                    string[] lines = File.ReadAllLines(profilePath);
                    foreach (string line in lines)
                    {
                        if (line.Trim().StartsWith("ChatBot", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                string value = parts[1].Trim().ToLower();
                                chatbotEnabled = (value == "true");
                            }
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Si hay error, mantener habilitado por defecto
            }
        }

        private void EnsureChatbotFileExists()
        {
            try
            {
                // Crear directorio si no existe
                if (!Directory.Exists(chatbotFolder))
                {
                    Directory.CreateDirectory(chatbotFolder);
                }

                // Crear archivo si no existe
                if (!File.Exists(chatbotFilePath))
                {
                    string defaultContent = @"[KEYWORDS]
KEYWORDS=hola|hi|hello|ola|necesito ayuda|i need help|help|help me|ayudame|problemas|errores|solucion|solucionalo
RESPONSE_ES=Hola!, intenta explicar brevemente lo que necesitas... por ejemplo ""Abre el DLC Updater"" o ""Me crashea al descargar""
RESPONSE_EN=Hi!, try telling me briefly what you need... For example ""Open DLC Updater"" or ""When downloading the toolkit crashes""
ACTION=

[KEYWORDS]
KEYWORDS=discord|more help|help|ayuda|mas ayuda|más ayuda
RESPONSE_ES=🧶 Abriendo Discord...
RESPONSE_EN=🧶 Opening Discord...
ACTION=OPEN_URL:https://discord.gg/JYnpPt4nUu

[KEYWORDS]
KEYWORDS=Open dlc updater|abrir dlc updater|dlc updater|descargar dlcs|download dlcs|i want the new kits|update my game to newest version|update my game|download kits|download dlc
RESPONSE_ES=🧶 Abriendo DLC Updater...
RESPONSE_EN=🧶 Opening DLC Updater...
ACTION=OPEN_DLC_UPDATER

[KEYWORDS]
KEYWORDS=Why are packs showing as not owned|dlcs not owned|not owned|unowned|It says I don't own some|dont own|don't own
RESPONSE_ES=Mmh, al parecer si no aparecen como Desbloqueados es problema de una mala instalación del DLC Unlocker, prueba de manera manual.
RESPONSE_EN=Mmh, if they are appearing as ""unowned"", the problem is located in a bad installation for DLC Unlocker, try installing it manually.
ACTION=

[KEYWORDS]
KEYWORDS=Open dlc unlocker|abrir dlc unlocker|dlc unlocker|unlock dlcs|desbloquear dlcs|unlocker
RESPONSE_ES=🧶 Abriendo DLC Unlocker...
RESPONSE_EN=🧶 Opening DLC Unlocker...
ACTION=OPEN_DLC_UNLOCKER";

                    File.WriteAllText(chatbotFilePath, defaultContent);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creando chatbot.txt: {ex.Message}");
            }
        }

        private async void EnsureRobotVideoExists()
        {
            try
            {
                string robotVideoPath = Path.Combine(chatbotFolder, "robotin.mp4");

                // Si ya existe, no descargar de nuevo
                if (File.Exists(robotVideoPath))
                {
                    return;
                }

                // Descargar el video
                string videoUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/imgs/robotin.mp4";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var videoBytes = await client.GetByteArrayAsync(videoUrl);
                    File.WriteAllBytes(robotVideoPath, videoBytes);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error descargando robot video: {ex.Message}");
                // Si falla, no pasa nada, simplemente no se mostrará el video
            }
        }
        private void LoadChatbotResponsesFromLocal()
        {
            try
            {
                if (File.Exists(chatbotFilePath))
                {
                    string content = File.ReadAllText(chatbotFilePath);
                    ParseChatbotResponses(content);
                }
                else
                {
                    chatbotResponses = new List<ChatbotResponse>();
                }
            }
            catch
            {
                chatbotResponses = new List<ChatbotResponse>();
            }
        }

        private void ShowInitialTooltip(object sender, EventArgs e)
        {
            _tooltipTimer.Stop();

            bool isSpanish = GetLanguageCode().StartsWith("es");

            TooltipText.Text = isSpanish
                ? "¡Hola! Leuan me ha elegido para ti. ¡Por favor presiona la burbuja de abajo para abrir el ChatBot!"
                : "Hi!  Leuan has chosen me for you! Please press the bubble below to open the ChatBot!";

            // Cargar y reproducir video
            string robotVideoPath = Path.Combine(chatbotFolder, "robotin.mp4");
            if (File.Exists(robotVideoPath))
            {
                RobotVideo.Source = new Uri(robotVideoPath, UriKind.Absolute);
                RobotVideo.Play();
            }

            // Animación de aparición
            ChatbotTooltip.Visibility = Visibility.Visible;
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500));
            ChatbotTooltip.BeginAnimation(OpacityProperty, fadeIn);

            // Timer para ocultar después de 9 segundos
            DispatcherTimer hideTimer = new DispatcherTimer();
            hideTimer.Interval = TimeSpan.FromSeconds(9);
            hideTimer.Tick += (s, args) =>
            {
                hideTimer.Stop();
                if (RobotVideo != null)
                {
                    RobotVideo.Stop();
                }
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
                fadeOut.Completed += (ss, ee) => { ChatbotTooltip.Visibility = Visibility.Collapsed; };
                ChatbotTooltip.BeginAnimation(OpacityProperty, fadeOut);
            };
            hideTimer.Start();
        }

        private void RobotVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop del video
            if (RobotVideo != null)
            {
                RobotVideo.Position = TimeSpan.Zero;
                RobotVideo.Play();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCustomBackground();
        }

        private void LoadCustomBackground()
        {
            var colors = UserSettingsManager.GetBackgroundColors();

            try
            {
                var gradient = new LinearGradientBrush();
                gradient.StartPoint = new Point(0, 0);
                gradient.EndPoint = new Point(1, 1);
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(colors[0]), 0));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(colors[1]), 0.45));
                gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(colors[2]), 1));

                MainBackgroundBorder.Background = gradient;
            }
            catch { }
        }

        private void StartCleanerTimer()
        {
            _cleanerTimer = new DispatcherTimer();
            _cleanerTimer.Tick += CleanerTimer_Tick;

            ScheduleNextClean();
            _cleanerTimer.Start();
        }

        private void StartRamMonitorTimer()
        {
            _ramMonitorTimer = new DispatcherTimer();
            _ramMonitorTimer.Interval = TimeSpan.FromSeconds(10);
            _ramMonitorTimer.Tick += RamMonitorTimer_Tick;
            _ramMonitorTimer.Start();
        }

        private void RamMonitorTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                long ramUsageBytes = currentProcess.WorkingSet64;
                double ramUsageMB = ramUsageBytes / (1024.0 * 1024.0);

                if (ramUsageMB > 800 && !_ramWarningShown)
                {
                    _ramWarningShown = true;
                    ShowRamWarning(ramUsageMB);
                }
            }
            catch
            {
            }
        }

        private void ShowRamWarning(double ramUsageMB)
        {
            string languageCode = GetLanguageCode();
            bool isSpanish = languageCode.StartsWith("es", StringComparison.OrdinalIgnoreCase);

            string message = isSpanish
                ? $"⚠️ Alto uso de RAM detectado ({ramUsageMB:F0} MB)\n\n" +
                  "Recomendamos DESACTIVAR la opción 'Preload Images on Startup' en Settings para reducir el consumo de memoria.\n\n" +
                  "Esto mejorará significativamente el rendimiento de la aplicación."
                : $"⚠️ High RAM usage detected ({ramUsageMB:F0} MB)\n\n" +
                  "We strongly recommend DISABLING the 'Preload Images on Startup' option in Settings to reduce memory consumption.\n\n" +
                  "This will significantly improve application performance.";

            string title = isSpanish ? "Advertencia de Memoria" : "Memory Warning";

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
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
            }

            return languageCode;
        }

        private void ScheduleNextClean()
        {
            const double baseSeconds = 15.0;
            double jitter = (_rng.NextDouble() * 10.0) - 5.0;
            double nextSeconds = baseSeconds + jitter;

            if (nextSeconds < 5.0)
                nextSeconds = 5.0;

            _cleanerTimer.Interval = TimeSpan.FromSeconds(nextSeconds);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _cleanerTimer?.Stop();
            _ramMonitorTimer?.Stop();
            _tooltipTimer?.Stop();
        }

        private void CleanerTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Lógica de limpieza
            }
            catch
            {
            }

            ScheduleNextClean();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ==================== CHATBOT ====================

        private void ChatbotButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (!isChatbotOpen)
            {
                ChatbotWindow.Visibility = Visibility.Visible;
                isChatbotOpen = true;

                if (ChatMessagesPanel.Children.Count == 0)
                {
                    bool isSpanish = GetLanguageCode().StartsWith("es");
                    AddBotMessage(isSpanish
                        ? "¡Hola! 👋 Soy el asistente virtual que Leuan asignó para ti. Describe tu problema y te ayudaré a solucionarlo."
                        : "Hello! 👋 I'm the virtual assistant that Leuan assigned for you. Describe your problem and I'll help you solve it.");
                }
            }
            else
            {
                ChatbotWindow.Visibility = Visibility.Collapsed;
                isChatbotOpen = false;
            }
        }

        private void CloseChatbot_Click(object sender, RoutedEventArgs e)
        {
            ChatbotWindow.Visibility = Visibility.Collapsed;
            isChatbotOpen = false;
            ChatMessagesPanel.Children.Clear();
        }

        private void ChatbotSettings_Click(object sender, RoutedEventArgs e)
        {
            ChatbotSettingsWindow settingsWindow = new ChatbotSettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            bool? result = settingsWindow.ShowDialog();

            if (result == true)
            {
                // Recargar respuestas del chatbot
                LoadChatbotResponsesFromLocal();

                // Verificar si el chatbot fue deshabilitado
                CheckChatbotEnabled();
                if (!chatbotEnabled)
                {
                    ChatbotContainer.Visibility = Visibility.Collapsed;
                    ChatbotWindow.Visibility = Visibility.Collapsed;
                    isChatbotOpen = false;
                }
            }
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, null);
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string userMessage = ChatInputBox.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            AddUserMessage(userMessage);
            ChatInputBox.Clear();
            ProcessUserMessage(userMessage);
        }

        private void AddUserMessage(string message)
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                CornerRadius = new CornerRadius(12, 12, 0, 12),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(50, 5, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 250
            };

            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 13,
                FontFamily = new FontFamily("Bahnschrift Light"),
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = textBlock;
            ChatMessagesPanel.Children.Add(messageBorder);
            ChatScrollViewer.ScrollToBottom();
        }

        private void AddBotMessage(string message)
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                CornerRadius = new CornerRadius(12, 12, 12, 0),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(5, 5, 50, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 250,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(1)
            };

            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 13,
                FontFamily = new FontFamily("Bahnschrift Light"),
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = textBlock;
            ChatMessagesPanel.Children.Add(messageBorder);
            ChatScrollViewer.ScrollToBottom();
        }

        private void ParseChatbotResponses(string content)
        {
            chatbotResponses.Clear();

            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            ChatbotResponse currentResponse = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("[KEYWORDS]"))
                {
                    if (currentResponse != null)
                    {
                        chatbotResponses.Add(currentResponse);
                    }
                    currentResponse = new ChatbotResponse();
                }
                else if (trimmed.StartsWith("KEYWORDS=") && currentResponse != null)
                {
                    var keywordsStr = trimmed.Substring("KEYWORDS=".Length);
                    currentResponse.Keywords = keywordsStr.Split('|').Select(k => k.Trim().ToLower()).ToList();
                }
                else if (trimmed.StartsWith("RESPONSE_ES=") && currentResponse != null)
                {
                    currentResponse.ResponseES = trimmed.Substring("RESPONSE_ES=".Length).Replace("\\n", "\n");
                }
                else if (trimmed.StartsWith("RESPONSE_EN=") && currentResponse != null)
                {
                    currentResponse.ResponseEN = trimmed.Substring("RESPONSE_EN=".Length).Replace("\\n", "\n");
                }
                else if (trimmed.StartsWith("ACTION=") && currentResponse != null)
                {
                    currentResponse.Action = trimmed.Substring("ACTION=".Length).Trim();
                }
            }

            if (currentResponse != null)
            {
                chatbotResponses.Add(currentResponse);
            }
        }

        private void ProcessUserMessage(string message)
        {
            string lowerMessage = message.ToLower();
            bool isSpanish = GetLanguageCode().StartsWith("es");

            foreach (var response in chatbotResponses)
            {
                if (response.Keywords.Any(keyword => lowerMessage.Contains(keyword)))
                {
                    string answer = isSpanish ? response.ResponseES : response.ResponseEN;
                    AddBotMessage(answer);

                    if (!string.IsNullOrEmpty(response.Action))
                    {
                        ExecuteChatbotAction(response.Action);
                    }

                    return;
                }
            }

            string defaultMessage = isSpanish
                ? "🤔 No pude identificar tu problema específico.\n\n¿Necesitas ayuda personalizada?\n\nÚnete al Discord de Leuan:\n🔗 discord.gg/JYnpPt4nUu"
                : "🤔 I couldn't identify your specific problem.\n\nNeed personalized help?\n\nJoin Leuan's Discord:\n🔗 discord.gg/JYnpPt4nUu";

            AddBotMessage(defaultMessage);
        }

        private void SettingsButton_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation glowAnimation = new DoubleAnimation
            {
                From = 10,
                To = 25,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            SettingsGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, glowAnimation);
        }

        private void ExecuteChatbotAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            try
            {
                if (action.StartsWith("OPEN_URL:", StringComparison.OrdinalIgnoreCase))
                {
                    string url = action.Substring("OPEN_URL:".Length).Trim();

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = url,
                        UseShellExecute = false
                    });
                    return;
                }

                switch (action.ToUpper())
                {
                    case "OPEN_DLC_UPDATER":
                        InstallModeSelector installmodeWindow = new InstallModeSelector();
                        installmodeWindow.Owner = this;
                        installmodeWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        installmodeWindow.ShowDialog();
                        break;

                    case "OPEN_DLC_UNLOCKER":
                        DLCUnlockerWindow dlcunlockerWindow = new DLCUnlockerWindow();
                        dlcunlockerWindow.Owner = this;
                        dlcunlockerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        dlcunlockerWindow.ShowDialog();
                        break;

                    case "UPDATE_GAME":
                        MainSelectionWindow mainSelectionWindow = new MainSelectionWindow();
                        mainSelectionWindow.Owner = this;
                        mainSelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        mainSelectionWindow.ShowDialog();
                        break;

                    case "CRACK_GAME":
                        CrackingToolWindow crackingToolWindow = new CrackingToolWindow();
                        crackingToolWindow.Owner = this;
                        crackingToolWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        crackingToolWindow.ShowDialog();
                        break;

                    case "DOWNLOAD_BASEGAME":
                        InstallMethodSelectorWindow installMethodWindow = new InstallMethodSelectorWindow();
                        installMethodWindow.Owner = this;
                        installMethodWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        installMethodWindow.ShowDialog();
                        break;

                    case "MOD_MANAGER":
                        OrganizeModsWindow organizeModsWindow = new OrganizeModsWindow();
                        organizeModsWindow.Owner = this;
                        organizeModsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        organizeModsWindow.ShowDialog();
                        break;

                    case "LOADING_SCREEN":
                        LoadingScreenSelectorWindow loadingScreenWindow = new LoadingScreenSelectorWindow();
                        loadingScreenWindow.Owner = this;
                        loadingScreenWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        loadingScreenWindow.ShowDialog();
                        break;

                    case "CHEATS_GUIDE":
                        if (DataContext is MainViewModel mainVM)
                        {
                            mainVM.CurrentView = new CheatsGuideView();
                        }
                        break;

                    case "GAMEPLAY_ENHANCER":
                        if (DataContext is MainViewModel mainVM2)
                        {
                            mainVM2.CurrentView = new GameplayEnhancerView();
                        }
                        break;

                    case "AUTOEXTRACT_DLCS":
                        SemiAutoInstallerWindow semiAutoWindow = new SemiAutoInstallerWindow();
                        semiAutoWindow.Owner = this;
                        semiAutoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        semiAutoWindow.ShowDialog();
                        break;

                    case "REPAIR_GAME":
                        RepairLoggerWindow repairWindow = new RepairLoggerWindow();
                        repairWindow.Owner = this;
                        repairWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        repairWindow.ShowDialog();
                        break;

                    case "CHANGE_LANGUAGE":
                        LanguageSelectorWindow languageWindow = new LanguageSelectorWindow();
                        languageWindow.Owner = this;
                        languageWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        languageWindow.ShowDialog();
                        break;

                    case "GAME_TWEAKER":
                        GameTweakerWindow gameTweakerWindow = new GameTweakerWindow();
                        gameTweakerWindow.Owner = this;
                        gameTweakerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        gameTweakerWindow.ShowDialog();
                        break;

                    case "SCREENSHOT_MANAGER":
                        if (DataContext is MainViewModel mainVM3)
                        {
                            mainVM3.CurrentView = new GalleryManagerWindow();
                        }
                        break;

                    case "MUSIC_MANAGER":
                        if (DataContext is MainViewModel mainVM4)
                        {
                            mainVM4.CurrentView = new MusicManagerView();
                        }
                        break;

                    case "SAVEGAME_MANAGER":
                        if (DataContext is MainViewModel mainVM5)
                        {
                            mainVM5.CurrentView = new SaveGamesView();
                        }
                        break;

                    case "LEARN_MODDING":
                        S4SCategoriesWindow s4sWindow = new S4SCategoriesWindow();
                        s4sWindow.Owner = this;
                        s4sWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        s4sWindow.ShowDialog();
                        break;

                    case "FIX_COMMON":
                        FixCommonErrorsWindow fixCommonWindow = new FixCommonErrorsWindow();
                        fixCommonWindow.Owner = this;
                        fixCommonWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        fixCommonWindow.ShowDialog();
                        break;

                    case "METHOD50_50":
                        Method5050Window method5050Window = new Method5050Window();
                        method5050Window.Owner = this;
                        method5050Window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        method5050Window.ShowDialog();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ejecutando acción del chatbot: {ex.Message}");
            }
        }

        // SOLUCIÓN AL PROBLEMA DE CIERRE - Agregar al final de la clase
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Detener todos los timers
            try
            {
                _cleanerTimer?.Stop();
                _ramMonitorTimer?.Stop();
                _tooltipTimer?.Stop();
            }
            catch { }

            // Detener video del robot si está reproduciéndose
            try
            {
                if (RobotVideo != null)
                {
                    RobotVideo.Stop();
                    RobotVideo.Source = null;
                }
            }
            catch { }

            // Forzar cierre completo de la aplicación
            Application.Current.Shutdown();
        }
    }
}