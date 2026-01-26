using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace ModernDesign
{
    public partial class ChatbotSettingsWindow : Window
    {
        private string chatbotFolder = "";
        private string chatbotFilePath = "";
        private string profilePath = "";
        private List<ChatbotResponseItem> responses = new List<ChatbotResponseItem>();

        private class ChatbotResponseItem
        {
            public string Keywords { get; set; }
            public string ResponseES { get; set; }
            public string ResponseEN { get; set; }
            public string Action { get; set; }

            public override string ToString()
            {
                return $"Keywords: {Keywords}";
            }
        }

        public ChatbotSettingsWindow()
        {
            InitializeComponent();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string toolkitFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
            chatbotFolder = Path.Combine(toolkitFolder, "chatbot");
            chatbotFilePath = Path.Combine(chatbotFolder, "chatbot.txt");
            profilePath = Path.Combine(toolkitFolder, "profile.ini");

            LoadLanguage();
            LoadChatbotSettings();
            LoadResponses();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
            catch { }
        }

        private void LoadLanguage()
        {
            bool isSpanish = GetLanguageCode().StartsWith("es");

            if (isSpanish)
            {
                TitleText.Text = "Configuración del Chatbot";
                EnableChatbotText.Text = "Habilitar Chatbot";
                EnableChatbotDesc.Text = "Mostrar u ocultar la burbuja del chatbot. Si se deshabilita, puedes reactivarlo en Settings.";
                EnableChatbotCheckbox.Content = "Chatbot Habilitado";
                ManageResponsesText.Text = "Administrar Respuestas del Chatbot";
                AddResponseButton.Content = "Agregar";
                EditResponseButton.Content = "Editar";
                DeleteResponseButton.Content = "Eliminar";
                ShareChatbotText.Text = "Comparte tu Chatbot";
                ShareChatbotDesc.Text = "Si subes tu chatbot.txt entrenado con tu idioma y keywords, ayudarás a crear un chatbot.txt general mejor. ¡Esto es muy apreciado!\n\nTe invitamos a subir tu chatbot.txt en nuestro Discord, en el canal #chatbot.";
                OpenDiscordButton.Content = "Abrir Discord #chatbot";
                SaveButton.Content = "Guardar Cambios";
            }
        }

        private string GetLanguageCode()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string iniPath = Path.Combine(appData, "Leuan's - Sims 4 ToolKit", "language.ini");
            string languageCode = "en-US";

            try
            {
                if (File.Exists(iniPath))
                {
                    foreach (string line in File.ReadAllLines(iniPath))
                    {
                        if (line.StartsWith("Language = ", StringComparison.OrdinalIgnoreCase))
                        {
                            languageCode = line.Substring("Language = ".Length).Trim();
                            break;
                        }
                    }
                }
            }
            catch { }

            return languageCode;
        }

        private void LoadChatbotSettings()
        {
            try
            {
                if (File.Exists(profilePath))
                {
                    foreach (string line in File.ReadAllLines(profilePath))
                    {
                        if (line.Trim().StartsWith("ChatBot", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                EnableChatbotCheckbox.IsChecked = (parts[1].Trim().ToLower() == "true");
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadResponses()
        {
            responses.Clear();

            try
            {
                if (File.Exists(chatbotFilePath))
                {
                    string content = File.ReadAllText(chatbotFilePath);
                    ParseResponses(content);
                }
            }
            catch { }

            ResponsesListBox.ItemsSource = null;
            ResponsesListBox.ItemsSource = responses;
        }

        private void ParseResponses(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            ChatbotResponseItem currentItem = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("[KEYWORDS]"))
                {
                    if (currentItem != null)
                        responses.Add(currentItem);
                    currentItem = new ChatbotResponseItem();
                }
                else if (trimmed.StartsWith("KEYWORDS=") && currentItem != null)
                {
                    currentItem.Keywords = trimmed.Substring("KEYWORDS=".Length);
                }
                else if (trimmed.StartsWith("RESPONSE_ES=") && currentItem != null)
                {
                    currentItem.ResponseES = trimmed.Substring("RESPONSE_ES=".Length);
                }
                else if (trimmed.StartsWith("RESPONSE_EN=") && currentItem != null)
                {
                    currentItem.ResponseEN = trimmed.Substring("RESPONSE_EN=".Length);
                }
                else if (trimmed.StartsWith("ACTION=") && currentItem != null)
                {
                    currentItem.Action = trimmed.Substring("ACTION=".Length);
                }
            }

            if (currentItem != null)
                responses.Add(currentItem);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateProfileIni();
                SaveResponsesToFile();

                bool isSpanish = GetLanguageCode().StartsWith("es");
                MessageBox.Show(
                    isSpanish ? "Configuración guardada exitosamente." : "Settings saved successfully.",
                    isSpanish ? "Éxito" : "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                bool isSpanish = GetLanguageCode().StartsWith("es");
                MessageBox.Show(
                    isSpanish ? $"Error al guardar: {ex.Message}" : $"Error saving: {ex.Message}",
                    isSpanish ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateProfileIni()
        {
            if (!File.Exists(profilePath)) return;

            var lines = File.ReadAllLines(profilePath).ToList();
            bool found = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().StartsWith("ChatBot", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"ChatBot = {(EnableChatbotCheckbox.IsChecked == true ? "true" : "false")}";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                int miscIndex = lines.FindIndex(l => l.Trim() == "[Misc]");
                if (miscIndex >= 0)
                {
                    lines.Insert(miscIndex + 1, $"ChatBot = {(EnableChatbotCheckbox.IsChecked == true ? "true" : "false")}");
                }
            }

            File.WriteAllLines(profilePath, lines);
        }

        private void SaveResponsesToFile()
        {
            if (!Directory.Exists(chatbotFolder))
                Directory.CreateDirectory(chatbotFolder);

            using (StreamWriter writer = new StreamWriter(chatbotFilePath, false))
            {
                foreach (var response in responses)
                {
                    writer.WriteLine("[KEYWORDS]");
                    writer.WriteLine($"KEYWORDS={response.Keywords}");
                    writer.WriteLine($"RESPONSE_ES={response.ResponseES}");
                    writer.WriteLine($"RESPONSE_EN={response.ResponseEN}");
                    writer.WriteLine($"ACTION={response.Action}");
                    writer.WriteLine();
                }
            }
        }

        private void ResponsesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            EditResponseButton.IsEnabled = ResponsesListBox.SelectedItem != null;
            DeleteResponseButton.IsEnabled = ResponsesListBox.SelectedItem != null;
        }

        private void AddResponse_Click(object sender, RoutedEventArgs e)
        {
            ChatbotResponseEditorWindow editor = new ChatbotResponseEditorWindow();
            editor.Owner = this;
            if (editor.ShowDialog() == true)
            {
                responses.Add(new ChatbotResponseItem
                {
                    Keywords = editor.Keywords,
                    ResponseES = editor.ResponseES,
                    ResponseEN = editor.ResponseEN,
                    Action = editor.Action
                });
                ResponsesListBox.ItemsSource = null;
                ResponsesListBox.ItemsSource = responses;
            }
        }

        private void EditResponse_Click(object sender, RoutedEventArgs e)
        {
            if (ResponsesListBox.SelectedItem is ChatbotResponseItem selected)
            {
                ChatbotResponseEditorWindow editor = new ChatbotResponseEditorWindow(selected.Keywords, selected.ResponseES, selected.ResponseEN, selected.Action);
                editor.Owner = this;
                if (editor.ShowDialog() == true)
                {
                    selected.Keywords = editor.Keywords;
                    selected.ResponseES = editor.ResponseES;
                    selected.ResponseEN = editor.ResponseEN;
                    selected.Action = editor.Action;
                    ResponsesListBox.ItemsSource = null;
                    ResponsesListBox.ItemsSource = responses;
                }
            }
        }

        private void DeleteResponse_Click(object sender, RoutedEventArgs e)
        {
            if (ResponsesListBox.SelectedItem is ChatbotResponseItem selected)
            {
                bool isSpanish = GetLanguageCode().StartsWith("es");
                var result = MessageBox.Show(
                    isSpanish ? "¿Estás seguro de eliminar esta respuesta?" : "Are you sure you want to delete this response?",
                    isSpanish ? "Confirmar" : "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    responses.Remove(selected);
                    ResponsesListBox.ItemsSource = null;
                    ResponsesListBox.ItemsSource = responses;
                }
            }
        }

        private void OpenDiscord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "https://discord.gg/JYnpPt4nUu",
                UseShellExecute = false
            });
        }

        private void UltraSupporters_Click(object sender, RoutedEventArgs e)
        {
            bool isSpanish = GetLanguageCode().StartsWith("es");
            MessageBox.Show(
                isSpanish
                    ? "Los Ultra Supporters son aquellos que se suscriben a la membresía mensual de Leuan, recibiendo el rol 'Ultra Supporters' y un montón de ventajas y cosas únicas!"
                    : "Ultra Supporters are those who subscribe to Leuan's monthly membership, receiving the 'Ultra Supporters' role and a lot of unique advantages and perks!",
                "Ultra Supporters",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}