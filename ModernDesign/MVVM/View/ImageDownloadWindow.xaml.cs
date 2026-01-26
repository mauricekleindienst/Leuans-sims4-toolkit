using System;
using System.Windows;

namespace ModernDesign.MVVM.View
{
    public partial class ImageDownloadWindow : Window
    {
        private int _totalImages;
        private int _downloadedImages;

        public ImageDownloadWindow(int totalImages, bool isSpanish)
        {
            InitializeComponent();
            _totalImages = totalImages;
            _downloadedImages = 0;

            // Configurar idioma
            if (isSpanish)
            {
                TitleText.Text = "Descargando Imágenes de DLC";
                ExplanationText.Text = "Este proceso solo debe realizarse una vez.\nLas imágenes se almacenarán localmente para uso futuro.";
                ProgressText.Text = $"Descargando: 0 / {_totalImages}";
            }
            else
            {
                TitleText.Text = "Downloading DLC Images";
                ExplanationText.Text = "This process only needs to be done once.\nImages will be cached locally for future use.";
                ProgressText.Text = $"Downloading: 0 / {_totalImages}";
            }
        }

        public void UpdateProgress(int downloaded, string currentFileName, bool isSpanish)
        {
            Dispatcher.Invoke(() =>
            {
                _downloadedImages = downloaded;
                double percent = (_downloadedImages * 100.0) / _totalImages;

                // Actualizar texto de progreso
                if (isSpanish)
                {
                    ProgressText.Text = $"Descargando: {_downloadedImages} / {_totalImages}";
                }
                else
                {
                    ProgressText.Text = $"Downloading: {_downloadedImages} / {_totalImages}";
                }

                // Actualizar barra de progreso
                ProgressBar.Width = (percent / 100.0) * 440; // 440 es el ancho total aproximado
                PercentText.Text = $"{percent:F0}%";

                // Actualizar archivo actual
                CurrentFileText.Text = currentFileName;
            });
        }

        public void Complete(bool isSpanish)
        {
            Dispatcher.Invoke(() =>
            {
                if (isSpanish)
                {
                    TitleText.Text = " Descarga Completada";
                    ProgressText.Text = $"Completado: {_totalImages} / {_totalImages}";
                    CurrentFileText.Text = "Todas las imágenes han sido descargadas.";
                }
                else
                {
                    TitleText.Text = " Download Complete";
                    ProgressText.Text = $"Completed: {_totalImages} / {_totalImages}";
                    CurrentFileText.Text = "All images have been downloaded.";
                }

                ProgressBar.Width = 440;
                PercentText.Text = "100%";

                // Cerrar automáticamente después de 1.5 segundos
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1500);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    this.Close();
                };
                timer.Start();
            });
        }
    }
}