using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModernDesign.Profile
{
    public enum MedalType
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3
    }

    public class UserProfile
    {
        public string UserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public Dictionary<string, MedalType> TutorialMedals { get; set; }
        public string BackgroundColor { get; set; } = "#22D3EE,#1E293B,#21b96b"; // Colores por defecto separados por comas
        public string SelectedAvatar { get; set; } = "👤"; // Avatar por defecto

        public UserProfile()
        {
            TutorialMedals = new Dictionary<string, MedalType>();
        }
    }

    public static class ProfileManager
    {
        private static UserProfile _currentProfile;
        private static readonly string ProfilePath;

        static ProfileManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string toolkitFolder = Path.Combine(appData, "Leuan's - Sims 4 ToolKit");
            ProfilePath = Path.Combine(toolkitFolder, "Profile.ini");
        }

        public static UserProfile CurrentProfile
        {
            get
            {
                if (_currentProfile == null)
                    LoadProfile();
                return _currentProfile;
            }
        }

        public static bool ProfileExists()
        {
            return File.Exists(ProfilePath);
        }

        public static void CreateProfile(string userName)
        {
            _currentProfile = new UserProfile
            {
                UserName = userName,
                CreatedDate = DateTime.Now,
                TutorialMedals = new Dictionary<string, MedalType>()
            };

            SaveProfile();
        }

        public static void LoadProfile()
        {
            if (!File.Exists(ProfilePath))
            {
                _currentProfile = null;
                return;
            }

            try
            {
                _currentProfile = new UserProfile();
                var lines = File.ReadAllLines(ProfilePath);
                bool inMedalsSection = false;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    if (line.Trim() == "[Medals]")
                    {
                        inMedalsSection = true;
                        continue;
                    }

                    if (line.Contains("="))
                    {
                        var parts = line.Split('=');
                        if (parts.Length != 2) continue;

                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        if (!inMedalsSection)
                        {
                            // Sección [Profile]
                            if (key == "UserName")
                            {
                                _currentProfile.UserName = value;
                            }
                            else if (key == "CreatedDate")
                            {
                                DateTime tempDate;
                                if (DateTime.TryParse(value, out tempDate))
                                {
                                    _currentProfile.CreatedDate = tempDate;
                                }
                            }
                        }
                        else
                        {
                            // Sección [Medals]
                            MedalType medal;
                            if (Enum.TryParse<MedalType>(value, out medal))
                            {
                                _currentProfile.TutorialMedals[key] = medal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile: {ex.Message}");
                _currentProfile = null;
            }
        }

        public static void SaveProfile()
        {
            if (_currentProfile == null)
                return;

            try
            {
                string directory = Path.GetDirectoryName(ProfilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var lines = new List<string>
                {
                    "# ------------------------------------ Leuan's - Sims 4 ToolKit ------------------------------------",
                    "# User Profile Configuration",
                    "# This file stores your username and tutorial progress (medals)",
                    "# ",
                    "\"Digital Culture was born to be shared,",
                    "but the big companies locked it away.",
                    "Piracy is just the human gesture",
                    "of ensuring that culture remains accessible to everyone,",
                    "and not only to those who can afford the luxury of paying.\"",
                    " ",
                    "\"La cultura digital nació para compartirse,",
                    "pero las grandes compañías lo privatizaron.",
                    "La piratería es solo un gesto humano que se asegura que esta cultura sea accesible para todos,",
                    "    y no solo para aquellos que tienen el lujo de poder pagar.\"",
                    "# ------------------------------------ Leuan's - Sims 4 ToolKit ------------------------------------",
                    "",
                    "[Profile]",
                    $"UserName = {_currentProfile.UserName}",
                    $"CreatedDate = {_currentProfile.CreatedDate:yyyy-MM-dd HH:mm:ss}",
                    "",
                    "[Misc]",
                    "randomizeLoadingScreen = false",
                    "PreloadImages = false",
                    "LoadDLCImages = false",
                    "",
                    "[Medals]"
                };

                foreach (var kvp in _currentProfile.TutorialMedals.OrderBy(x => x.Key))
                {
                    lines.Add($"{kvp.Key} = {kvp.Value}");
                }

                File.WriteAllLines(ProfilePath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex.Message}");
            }
        }

        public static MedalType GetTutorialMedal(string tutorialId)
        {
            if (_currentProfile == null)
                LoadProfile();

            if (_currentProfile != null && _currentProfile.TutorialMedals.ContainsKey(tutorialId))
                return _currentProfile.TutorialMedals[tutorialId];

            return MedalType.None;
        }

        public static void SetTutorialMedal(string tutorialId, MedalType medal)
        {
            if (_currentProfile == null)
                LoadProfile();

            if (_currentProfile == null)
                return;

            // Solo actualizar si la nueva medalla es mejor que la actual
            if (_currentProfile.TutorialMedals.ContainsKey(tutorialId))
            {
                if (medal > _currentProfile.TutorialMedals[tutorialId])
                    _currentProfile.TutorialMedals[tutorialId] = medal;
            }
            else
            {
                _currentProfile.TutorialMedals[tutorialId] = medal;
            }

            SaveProfile();
        }

        public static int GetTotalMedals(MedalType type)
        {
            if (_currentProfile == null)
                LoadProfile();

            if (_currentProfile == null)
                return 0;

            return _currentProfile.TutorialMedals.Count(x => x.Value == type);
        }

        public static int GetTotalMedalsCount()
        {
            if (_currentProfile == null)
                LoadProfile();

            if (_currentProfile == null)
                return 0;

            return _currentProfile.TutorialMedals.Count(x => x.Value != MedalType.None);
        }
    }
}