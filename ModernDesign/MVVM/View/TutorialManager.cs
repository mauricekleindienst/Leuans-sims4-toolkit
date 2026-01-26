using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModernDesign.Managers
{
    public static class TutorialManager
    {
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string ToolkitFolder = Path.Combine(AppDataPath, "Leuan's - Sims 4 ToolKit");
        private static readonly string TutorialIniPath = Path.Combine(ToolkitFolder, "tutorial.ini");
        private static readonly string TutorialsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tutorials");
        private static readonly string CacheFolder = Path.Combine(ToolkitFolder, "TutorialsCache");

        // GitHub base URL
        private const string GitHubBaseUrl = "https://raw.githubusercontent.com/Johnn-sin/leuansin-dlcs/refs/heads/main/tutoriales/";

        public static bool HasCompletedTutorial()
        {
            try
            {
                // Crear carpeta si no existe
                if (!Directory.Exists(ToolkitFolder))
                {
                    Directory.CreateDirectory(ToolkitFolder);
                }

                // Si no existe el archivo, crearlo con false
                if (!File.Exists(TutorialIniPath))
                {
                    CreateTutorialIni(false);
                    return false;
                }

                // Leer el archivo
                var lines = File.ReadAllLines(TutorialIniPath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("hasCompletedTutorial", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split('=');
                        if (parts.Length == 2)
                        {
                            var value = parts[1].Trim().ToLower();
                            return value == "true";
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

        public static void SetTutorialCompleted(bool completed)
        {
            try
            {
                if (!Directory.Exists(ToolkitFolder))
                {
                    Directory.CreateDirectory(ToolkitFolder);
                }

                CreateTutorialIni(completed);
            }
            catch
            {
                // Silently fail
            }
        }

        private static void CreateTutorialIni(bool completed)
        {
            string content = $@"[Tutorial]
hasCompletedTutorial = {completed.ToString().ToLower()}";

            File.WriteAllText(TutorialIniPath, content);
        }

        /// <summary>
        /// Load tutorial data from GitHub (with local fallback) - ASYNC
        /// </summary>
        public static async Task<TutorialData> LoadTutorialAsync(string category)
        {
            // Asegurar que existe la carpeta de cache
            if (!Directory.Exists(CacheFolder))
            {
                Directory.CreateDirectory(CacheFolder);
            }

            // Intentar descargar desde GitHub
            var tutorialData = await DownloadTutorialFromGitHub(category);

            if (tutorialData != null)
            {
                // Guardar en cache
                SaveToCache(category, tutorialData);
                return tutorialData;
            }

            // Si falla GitHub, intentar cache
            tutorialData = LoadFromCache(category);
            if (tutorialData != null)
            {
                return tutorialData;
            }

            // Si falla cache, intentar local
            tutorialData = LoadFromLocal(category);
            if (tutorialData != null)
            {
                return tutorialData;
            }

            // Último recurso: tutorial vacío
            return new TutorialData
            {
                Id = category,
                DisplayName = new Dictionary<string, string>
                {
                    { "en", category },
                    { "es", category }
                },
                Steps = new List<TutorialStep>()
            };
        }

        /// <summary>
        /// Load tutorial data from GitHub (with local fallback) - SYNC (for backward compatibility)
        /// </summary>
        public static TutorialData LoadTutorial(string category)
        {
            return LoadTutorialAsync(category).GetAwaiter().GetResult();
        }

        private static async Task<TutorialData> DownloadTutorialFromGitHub(string category)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5); // 5 segundos timeout (más rápido)

                    string url = $"{GitHubBaseUrl}{category}.json";
                    string json = await client.GetStringAsync(url);

                    return JsonConvert.DeserializeObject<TutorialData>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading tutorial from GitHub: {ex.Message}");
                return null;
            }
        }

        private static TutorialData LoadFromCache(string category)
        {
            try
            {
                string cachePath = Path.Combine(CacheFolder, $"{category}.json");

                if (!File.Exists(cachePath))
                {
                    return null;
                }

                string json = File.ReadAllText(cachePath);
                return JsonConvert.DeserializeObject<TutorialData>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tutorial from cache: {ex.Message}");
                return null;
            }
        }

        private static TutorialData LoadFromLocal(string category)
        {
            try
            {
                string localPath = Path.Combine(TutorialsFolder, $"{category}.json");

                if (!File.Exists(localPath))
                {
                    return null;
                }

                string json = File.ReadAllText(localPath);
                return JsonConvert.DeserializeObject<TutorialData>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tutorial from local: {ex.Message}");
                return null;
            }
        }

        private static void SaveToCache(string category, TutorialData data)
        {
            try
            {
                string cachePath = Path.Combine(CacheFolder, $"{category}.json");
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(cachePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving tutorial to cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Get list of available tutorials
        /// </summary>
        public static List<string> GetAvailableTutorials()
        {
            var tutorials = new List<string>();

            try
            {
                // Buscar en cache primero
                if (Directory.Exists(CacheFolder))
                {
                    foreach (var file in Directory.GetFiles(CacheFolder, "*.json"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (!tutorials.Contains(name))
                        {
                            tutorials.Add(name);
                        }
                    }
                }

                // Luego en local
                if (Directory.Exists(TutorialsFolder))
                {
                    foreach (var file in Directory.GetFiles(TutorialsFolder, "*.json"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (!tutorials.Contains(name))
                        {
                            tutorials.Add(name);
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }

            return tutorials;
        }

        /// <summary>
        /// Clear tutorial cache (force re-download on next load)
        /// </summary>
        public static void ClearCache()
        {
            try
            {
                if (Directory.Exists(CacheFolder))
                {
                    Directory.Delete(CacheFolder, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing cache: {ex.Message}");
            }
        }
    }

    // Data classes for JSON deserialization
    public class TutorialData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public Dictionary<string, string> DisplayName { get; set; }

        [JsonProperty("steps")]
        public List<TutorialStep> Steps { get; set; }
    }

    public class TutorialStep
    {
        [JsonProperty("title")]
        public Dictionary<string, string> Title { get; set; }

        [JsonProperty("description")]
        public Dictionary<string, string> Description { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("label")]
        public Dictionary<string, string> Label { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("tips")]
        public Dictionary<string, string> Tips { get; set; }
    }
}
