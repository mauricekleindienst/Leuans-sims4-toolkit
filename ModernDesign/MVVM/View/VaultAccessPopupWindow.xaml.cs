using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ModernDesign.MVVM.View
{
    public partial class VaultAccessPopupWindow : Window
    {
        public VaultAccessPopupWindow()
        {
            InitializeComponent();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool isSpanish = IsSpanishLanguage();

            if (isSpanish)
            {
                // First Popup
                FirstPopupTitle.Text = "Contenido Externo Detectado";
                FirstPopupMessage.Text = "¡Woops! Parece que esto está fuera del alcance del Sims 4 ToolKit. Al ser un título no relacionado con Los Sims, este contenido se aloja exclusivamente en nuestro repositorio privado de alta velocidad: 'Leuan's Vault'.\n\nPara garantizar la seguridad y la velocidad de descarga de estos archivos pesados, la descarga automática está limitada a colaboradores, puedes descargarlo manualmente en nuestro Discord completamente gratuito.";
                LearnMoreBtn.Content = "➔ ¿Cómo obtengo acceso a Leuan's Vault?";
                CloseFirstPopupBtn.Content = "Cerrar";

                // Second Popup
                SecondPopupTitle.Text = "Sobre Leuan's Vault";
                SecondPopupDescription.Text = "Leuan's Vault es una biblioteca privada de preservación digital donde gestionamos varías librerias y software externo con un sistema de instalación simplificado (2-clicks).";
                KofiBtn.Content = "☕ Ir a Ko-fi y ser Ultra Supporter";
                BackBtn.Content = "← Volver";
            }
            else
            {
                // English (already set in XAML)
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

        private void LearnMoreBtn_Click(object sender, RoutedEventArgs e)
        {
            // Hide first popup, show second
            FirstPopupPanel.Visibility = Visibility.Collapsed;
            SecondPopupPanel.Visibility = Visibility.Visible;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            // Hide second popup, show first
            SecondPopupPanel.Visibility = Visibility.Collapsed;
            FirstPopupPanel.Visibility = Visibility.Visible;
        }

        private void KofiBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open Ko-fi link
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "https://ko-fi.com/leuansin", // Replace with your actual Ko-fi link
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Ko-fi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ✅ NUEVO: Método para permitir arrastrar la ventana
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}