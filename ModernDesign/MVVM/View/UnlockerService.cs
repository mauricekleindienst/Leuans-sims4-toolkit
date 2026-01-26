using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO.Compression;

namespace ModernDesign.MVVM.View
{
    public static class UnlockerService
    {
        // Shared HttpClient for downloads
        private static readonly HttpClient _httpClient = new HttpClient();

        // Unlocker working folder (NOT in TEMP, setup.bat rejects TEMP paths)
        private static readonly string _unlockerFolder;

        // Unlocker package URL (.zip with setup + config + g_The Sims 4.ini)
        private const string UnlockerPackageUrl = "https://github.com/Leuansin/leuan-dlcs/releases/download/unlocker/Unlocker.zip";

        // AppData folder structure
        private const string CommonDir = @"anadius\EA DLC Unlocker v2";

        // Static constructor: runs once
        static UnlockerService()
        {
            // Use AppData\Local instead of TEMP (setup.bat rejects TEMP paths)
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _unlockerFolder = Path.Combine(localAppData, "LeuansSims4Toolkit", "DLCUnlocker");

            if (!Directory.Exists(_unlockerFolder))
            {
                Directory.CreateDirectory(_unlockerFolder);
            }
        }

        // ===================== PUBLIC API =====================

        private static void DeleteFileSafe(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }

        private static void DeleteDirectorySafe(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Returns true if the unlocker is installed for EA app / Origin.
        /// </summary>
        public static bool IsUnlockerInstalled(out string clientName)
        {
            clientName = null;

            if (!TryGetClientPath(out var clientPath, out var clientId, out var friendlyName))
                return false;

            clientName = friendlyName;

            var dstDll = Path.Combine(clientPath, "version.dll");
            var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDataDir = Path.Combine(appDataRoot, CommonDir);
            var dstConfig = Path.Combine(appDataDir, "config.ini");

            return File.Exists(dstDll) && File.Exists(dstConfig);
        }

        /// <summary>
        /// Downloads the unlocker package and runs setup.bat in AUTO mode
        /// </summary>
        public static async Task InstallUnlockerAsync()
        {
            await DownloadUnlockerPackageAsync();

            // Descargar g_The Sims 4.ini desde GitHub
            await DownloadGameConfigAsync();

            // Ejecutar setup.bat en modo AUTO (parámetro "auto")
            RunUnlockerScriptAuto();

            // Esperar más tiempo para que complete la instalación automática
            // En modo auto es más rápido porque no hay interacción del usuario
            await Task.Delay(15000);

            // NO limpiar archivos aquí - el usuario puede necesitar ejecutar setup.bat manualmente después
            // Solo limpia cuando confirmes que está instalado
        }

        // ===================== INTERNAL HELPERS =====================

        /// <summary>
        /// Downloads and extracts the unlocker package to LocalAppData (NOT TEMP)
        /// </summary>
        private static async Task DownloadUnlockerPackageAsync()
        {
            var tempZip = Path.Combine(Path.GetTempPath(), "LeuansSims4_Unlocker.zip");

            await DownloadWithResumeAsync(UnlockerPackageUrl, tempZip);

            try
            {
                await Task.Run(() =>
                {
                    // Clean old files first
                    DeleteFileSafe(Path.Combine(_unlockerFolder, "setup.bat"));
                    DeleteFileSafe(Path.Combine(_unlockerFolder, "setup_macos.sh"));
                    DeleteFileSafe(Path.Combine(_unlockerFolder, "setup_linux.sh"));
                    DeleteFileSafe(Path.Combine(_unlockerFolder, "setup.exe"));
                    DeleteFileSafe(Path.Combine(_unlockerFolder, "g_The Sims 4.ini"));
                    DeleteFileSafe(Path.Combine(_unlockerFolder, "config.ini"));
                    DeleteDirectorySafe(Path.Combine(_unlockerFolder, "ea_app"));
                    DeleteDirectorySafe(Path.Combine(_unlockerFolder, "origin"));

                    // Extract to LocalAppData folder (NOT TEMP)
                    ZipFile.ExtractToDirectory(tempZip, _unlockerFolder);
                });
            }
            finally
            {
                DeleteFileSafe(tempZip);
            }

            var batPath = Path.Combine(_unlockerFolder, "setup.bat");
            if (!File.Exists(batPath))
            {
                throw new FileNotFoundException(
                    "The unlocker package was extracted, but 'setup.bat' was not found.",
                    batPath);
            }
        }

        /// <summary>
        /// Descarga g_The Sims 4.ini desde GitHub
        /// </summary>
        private static async Task DownloadGameConfigAsync()
        {
            var gameConfigPath = Path.Combine(_unlockerFolder, "g_The Sims 4.ini");

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var gameConfigContent = await client.GetStringAsync(
                        "https://raw.githubusercontent.com/Leuansin/Leuans-sims4-toolkit/main/Misc/g_s4_db.ini");

                    File.WriteAllText(gameConfigPath, gameConfigContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download game configuration: {ex.Message}", ex);
            }

            if (!File.Exists(gameConfigPath))
            {
                throw new FileNotFoundException(
                    "Failed to download 'g_The Sims 4.ini'.",
                    gameConfigPath);
            }
        }

        /// <summary>
        /// Runs setup.bat in AUTO mode (non-interactive installation)
        /// This automatically installs the unlocker and copies The Sims 4 config
        /// </summary>
        private static void RunUnlockerScriptAuto()
        {
            var batPath = Path.Combine(_unlockerFolder, "setup.bat");

            if (!File.Exists(batPath))
            {
                throw new FileNotFoundException("setup.bat was not found in the unlocker folder.", batPath);
            }

            var psi = new ProcessStartInfo
            {
                FileName = batPath,
                Arguments = "auto", // Modo automático - instala sin interacción
                WorkingDirectory = _unlockerFolder,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden, // Oculto porque es automático
                Verb = "runas" // Pedir permisos de admin directamente
            };

            try
            {
                var process = Process.Start(psi);
                process?.WaitForExit(15000); // Esperar hasta 15 segundos
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to run setup.bat in auto mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleans up unlocker files after installation
        /// </summary>
        private static void CleanupUnlockerFiles()
        {
            try
            {
                DeleteFileSafe(Path.Combine(_unlockerFolder, "setup.bat"));
                DeleteFileSafe(Path.Combine(_unlockerFolder, "setup.exe"));
                DeleteFileSafe(Path.Combine(_unlockerFolder, "setup_linux.sh"));
                DeleteFileSafe(Path.Combine(_unlockerFolder, "setup_macos.sh"));
                DeleteFileSafe(Path.Combine(_unlockerFolder, "g_The Sims 4.ini"));
                DeleteFileSafe(Path.Combine(_unlockerFolder, "config.ini"));

                DeleteDirectorySafe(Path.Combine(_unlockerFolder, "origin"));
                DeleteDirectorySafe(Path.Combine(_unlockerFolder, "ea_app"));

                // Try to delete the folder itself if empty
                try
                {
                    if (Directory.Exists(_unlockerFolder) && Directory.GetFileSystemEntries(_unlockerFolder).Length == 0)
                    {
                        Directory.Delete(_unlockerFolder);
                    }
                }
                catch { }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // ===================== CLIENT DETECTION (EA app / Origin) =====================

        private static bool TryGetClientPath(out string clientPath, out string clientId, out string clientFriendlyName)
        {
            clientPath = null;
            clientId = null;
            clientFriendlyName = null;

            // EA Desktop (EA app) first
            if (TryGetClientPathFromRegistry(@"Electronic Arts\EA Desktop", out var eaClientPath))
            {
                clientPath = eaClientPath;
                clientId = "ea_app";
                clientFriendlyName = "EA app";
                return true;
            }

            // Origin 32-bit key
            if (TryGetClientPathFromRegistry(@"WOW6432Node\Origin", out var origin32Path))
            {
                clientPath = origin32Path;
                clientId = "origin";
                clientFriendlyName = "Origin";
                return true;
            }

            // Origin normal key
            if (TryGetClientPathFromRegistry(@"Origin", out var originPath))
            {
                clientPath = originPath;
                clientId = "origin";
                clientFriendlyName = "Origin";
                return true;
            }

            return false;
        }

        private static bool TryGetClientPathFromRegistry(string subKey, out string clientPath)
        {
            clientPath = null;

            try
            {
                // Try 64-bit view first
                using (var key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                              .OpenSubKey(@"SOFTWARE\" + subKey))
                {
                    if (key64 != null)
                    {
                        var cp = key64.GetValue("ClientPath") as string;
                        if (!string.IsNullOrEmpty(cp) && File.Exists(cp))
                        {
                            clientPath = Directory.GetParent(cp).FullName;
                            return true;
                        }
                    }
                }

                // Try 32-bit view
                using (var key32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                                              .OpenSubKey(@"SOFTWARE\" + subKey))
                {
                    if (key32 != null)
                    {
                        var cp = key32.GetValue("ClientPath") as string;
                        if (!string.IsNullOrEmpty(cp) && File.Exists(cp))
                        {
                            clientPath = Directory.GetParent(cp).FullName;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore registry errors
            }

            return false;
        }

        // ===================== DOWNLOAD + RESUME =====================

        private static async Task DownloadWithResumeAsync(string url, string tempFilePath)
        {
            long existingLength = 0;

            if (File.Exists(tempFilePath))
            {
                var info = new FileInfo(tempFilePath);
                existingLength = info.Length;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (existingLength > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existingLength, null);
            }

            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.StatusCode == HttpStatusCode.OK && existingLength > 0)
                {
                    using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                else if (response.StatusCode == HttpStatusCode.PartialContent || existingLength == 0)
                {
                    using (var fs = new FileStream(
                               tempFilePath,
                               existingLength > 0 ? FileMode.Append : FileMode.Create,
                               FileAccess.Write,
                               FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                else
                {
                    throw new Exception($"Unexpected HTTP response: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
        }

        // ===================== UNINSTALL =====================

        public static async Task UninstallUnlockerAsync()
        {
            await Task.Run(() =>
            {
                if (TryGetClientPath(out var clientPath, out var clientId, out var friendlyName))
                {
                    var dstDll = Path.Combine(clientPath, "version.dll");
                    DeleteFileSafe(dstDll);

                    if (clientId == "ea_app")
                    {
                        try
                        {
                            var parentOfClient = Directory.GetParent(clientPath)?.FullName;
                            if (!string.IsNullOrEmpty(parentOfClient))
                            {
                                var stagedDir = Path.Combine(parentOfClient, @"StagedEADesktop\EA Desktop");
                                var stagedDll = Path.Combine(stagedDir, "version.dll");
                                DeleteFileSafe(stagedDll);
                            }

                            var psi = new ProcessStartInfo
                            {
                                FileName = "schtasks",
                                Arguments = "/Delete /TN copy_dlc_unlocker /F",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
                            using (var proc = Process.Start(psi))
                            {
                                proc?.WaitForExit(5000);
                            }
                        }
                        catch { }
                    }

                    var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var appDataDir = Path.Combine(appDataRoot, CommonDir);

                    var localAppDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var localAppDataDir = Path.Combine(localAppDataRoot, CommonDir);

                    DeleteFolderRecursively(appDataDir);
                    DeleteFolderIfEmptyParent(appDataDir);

                    DeleteFolderRecursively(localAppDataDir);
                    DeleteFolderIfEmptyParent(localAppDataDir);
                }

                CleanupUnlockerFiles();
            });
        }

        private static void DeleteFolderRecursively(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch { }
        }

        private static void DeleteFolderIfEmptyParent(string directory)
        {
            try
            {
                var parent = Directory.GetParent(directory);
                if (parent != null && Directory.Exists(parent.FullName))
                {
                    if (Directory.GetFileSystemEntries(parent.FullName).Length == 0)
                    {
                        Directory.Delete(parent.FullName);
                    }
                }
            }
            catch { }
        }

        public static void CleanLocalUnlockerFiles()
        {
            CleanupUnlockerFiles();
        }
    }
}