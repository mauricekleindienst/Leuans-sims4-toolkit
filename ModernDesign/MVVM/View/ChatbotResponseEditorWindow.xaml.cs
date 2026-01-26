using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModernDesign
{
    public partial class ChatbotResponseEditorWindow : Window
    {
        public string Keywords { get; private set; }
        public string ResponseES { get; private set; }
        public string ResponseEN { get; private set; }
        public string Action { get; private set; }

        public ChatbotResponseEditorWindow()
        {
            InitializeComponent();
            LoadLanguage();
            ActionComboBox.SelectedIndex = 0;
            ActionComboBox.SelectionChanged += ActionComboBox_SelectionChanged;
        }

        public ChatbotResponseEditorWindow(string keywords, string responseES, string responseEN, string action) : this()
        {
            KeywordsTextBox.Text = keywords;
            ResponseESTextBox.Text = responseES;
            ResponseENTextBox.Text = responseEN;

            // Seleccionar el action correcto
            if (string.IsNullOrEmpty(action))
            {
                ActionComboBox.SelectedIndex = 0;
            }
            else if (action.StartsWith("OPEN_URL:"))
            {
                ActionComboBox.SelectedIndex = ActionComboBox.Items.Count - 1; // Último item (OPEN_URL)
                CustomURLTextBox.Text = action.Substring("OPEN_URL:".Length);
                CustomURLLabel.Visibility = Visibility.Visible;
                CustomURLTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                foreach (ComboBoxItem item in ActionComboBox.Items)
                {
                    if (item.Tag?.ToString() == action)
                    {
                        ActionComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
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
                TitleText.Text = "Editar Respuesta del Chatbot";
                KeywordsLabel.Text = "Palabras Clave (separadas por |)";
                ResponseESLabel.Text = "Respuesta (Español)";
                ResponseENLabel.Text = "Respuesta (Inglés)";
                ActionLabel.Text = "Acción";
                CustomURLLabel.Text = "URL Personalizada";
                SaveButton.Content = "Guardar";
                CancelButton.Content = "Cancelar";
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

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionComboBox.SelectedItem is ComboBoxItem selected)
            {
                string tag = selected.Tag?.ToString() ?? "";

                if (tag.StartsWith("OPEN_URL:"))
                {
                    CustomURLLabel.Visibility = Visibility.Visible;
                    CustomURLTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomURLLabel.Visibility = Visibility.Collapsed;
                    CustomURLTextBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(KeywordsTextBox.Text))
            {
                bool isSpanish = GetLanguageCode().StartsWith("es");
                MessageBox.Show(
                    isSpanish ? "Por favor ingresa al menos una palabra clave." : "Please enter at least one keyword.",
                    isSpanish ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Keywords = KeywordsTextBox.Text.Trim();
            ResponseES = ResponseESTextBox.Text.Trim();
            ResponseEN = ResponseENTextBox.Text.Trim();

            if (ActionComboBox.SelectedItem is ComboBoxItem selected)
            {
                string tag = selected.Tag?.ToString() ?? "";

                if (tag.StartsWith("OPEN_URL:"))
                {
                    Action = $"OPEN_URL:{CustomURLTextBox.Text.Trim()}";
                }
                else
                {
                    Action = tag;
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}