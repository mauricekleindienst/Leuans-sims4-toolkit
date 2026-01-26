using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ModernDesign.MVVM.View
{
    public partial class ManualInstallerWindow : Window
    {
        private int _currentStep = 0;
        private readonly List<TutorialStep> _steps = new List<TutorialStep>();
        private readonly HttpClient _httpClient = new HttpClient();
        private string _manualInstallUrl = "https://leuan.zeroauno.com/sims4-toolkit/manualdownloads.html";
        private string _manualInstall2Url = "https://www.youtube.com/watch?v=TF0EBobPWdc&lc=UgylGJhFPp954naFxRp4AaABAg";

        public ManualInstallerWindow()
        {
            InitializeComponent();
            InitializeTutorialSteps();
            ShowCurrentStep();

            //  Links dinámicos
            Loaded += async (s, e) => await LoadManualInstallLinksAsync();
        }

        private async Task LoadManualInstallLinksAsync()
        {
            try
            {
                string url = "https://zeroauno.blob.core.windows.net/leuan/TheSims4/Utility/links.txt";
                string content = await _httpClient.GetStringAsync(url);

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

                            if (key == "manualInstall")
                                _manualInstallUrl = value;
                            else if (key == "manualInstall2")
                                _manualInstall2Url = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading manual install links: {ex.Message}");
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

        private void InitializeTutorialSteps()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                WelcomeTitle.Text = "👋 ¡Bienvenido a la Instalación Manual!";
                WelcomeDescription.Text = "Sigue estos simples pasos para instalar tus DLCs manualmente. ¡Tómate tu tiempo y lee cada paso con cuidado!";

                _steps.Add(new TutorialStep
                {
                    Number = 1,
                    Icon = "📥",
                    Title = "Descarga tus DLCs",
                    Description = "Primero, necesitas descargar los archivos DLC desde tu fuente de confianza.",
                    Instructions = new List<string>
                    {
                        "Descarga los archivos DLC (.zip) a una carpeta de tu elección",
                        "Asegúrate de recordar dónde guardaste los archivos",
                        "Verifica que la descarga se haya completado correctamente",
                        "Los archivos pueden tener nombres como: EP01.zip, GP05.zip, SP20.zip, etc.",
                        " ",
                        "¿No sabes de donde descargar los archivos?",
                        "Prueba buscando aquí: https://leuan.zeroauno.com/sims4-toolkit/manualdownloads.html"
                    },
                    TipText = "💡 Consejo: Crea una carpeta llamada 'Sims 4 DLCs' en tu escritorio para mantener todo organizado."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 2,
                    Icon = "📂",
                    Title = "Extrae los archivos",
                    Description = "Necesitas extraer el contenido de los archivos descargados.",
                    Instructions = new List<string>
                    {
                        "Haz clic derecho en cada archivo .zip",
                        "Selecciona 'Extraer aquí' o 'Extract here'",
                        "Espera a que se complete la extracción de todos los archivos",
                        "Verás carpetas con nombres como: EP01, __Installer, etc.",
                        "",
                        "Desde aquí, puedes seguir sin problemas el video tutorial de:",
                        _manualInstall2Url
                    },
                    TipText = "⚠️ Importante: NO borres los archivos .zip originales hasta confirmar que todo funciona correctamente."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 3,
                    Icon = "🎮",
                    Title = "Localiza tu carpeta de The Sims 4",
                    Description = "Encuentra dónde está instalado The Sims 4 en tu computadora.",
                    Instructions = new List<string>
                    {
                        "Abre el Explorador de archivos de Windows",
                        "Las ubicaciones comunes son:",
                        "   • C:\\Program Files\\EA Games\\The Sims 4",
                        "   • C:\\Program Files (x86)\\Origin Games\\The Sims 4",
                        "Busca la carpeta que contiene las subcarpetas 'Game' y 'Data'",
                        "Anota la ruta completa de esta carpeta"
                    },
                    TipText = "💡 Consejo: Si no encuentras la carpeta, haz clic derecho en el acceso directo del juego → Propiedades → Abrir ubicación del archivo."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 4,
                    Icon = "📋",
                    Title = "Copia las carpetas extraídas",
                    Description = "Ahora vamos a mover los DLCs a la carpeta del juego.",
                    Instructions = new List<string>
                    {
                        "Abre la carpeta donde extrajiste los DLCs",
                        "Selecciona TODAS las carpetas extraídas (EP01, EP02, __Installer, etc.)",
                        "Haz clic derecho y selecciona 'Copiar'",
                        "Ve a la carpeta de instalación de The Sims 4",
                        "Haz clic derecho en un espacio vacío y selecciona 'Pegar'",
                        "Si te pregunta si deseas reemplazar archivos, selecciona 'Sí' o 'Reemplazar'"
                    },
                    TipText = "⚠️ Importante: Asegúrate de copiar TODO, incluyendo la carpeta __Installer que es esencial."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 5,
                    Icon = "🔓",
                    Title = "Instala el DLC Unlocker",
                    Description = "Para que los DLCs funcionen, necesitas el EA DLC Unlocker.",
                    Instructions = new List<string>
                    {
                        "Cierra The Sims 4 si está abierto",
                        "Cierra Origin o EA App completamente",
                        "Ve a la sección 'Home' de esta herramienta",
                        "Haz clic en 'Install EA DLC Unlocker'",
                        "Sigue las instrucciones en pantalla",
                        "Reinicia Origin/EA App después de la instalación"
                    },
                    TipText = "💡 Consejo: El DLC Unlocker es completamente seguro y necesario para que los DLCs funcionen correctamente."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 6,
                    Icon = "",
                    Title = "¡Listo para jugar!",
                    Description = "Verifica que todo esté funcionando correctamente.",
                    Instructions = new List<string>
                    {
                        "Abre Origin o EA App",
                        "Inicia The Sims 4",
                        "En el menú principal, ve a 'Opciones del juego'",
                        "Revisa la sección 'Otros' → 'Contenido del juego'",
                        "Deberías ver todos tus DLCs instalados listados ahí",
                        "¡Disfruta jugando con tus nuevos DLCs!"
                    },
                    TipText = "🎉 ¡Felicidades! Si ves tus DLCs en el juego, la instalación fue exitosa. ¡Diviértete!"
                });
            }
            else
            {
                _steps.Add(new TutorialStep
                {
                    Number = 1,
                    Icon = "📥",
                    Title = "Download Your DLCs",
                    Description = "First, you need to download the DLC files from your trusted source.",
                    Instructions = new List<string>
                    {
                        "Download the DLC files (.zip) to a folder of your choice",
                        "Make sure to remember where you saved the files",
                        "Verify that the download completed successfully",
                        "Files may have names like: EP01.zip, GP05.zip, SP20.zip, etc.",
                        " ",
                        "Dont know where to download the files?",
                        "Try searching here: https://leuan.zeroauno.com/sims4-toolkit/manualdownloads.html"
                    },
                    TipText = "💡 Tip: Create a folder called 'Sims 4 DLCs' on your desktop to keep everything organized."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 2,
                    Icon = "📂",
                    Title = "Extract the Files",
                    Description = "You need to extract the contents of the downloaded files.",
                    Instructions = new List<string>
                    {
                        "Right-click on each .zip file",
                        "Select 'Extract here'",
                        "Wait for all files to finish extracting",
                        "You'll see folders with names like: EP01, __Installer, etc.",
                        "",
                        "From here, you can follow without problems the video tutorial from:",
                        _manualInstall2Url
                    },
                    TipText = "⚠️ Important: DON'T delete the original .zip files until you confirm everything works correctly."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 3,
                    Icon = "🎮",
                    Title = "Locate Your Sims 4 Folder",
                    Description = "Find where The Sims 4 is installed on your computer.",
                    Instructions = new List<string>
                    {
                        "Open Windows File Explorer",
                        "Common locations are:",
                        "   • C:\\Program Files\\EA Games\\The Sims 4",
                        "   • C:\\Program Files (x86)\\Origin Games\\The Sims 4",
                        "Look for the folder containing 'Game' and 'Data' subfolders",
                        "Write down the full path to this folder"
                    },
                    TipText = "💡 Tip: If you can't find the folder, right-click the game shortcut → Properties → Open file location."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 4,
                    Icon = "📋",
                    Title = "Copy the Extracted Folders",
                    Description = "Now we'll move the DLCs to the game folder.",
                    Instructions = new List<string>
                    {
                        "Open the folder where you extracted the DLCs",
                        "Select ALL extracted folders (EP01, EP02, __Installer, etc.)",
                        "Right-click and select 'Copy'",
                        "Go to The Sims 4 installation folder",
                        "Right-click in an empty space and select 'Paste'",
                        "If asked to replace files, select 'Yes' or 'Replace'"
                    },
                    TipText = "⚠️ Important: Make sure to copy EVERYTHING, including the __Installer folder which is essential."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 5,
                    Icon = "🔓",
                    Title = "Install the DLC Unlocker",
                    Description = "For the DLCs to work, you need the EA DLC Unlocker.",
                    Instructions = new List<string>
                    {
                        "Close The Sims 4 if it's open",
                        "Close Origin or EA App completely",
                        "Go to the 'Home' section of this tool",
                        "Click 'Install EA DLC Unlocker'",
                        "Follow the on-screen instructions",
                        "Restart Origin/EA App after installation"
                    },
                    TipText = "💡 Tip: The DLC Unlocker is completely safe and necessary for DLCs to work properly."
                });

                _steps.Add(new TutorialStep
                {
                    Number = 6,
                    Icon = "",
                    Title = "Ready to Play!",
                    Description = "Verify that everything is working correctly.",
                    Instructions = new List<string>
                    {
                        "Open Origin or EA App",
                        "Launch The Sims 4",
                        "In the main menu, go to 'Game Options'",
                        "Check the 'Other' → 'Game Content' section",
                        "You should see all your installed DLCs listed there",
                        "Enjoy playing with your new DLCs!"
                    },
                    TipText = "🎉 Congratulations! If you see your DLCs in the game, the installation was successful. Have fun!"
                });
            }
        }

        private void ShowCurrentStep()
        {
            TutorialContainer.Children.Clear();

            // Create NEW welcome message (don't reuse XAML elements)
            var welcomeBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FFFFFF")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(25),
                Margin = new Thickness(0, 0, 0, 30)
            };

            var welcomeStack = new StackPanel();

            bool isSpanish = IsSpanishLanguage();

            var welcomeTitle = new TextBlock
            {
                Text = isSpanish ? "👋 ¡Bienvenido a la Instalación Manual!" : "👋 Welcome to Manual Installation!",
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var welcomeDesc = new TextBlock
            {
                Text = isSpanish
                    ? "Sigue estos simples pasos para instalar tus DLCs manualmente. ¡Tómate tu tiempo y lee cada paso con cuidado!"
                    : "Follow these simple steps to install your DLCs manually. Take your time and read each step carefully!",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            };

            welcomeStack.Children.Add(welcomeTitle);
            welcomeStack.Children.Add(welcomeDesc);
            welcomeBorder.Child = welcomeStack;
            TutorialContainer.Children.Add(welcomeBorder);

            // Show current step
            if (_currentStep < _steps.Count)
            {
                var step = _steps[_currentStep];
                var stepCard = CreateStepCard(step);
                TutorialContainer.Children.Add(stepCard);

                // Update navigation
                StepIndicator.Text = isSpanish
                    ? $"Paso {_currentStep + 1} de {_steps.Count}"
                    : $"Step {_currentStep + 1} of {_steps.Count}";

                PrevBtn.IsEnabled = _currentStep > 0;

                if (_currentStep == _steps.Count - 1)
                {
                    NextBtn.Content = isSpanish ? "✓ Finalizar" : "✓ Finish";
                }
                else
                {
                    NextBtn.Content = isSpanish ? "Siguiente Paso →" : "Next Step →";
                }
            }
        }

        private Border CreateStepCard(TutorialStep step)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15FFFFFF")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(30),
                Margin = new Thickness(0, 0, 0, 25)
            };

            var mainStack = new StackPanel();

            // Header with number and icon
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var numberBorder = new Border
            {
                Width = 60,
                Height = 60,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3EC7E8")),
                CornerRadius = new CornerRadius(30),
                VerticalAlignment = VerticalAlignment.Center
            };

            var numberText = new TextBlock
            {
                Text = step.Number.ToString(),
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            numberBorder.Child = numberText;
            Grid.SetColumn(numberBorder, 0);
            headerGrid.Children.Add(numberBorder);

            var titleStack = new StackPanel
            {
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = $"{step.Icon}  {step.Title}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            var descText = new TextBlock
            {
                Text = step.Description,
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            titleStack.Children.Add(titleText);
            titleStack.Children.Add(descText);
            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(titleStack);

            mainStack.Children.Add(headerGrid);

            // Instructions
            var instructionsBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10FFFFFF")),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var instructionsStack = new StackPanel();

            foreach (var instruction in step.Instructions)
            {
                var instructionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var bullet = new TextBlock
                {
                    Text = "▸",
                    FontSize = 16,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3EC7E8")),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };

                // NUEVO: Detectar si hay URL en la instrucción
                if (instruction.Contains("http://") || instruction.Contains("https://"))
                {
                    var textBlock = CreateTextBlockWithHyperlinks(instruction);
                    instructionPanel.Children.Add(bullet);
                    instructionPanel.Children.Add(textBlock);
                }
                else
                {
                    var instructionText = new TextBlock
                    {
                        Text = instruction,
                        FontSize = 13,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 20
                    };
                    instructionPanel.Children.Add(bullet);
                    instructionPanel.Children.Add(instructionText);
                }

                instructionsStack.Children.Add(instructionPanel);
            }

            instructionsBorder.Child = instructionsStack;
            mainStack.Children.Add(instructionsBorder);

            // Tip
            if (!string.IsNullOrEmpty(step.TipText))
            {
                var tipBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20F59E0B")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(15)
                };

                var tipText = new TextBlock
                {
                    Text = step.TipText,
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D")),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                };

                tipBorder.Child = tipText;
                mainStack.Children.Add(tipBorder);
            }

            card.Child = mainStack;
            return card;
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 0)
            {
                _currentStep--;
                ShowCurrentStep();
            }
        }

        private TextBlock CreateTextBlockWithHyperlinks(string text)
        {
            var textBlock = new TextBlock
            {
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            // Buscar URLs en el texto
            var urlPattern = @"(https?://[^\s]+)";
            var regex = new System.Text.RegularExpressions.Regex(urlPattern);
            var matches = regex.Matches(text);

            if (matches.Count == 0)
            {
                textBlock.Text = text;
                return textBlock;
            }

            int lastIndex = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                // Texto antes del link
                if (match.Index > lastIndex)
                {
                    textBlock.Inlines.Add(new System.Windows.Documents.Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                // Crear hyperlink
                var hyperlink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(match.Value))
                {
                    NavigateUri = new Uri(match.Value),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3EC7E8")),
                    TextDecorations = null
                };

                hyperlink.RequestNavigate += (sender, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = e.Uri.AbsoluteUri,
                            UseShellExecute = false
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                textBlock.Inlines.Add(hyperlink);
                lastIndex = match.Index + match.Length;
            }

            // Texto después del último link
            if (lastIndex < text.Length)
            {
                textBlock.Inlines.Add(new System.Windows.Documents.Run(text.Substring(lastIndex)));
            }

            return textBlock;
        }
        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < _steps.Count - 1)
            {
                _currentStep++;
                ShowCurrentStep();
            }
            else
            {
                // Finished tutorial
                bool isSpanish = IsSpanishLanguage();
                var result = MessageBox.Show(
                    isSpanish
                        ? "¿Has completado todos los pasos?\n\n¿Deseas instalar el DLC Unlocker ahora?"
                        : "Have you completed all the steps?\n\nWould you like to install the DLC Unlocker now?",
                    isSpanish ? "Tutorial Completado" : "Tutorial Completed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    OpenDLCUnlockerWindow();
                }

                if (result == MessageBoxResult.No)
                {
                    this.Close();
                }
            }
        }

        private void OpenDLCUnlockerWindow()
        {
            try
            {
                var DLCUnlockerWindow = new DLCUnlockerWindow
                {
                    Owner = null  // CAMBIAR: de this a null para evitar problemas
                };

                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                fadeOut.Completed += (s, args) =>
                {
                    this.Hide();  // CAMBIAR: de Close() a Hide() temporalmente

                    DLCUnlockerWindow.Opacity = 0;
                    DLCUnlockerWindow.Show();

                    var fadeIn = new DoubleAnimation
                    {
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };

                    DLCUnlockerWindow.BeginAnimation(Window.OpacityProperty, fadeIn);

                    // NUEVO: Cerrar esta ventana cuando se cierre DLCUnlockerWindow
                    DLCUnlockerWindow.Closed += (s2, e2) =>
                    {
                        try { this.Close(); } catch { }
                    };
                };

                this.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
            catch (Exception ex)
            {
                bool isSpanish = IsSpanishLanguage();
                MessageBox.Show(
                    isSpanish
                        ? $"Error al abrir el instalador del DLC Unlocker:\n{ex.Message}"
                        : $"Error opening DLC Unlocker installer:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current?.MainWindow;

                // Si no hay ventana principal o somos nosotros mismos, cerrar directamente
                if (mainWindow == null || mainWindow == this || mainWindow.IsLoaded == false)
                {
                    var simpleFade = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };
                    simpleFade.Completed += (s, args) => { try { this.Close(); } catch { } };
                    this.BeginAnimation(Window.OpacityProperty, simpleFade);
                    return;
                }

                var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };

                fadeOut.Completed += (s, args) =>
                {
                    try
                    {
                        // Verificar nuevamente antes de manipular mainWindow
                        if (mainWindow != null && !mainWindow.IsLoaded)
                        {
                            this.Close();
                            return;
                        }

                        this.Hide();

                        if (mainWindow != null)
                        {
                            mainWindow.Opacity = 0;
                            mainWindow.Show();

                            var fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(200) };
                            fadeIn.Completed += (s2, args2) =>
                            {
                                try { this.Close(); } catch { }
                            };

                            try
                            {
                                mainWindow.BeginAnimation(Window.OpacityProperty, fadeIn);
                            }
                            catch
                            {
                                this.Close();
                            }
                        }
                        else
                        {
                            this.Close();
                        }
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

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current?.MainWindow;

                // Si no hay ventana principal o somos nosotros mismos, cerrar directamente
                if (mainWindow == null || mainWindow == this || mainWindow.IsLoaded == false)
                {
                    var simpleFade = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };
                    simpleFade.Completed += (s, args) => { try { this.Close(); } catch { } };
                    this.BeginAnimation(Window.OpacityProperty, simpleFade);
                    return;
                }

                var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };

                fadeOut.Completed += (s, args) =>
                {
                    try
                    {
                        // Verificar nuevamente antes de manipular mainWindow
                        if (mainWindow != null && !mainWindow.IsLoaded)
                        {
                            this.Close();
                            return;
                        }

                        this.Hide();

                        if (mainWindow != null)
                        {
                            mainWindow.Opacity = 0;
                            mainWindow.Show();

                            var fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(200) };
                            fadeIn.Completed += (s2, args2) =>
                            {
                                try { this.Close(); } catch { }
                            };

                            try
                            {
                                mainWindow.BeginAnimation(Window.OpacityProperty, fadeIn);
                            }
                            catch
                            {
                                this.Close();
                            }
                        }
                        else
                        {
                            this.Close();
                        }
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
    }

    // Helper class for tutorial steps
    public class TutorialStep
    {
        public int Number { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Instructions { get; set; }
        public string TipText { get; set; }
    }
}