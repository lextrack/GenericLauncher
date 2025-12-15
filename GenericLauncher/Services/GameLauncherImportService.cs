using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GenericLauncher.Services
{
    public class GameLauncherImportService
    {
        public class DetectedGame : INotifyPropertyChanged
        {
            private bool _isSelected = false;

            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Launcher { get; set; } = string.Empty;
            public string GameId { get; set; } = string.Empty;

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        public class LauncherInfo
        {
            public string Name { get; set; } = string.Empty;
            public bool IsInstalled { get; set; }
            public int GameCount { get; set; }
        }

        public List<LauncherInfo> DetectInstalledLaunchers()
        {
            var launchers = new List<LauncherInfo>();

            // Steam
            var steamPath = GetSteamInstallPath();
            var steamGames = steamPath != null ? DetectSteamGames(steamPath) : new List<DetectedGame>();
            launchers.Add(new LauncherInfo
            {
                Name = "Steam",
                IsInstalled = steamPath != null,
                GameCount = steamGames.Count
            });

            // Epic Games
            var epicGames = DetectEpicGames();
            launchers.Add(new LauncherInfo
            {
                Name = "Epic Games",
                IsInstalled = epicGames.Count > 0,
                GameCount = epicGames.Count
            });

            // GOG Galaxy
            var gogGames = DetectGOGGames();
            launchers.Add(new LauncherInfo
            {
                Name = "GOG Galaxy",
                IsInstalled = gogGames.Count > 0,
                GameCount = gogGames.Count
            });

            return launchers;
        }

        public List<DetectedGame> DetectAllGames()
        {
            var allGames = new List<DetectedGame>();

            // Steam
            var steamPath = GetSteamInstallPath();
            if (steamPath != null)
            {
                allGames.AddRange(DetectSteamGames(steamPath));
            }

            // Epic Games
            allGames.AddRange(DetectEpicGames());

            // GOG Galaxy
            allGames.AddRange(DetectGOGGames());

            return allGames;
        }

        private string? GetSteamInstallPath()
        {
            try
            {
                var steamPaths = new[]
                {
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Program Files\Steam",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
                };

                foreach (var path in steamPaths)
                {
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
                    {
                        return path;
                    }
                }

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var registryPath = key?.GetValue("SteamPath") as string;
                    if (registryPath != null && Directory.Exists(registryPath))
                    {
                        return registryPath;
                    }
                }
            }
            catch { }

            return null;
        }

        private List<DetectedGame> DetectSteamGames(string steamPath)
        {
            var games = new List<DetectedGame>();

            try
            {
                var libraryFolders = new List<string> { steamPath };

                var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFoldersPath))
                {
                    var content = File.ReadAllText(libraryFoldersPath);
                    var pathMatches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                    foreach (Match match in pathMatches)
                    {
                        var path = match.Groups[1].Value.Replace(@"\\", @"\");
                        if (Directory.Exists(path) && !libraryFolders.Contains(path))
                        {
                            libraryFolders.Add(path);
                        }
                    }
                }

                foreach (var libraryPath in libraryFolders)
                {
                    var steamappsPath = Path.Combine(libraryPath, "steamapps");
                    if (!Directory.Exists(steamappsPath)) continue;

                    var manifestFiles = Directory.GetFiles(steamappsPath, "appmanifest_*.acf");
                    foreach (var manifestFile in manifestFiles)
                    {
                        try
                        {
                            var manifestContent = File.ReadAllText(manifestFile);
                            var appIdMatch = Regex.Match(Path.GetFileName(manifestFile), @"appmanifest_(\d+)\.acf");
                            if (!appIdMatch.Success) continue;
                            var appId = appIdMatch.Groups[1].Value;

                            var nameMatch = Regex.Match(manifestContent, @"""name""\s+""([^""]+)""");
                            if (!nameMatch.Success) continue;
                            var gameName = nameMatch.Groups[1].Value;

                            var stateMatch = Regex.Match(manifestContent, @"""StateFlags""\s+""(\d+)""");
                            if (stateMatch.Success)
                            {
                                var state = int.Parse(stateMatch.Groups[1].Value);
                                if ((state & 4) != 4) continue;
                            }

                            games.Add(new DetectedGame
                            {
                                Name = gameName,
                                Path = $"steam://rungameid/{appId}",
                                Launcher = "Steam",
                                GameId = appId
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return games;
        }

        private List<DetectedGame> DetectEpicGames()
        {
            var games = new List<DetectedGame>();

            try
            {
                var manifestsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Epic", "EpicGamesLauncher", "Data", "Manifests");

                if (!Directory.Exists(manifestsPath)) return games;

                var manifestFiles = Directory.GetFiles(manifestsPath, "*.item");
                foreach (var manifestFile in manifestFiles)
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(manifestFile);
                        using var doc = JsonDocument.Parse(jsonContent);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("bIsIncompleteInstall", out var incomplete) && incomplete.GetBoolean())
                            continue;

                        var displayName = root.GetProperty("DisplayName").GetString();
                        var installLocation = root.GetProperty("InstallLocation").GetString();
                        var launchExecutable = root.GetProperty("LaunchExecutable").GetString();
                        var appName = root.GetProperty("AppName").GetString();

                        if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(installLocation) || string.IsNullOrEmpty(launchExecutable))
                            continue;

                        var executablePath = Path.Combine(installLocation, launchExecutable);

                        if (File.Exists(executablePath))
                        {
                            games.Add(new DetectedGame
                            {
                                Name = displayName,
                                Path = executablePath,
                                Launcher = "Epic Games",
                                GameId = appName ?? ""
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return games;
        }

        private List<DetectedGame> DetectGOGGames()
        {
            var games = new List<DetectedGame>();

            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games"))
                {
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var gameKey = key.OpenSubKey(subKeyName))
                            {
                                if (gameKey == null) continue;

                                var gameName = gameKey.GetValue("GAMENAME") as string;
                                var exePath = gameKey.GetValue("EXE") as string;
                                var workingDir = gameKey.GetValue("WORKINGDIR") as string;

                                if (!string.IsNullOrEmpty(gameName) && !string.IsNullOrEmpty(exePath))
                                {
                                    var fullPath = exePath;
                                    if (!Path.IsPathRooted(exePath) && !string.IsNullOrEmpty(workingDir))
                                    {
                                        fullPath = Path.Combine(workingDir, exePath);
                                    }

                                    if (File.Exists(fullPath))
                                    {
                                        games.Add(new DetectedGame
                                        {
                                            Name = gameName,
                                            Path = fullPath,
                                            Launcher = "GOG Galaxy",
                                            GameId = subKeyName
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return games;
        }
    }
}
