using GenericLauncher.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GenericLauncher.Services
{
    public class AppDetectionService
    {
        private readonly IconExtractionService _iconExtractionService;

        public AppDetectionService()
        {
            _iconExtractionService = new IconExtractionService();
        }

        public class DetectedApp : System.ComponentModel.INotifyPropertyChanged
        {
            private bool _isSelected = false;

            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
                    }
                }
            }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        }

        public List<DetectedApp> ScanForApplications()
        {
            var detectedApps = new List<DetectedApp>();

            try
            {
                ScanDirectory(detectedApps, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Desktop");
                ScanDirectory(detectedApps, Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Start Menu");
                ScanDirectory(detectedApps, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Start Menu");

                ScanDirectory(detectedApps, @"C:\Program Files", "Program Files", maxDepth: 2);
                ScanDirectory(detectedApps, @"C:\Program Files (x86)", "Program Files", maxDepth: 2);

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (Directory.Exists(Path.Combine(localAppData, "Programs")))
                {
                    ScanDirectory(detectedApps, Path.Combine(localAppData, "Programs"), "User Programs", maxDepth: 2);
                }

                ScanGameLaunchers(detectedApps);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while scanning for applications:\n\n{ex.Message}\n\n" +
                    "Some applications may not have been detected.",
                    "Scan Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var uniqueApps = detectedApps
                .GroupBy(app => app.Path.ToLower())
                .Select(group => group.First())
                .OrderBy(app => app.Name)
                .ToList();

            return uniqueApps;
        }

        private void ScanDirectory(List<DetectedApp> apps, string directory, string category, int currentDepth = 0, int maxDepth = 1)
        {
            if (!Directory.Exists(directory) || currentDepth > maxDepth)
                return;

            try
            {
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);

                foreach (var exePath in exeFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(exePath);

                        if (IsSystemOrInstallerFile(fileName))
                            continue;

                        apps.Add(new DetectedApp
                        {
                            Name = fileName,
                            Path = exePath,
                            Category = category
                        });
                    }
                    catch
                    {

                    }
                }

                if (currentDepth < maxDepth)
                {
                    var subdirectories = Directory.GetDirectories(directory);
                    foreach (var subdir in subdirectories)
                    {
                        ScanDirectory(apps, subdir, category, currentDepth + 1, maxDepth);
                    }
                }
            }
            catch
            {

            }
        }

        private void ScanGameLaunchers(List<DetectedApp> apps)
        {
            string steamPath = @"C:\Program Files (x86)\Steam\steam.exe";
            if (File.Exists(steamPath))
            {
                apps.Add(new DetectedApp { Name = "Steam", Path = steamPath, Category = "Game Launchers" });
            }

            string epicPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"EpicGamesLauncher\Portal\Binaries\Win32\EpicGamesLauncher.exe");
            if (File.Exists(epicPath))
            {
                apps.Add(new DetectedApp { Name = "Epic Games Launcher", Path = epicPath, Category = "Game Launchers" });
            }

            string gogPath = @"C:\Program Files (x86)\GOG Galaxy\GalaxyClient.exe";
            if (File.Exists(gogPath))
            {
                apps.Add(new DetectedApp { Name = "GOG Galaxy", Path = gogPath, Category = "Game Launchers" });
            }

            string battleNetPath = @"C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe";
            if (File.Exists(battleNetPath))
            {
                apps.Add(new DetectedApp { Name = "Battle.net", Path = battleNetPath, Category = "Game Launchers" });
            }
        }

        private bool IsSystemOrInstallerFile(string fileName)
        {
            var lowerName = fileName.ToLower();

            string[] skipKeywords = new[]
            {
                "unins", "uninstall", "setup", "install", "update", "updater",
                "crashreporter", "crashhandler", "helper", "service",
                "launcher", "bootstrapper", "installer"
            };

            return skipKeywords.Any(keyword => lowerName.Contains(keyword));
        }

        public int ImportDetectedApps(List<DetectedApp> detectedApps, ObservableCollection<Item> existingItems, Action<Item> addItemCallback)
        {
            int importedCount = 0;

            var existingPaths = new HashSet<string>(
                existingItems.Select(item => item.Path?.ToLower() ?? string.Empty),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var detectedApp in detectedApps.Where(app => app.IsSelected))
            {
                if (existingPaths.Contains(detectedApp.Path.ToLower()))
                    continue;

                var newItem = new Item
                {
                    Title = detectedApp.Name,
                    Path = detectedApp.Path,
                    UsageTime = TimeSpan.Zero
                };

                if (!string.IsNullOrEmpty(detectedApp.Category))
                {
                    newItem.AddCategory(detectedApp.Category);
                }

                var iconPath = _iconExtractionService.ExtractAndSaveIcon(detectedApp.Path, detectedApp.Name);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    newItem.IconUrl = iconPath;
                }

                addItemCallback(newItem);
                importedCount++;
            }

            return importedCount;
        }
    }
}
