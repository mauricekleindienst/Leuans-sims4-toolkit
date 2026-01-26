using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModernDesign.MVVM.View
{
    public partial class DlcInfoWindow : Window
    {
        private readonly DLCInfo _dlc;

        public DlcInfoWindow(DLCInfo dlc)
        {
            InitializeComponent();
            _dlc = dlc ?? throw new ArgumentNullException(nameof(dlc));

            TitleText.Text = $"{_dlc.Name} ({_dlc.Id})";
            DlcNameText.Text = _dlc.Name;
            DlcIdText.Text = _dlc.Id;
            ShortDescriptionText.Text = _dlc.Description;
            UrlText.Text = _dlc.WikiUrl;

            // Imagen desde URL (si existe)
            if (!string.IsNullOrWhiteSpace(_dlc.ImageUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_dlc.ImageUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    DlcImage.Source = bitmap;
                }
                catch
                {
                    // Si falla, simplemente dejamos la imagen en blanco
                }
            }

            Loaded += DlcInfoWindow_Loaded;
        }

        private async void DlcInfoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SummaryText.Text = "Loading info from The Sims Wiki...";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string html = await client.GetStringAsync(_dlc.WikiUrl);

                    string summary = ExtractSummaryFromWikiHtml(html);

                    if (string.IsNullOrWhiteSpace(summary))
                    {
                        SummaryText.Text =
                            "Could not extract a clean summary from the wiki.\n" +
                            "You can open the full page in your browser using the button above.";
                    }
                    else
                    {
                        SummaryText.Text = summary;
                    }
                }
            }
            catch (Exception ex)
            {
                SummaryText.Text =
                    "Could not load info from the wiki.\n\n" +
                    "Error: " + ex.Message;
            }
        }

        private string ExtractSummaryFromWikiHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            // 1) Encontrar el div principal de contenido de Fandom: <div ... class="...mw-parser-output...">
            var divMatch = Regex.Match(
                html,
                "<div[^>]*class=\"[^\"]*mw-parser-output[^\"]*\"[^>]*>",
                RegexOptions.IgnoreCase);

            int startIndex;
            if (divMatch.Success)
            {
                startIndex = divMatch.Index + divMatch.Length;
            }
            else
            {
                // Si no lo encontramos, probamos desde el principio
                startIndex = 0;
            }

            // 2) Definir un “fin” aproximado del bloque de intro:
            // antes de la primera sección <h2 ...> o del índice <div id="toc">
            int endIndex = html.IndexOf("<h2", startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
                endIndex = html.IndexOf("<div id=\"toc\"", startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
                endIndex = html.Length;

            string content = html.Substring(startIndex, endIndex - startIndex);

            // 3) Extraer todos los <p>...</p> dentro de ese bloque
            var pMatches = Regex.Matches(
                content,
                "<p[^>]*>(.*?)</p>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (pMatches.Count == 0)
                return null;

            var sb = new StringBuilder();
            int paragraphsFound = 0;
            const int maxParagraphs = 4; // los primeros 3–4 párrafos de la intro

            foreach (Match m in pMatches)
            {
                string innerHtml = m.Groups[1].Value;
                string text = StripHtml(innerHtml).Trim();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                // A veces hay párrafos tipo "Coordinates:" o cosas vacías, los saltamos por ser muy cortos
                if (text.Length < 20)
                    continue;

                sb.AppendLine(text);
                sb.AppendLine();
                paragraphsFound++;

                if (paragraphsFound >= maxParagraphs)
                    break;
            }

            var result = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        private string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Quitar scripts/estilos por si acaso
            input = Regex.Replace(input, "<script.*?</script>", string.Empty,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            input = Regex.Replace(input, "<style.*?</style>", string.Empty,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Quitar todas las etiquetas HTML
            string noTags = Regex.Replace(input, "<.*?>", string.Empty);

            // Decodificar entidades HTML (&amp;, &quot;, etc.)
            string decoded = System.Net.WebUtility.HtmlDecode(noTags);

            // Normalizar espacios
            decoded = Regex.Replace(decoded, @"\s+", " ").Trim();

            return decoded;
        }


        private void OpenWikiInBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = _dlc.WikiUrl,
                    UseShellExecute = false
                });
            }
            catch
            {
                MessageBox.Show(
                    "Could not open the browser. Please copy the link manually:\n" + _dlc.WikiUrl,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
