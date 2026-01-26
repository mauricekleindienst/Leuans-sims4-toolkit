using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ModernDesign.Profile; // Restaurado para MedalType

namespace ModernDesign.Managers
{
    public static class DeveloperModeManager
    {
        private static readonly string AppDataRoaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Leuan's - Sims 4 ToolKit");
        private static readonly string AppDataLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LTK");

        private static readonly string ProgressFilePath = Path.Combine(AppDataRoaming, "progress.ini");
        private static readonly string ProfileIniPath = Path.Combine(AppDataRoaming, "profile.ini");
        private static readonly string TmpFile2025Path = Path.Combine(AppDataLocal, "tmpFile2025.ini");

        private static readonly string KeyUrl = "https://zeroauno.blob.core.windows.net/leuan/Skaparabipbop.txt?sp=r&st=2026-01-18T20:03:19Z&se=2027-01-19T04:18:19Z&spr=https&sv=2024-11-04&sr=b&sig=g6IfDljJD1%2FKqHhQCl%2Fu1v%2FpBLd0RSnSGO1eHEnVpl8%3D";

        private static readonly string[] RequiredFeatures = new string[]
        {
            "install_mods", "mod_manager", "loading_screen", "cheats_guide",
            "gallery_manager", "gameplay_enhancer", "fix_common_errors", "method_5050"
        };

        // --- MÉTODOS DE ESTADO ---

        public static bool IsDeveloperModeUnlocked()
        {
            // Versión síncrona para la UI
            return HasAllGoldMedals() && AreAllFeaturesVisited() && HasDonated();
        }

        public static async Task<bool> IsDeveloperModeUnlockedAsync()
        {
            return HasAllGoldMedals() && AreAllFeaturesVisited() && await HasDonatedAsync();
        }

        public static bool HasAllGoldMedals()
        {
            string[] tutorialIds = { "beginner_guide", "tutorial_trait", "tutorial_interaction", "tutorial_career", "tutorial_buff", "tutorial_clothing", "tutorial_object" };

            try
            {
                foreach (var id in tutorialIds)
                {
                    if (ProfileManager.GetTutorialMedal(id) != MedalType.Gold)
                        return false;
                }
                return true;
            }
            catch { return false; }
        }

        public static bool AreAllFeaturesVisited()
        {
            foreach (var feature in RequiredFeatures)
            {
                if (!IsFeatureVisited(feature)) return false;
            }
            return true;
        }

        public static async Task<bool> HasDonatedAsync()
        {
            try
            {
                if (!File.Exists(TmpFile2025Path) || !File.Exists(ProfileIniPath)) return false;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string validKey = (await client.GetStringAsync(KeyUrl)).Trim();

                    var profileLines = File.ReadAllLines(ProfileIniPath);
                    string profileKey = profileLines.FirstOrDefault(l => l.StartsWith("key="))?.Split('=')[1].Trim();
                    bool isSupporter = profileLines.Any(l => l.Trim().ToLower() == "ispatreonsupporter=true");

                    string tmpKey = File.ReadAllLines(TmpFile2025Path).FirstOrDefault(l => l.StartsWith("key="))?.Split('=')[1].Trim();

                    return isSupporter && tmpKey == validKey && profileKey == validKey;
                }
            }
            catch { return false; }
        }

        public static bool HasDonated()
        {
            // Verificación rápida local para no bloquear la UI con peticiones web
            try
            {
                if (!File.Exists(TmpFile2025Path) || !File.Exists(ProfileIniPath)) return false;

                string tmpKey = File.ReadAllLines(TmpFile2025Path).FirstOrDefault(l => l.StartsWith("key="))?.Split('=')[1].Trim();
                string profileKey = File.ReadAllLines(ProfileIniPath).FirstOrDefault(l => l.StartsWith("key="))?.Split('=')[1].Trim();
                bool isSupporter = File.ReadAllText(ProfileIniPath).ToLower().Contains("ispatreonsupporter=true");

                return isSupporter && !string.IsNullOrEmpty(tmpKey) && tmpKey == profileKey;
            }
            catch { return false; }
        }

        // --- GESTIÓN DE FEATURES ---

        public static void MarkFeatureAsVisited(string featureId)
        {
            try
            {
                var lines = File.Exists(ProgressFilePath) ? File.ReadAllLines(ProgressFilePath).ToList() : new System.Collections.Generic.List<string>();
                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith($"{featureId}="))
                    {
                        lines[i] = $"{featureId}=true";
                        found = true;
                        break;
                    }
                }
                if (!found) lines.Add($"{featureId}=true");
                File.WriteAllLines(ProgressFilePath, lines);
            }
            catch { }
        }

        public static bool IsFeatureVisited(string featureId)
        {
            try
            {
                if (!File.Exists(ProgressFilePath)) return false;
                return File.ReadAllLines(ProgressFilePath).Any(l => l.Trim() == $"{featureId}=true");
            }
            catch { return false; }
        }

        // --- PROGRESO PARA LA UI ---

        public static DeveloperModeProgress GetProgress()
        {
            int visited = RequiredFeatures.Count(f => IsFeatureVisited(f));
            return new DeveloperModeProgress
            {
                HasAllGoldMedals = HasAllGoldMedals(),
                AllFeaturesVisited = AreAllFeaturesVisited(),
                HasDonated = HasDonated(),
                FeaturesVisited = visited,
                TotalFeatures = RequiredFeatures.Length
            };
        }
    }

    public class DeveloperModeProgress
    {
        public bool HasAllGoldMedals { get; set; }
        public bool AllFeaturesVisited { get; set; }
        public bool HasDonated { get; set; }
        public int FeaturesVisited { get; set; }
        public int TotalFeatures { get; set; }

        public bool IsUnlocked => HasAllGoldMedals && AllFeaturesVisited && HasDonated;
        public int ProgressPercentage
        {
            get
            {
                double score = 0;
                if (HasAllGoldMedals) score += 33.33;
                if (AllFeaturesVisited) score += 33.33;
                if (HasDonated) score += 33.34;
                return (int)score;
            }
        }
    }
}