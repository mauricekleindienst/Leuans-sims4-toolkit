using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ModernDesign.MVVM.View
{
    public partial class AvatarSelectorWindow : Window
    {
        public string SelectedAvatar { get; private set; }
        public bool IsCustomAvatar { get; private set; }

        private readonly string customAvatarPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Leuan's - Sims 4 ToolKit",
            "qol",
            "avatar.png"
        );

        public AvatarSelectorWindow()
        {
            InitializeComponent();

            // Asegurar que el directorio existe
            string directory = Path.GetDirectoryName(customAvatarPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void Avatar_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border != null)
            {
                SelectedAvatar = border.Tag.ToString();
                IsCustomAvatar = false;
                DialogResult = true;
                Close();
            }
        }

        private void UploadCustomAvatar_Click(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Your Avatar Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Cargar la imagen original
                    BitmapImage originalImage = new BitmapImage(new Uri(openFileDialog.FileName));

                    // Crear una imagen redimensionada (opcional, para optimizar)
                    int targetSize = 200; // Tamaño objetivo en píxeles
                    BitmapImage resizedImage = ResizeImage(originalImage, targetSize, targetSize);

                    // Guardar la imagen en la ubicación especificada
                    SaveImageAsPng(resizedImage, customAvatarPath);

                    // Establecer el avatar personalizado
                    SelectedAvatar = customAvatarPath;
                    IsCustomAvatar = true;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error uploading avatar: {ex.Message}",
                        "Upload Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private BitmapImage ResizeImage(BitmapImage source, int width, int height)
        {
            // Crear un DrawingVisual para renderizar la imagen redimensionada
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, new Rect(0, 0, width, height));
            }

            // Renderizar a un RenderTargetBitmap
            var resizedImage = new RenderTargetBitmap(
                width, height,
                96, 96,
                System.Windows.Media.PixelFormats.Pbgra32
            );
            resizedImage.Render(drawingVisual);

            return BitmapImageFromBitmapSource(resizedImage);
        }

        private BitmapImage BitmapImageFromBitmapSource(System.Windows.Media.Imaging.BitmapSource bitmapSource)
        {
            var encoder = new PngBitmapEncoder();
            var memoryStream = new MemoryStream();
            var bitmapImage = new BitmapImage();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            memoryStream.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private void SaveImageAsPng(BitmapImage image, string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}