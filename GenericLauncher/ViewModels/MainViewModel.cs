using GenericLauncher.Models;
using GenericLauncher.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GenericLauncher.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Item _selectedItem;
        private string _dataPath = "data.json";

        private ItemsManager _itemsManager;
        private CategoryManager _categoryManager;
        private LaunchManager _launchManager;
        private BackupManager _backupManager;
        private UtilityManager _utilityManager;
        private LaunchActionsViewModel _launchActionsViewModel;

        public MainViewModel()
        {
            _launchActionsViewModel = new LaunchActionsViewModel(this);

            _itemsManager = new ItemsManager(UpdateAllCategories);
            _categoryManager = new CategoryManager(() => _itemsManager.Items, SaveItems);
            _launchManager = new LaunchManager(SaveItems, _launchActionsViewModel);
            _utilityManager = new UtilityManager(SaveItems);
            _backupManager = new BackupManager(
                _dataPath,
                LoadItems,
                SaveItems,
                () => _itemsManager.Items,
                (items) => _itemsManager.SetItems(items),
                UpdateAllCategories
            );

            InitializeCommands();

            LoadItems();
            _utilityManager.MigrateExistingImages(_itemsManager.Items);

            _categoryManager.UpdateAllCategories();
            _itemsManager.SelectedCategory = "All categories";
            _itemsManager.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ItemsManager.SelectedCategory) ||
                    e.PropertyName == nameof(ItemsManager.IsCategoryFilterActive))
                {
                    OnPropertyChanged(nameof(SelectedCategory));
                    OnPropertyChanged(nameof(IsCategoryFilterActive));
                }
            };
        }

        private void SyncCategoryProperties()
        {
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(IsCategoryFilterActive));
            OnPropertyChanged(nameof(AllCategories));
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitializeCommands()
        {
            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, CanRemoveItem);

            LaunchItemCommand = new RelayCommand(LaunchItem, CanLaunchItem);
            StopItemCommand = new RelayCommand(StopItem, CanStopItem);
            LaunchOrStopItemCommand = new RelayCommand(LaunchOrStopItem, CanLaunchOrStopItem);

            SelectImageCommand = new RelayCommand(SelectImage, CanSelectImage);
            SelectItemPathCommand = new RelayCommand(SelectItemPath, CanSelectImage);
            SelectWorkingDirectoryCommand = new RelayCommand(SelectWorkingDirectory, () => SelectedItem != null);

            AddCategoryCommand = new RelayCommand(AddCategory, CanAddCategory);
            RemoveCategoryCommand = new RelayCommand(RemoveCategory, CanRemoveCategory);
            ClearCategoryFilterCommand = new RelayCommand(ClearCategoryFilter, () => _itemsManager.IsCategoryFilterActive);

            ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, () => SelectedItem != null);
            SaveNotesCommand = new RelayCommand(SaveNotes, () => SelectedItem != null);

            ExportLibraryCommand = new RelayCommand(_backupManager.ExportLibrary);
            ImportLibraryCommand = new RelayCommand(_backupManager.ImportLibrary);
            ShowAboutCommand = new RelayCommand(_utilityManager.ShowAbout);
            AutoDetectAppsCommand = new RelayCommand(AutoDetectApps);
            ImportFromLaunchersCommand = new RelayCommand(ImportFromLaunchers);
        }

        #region Properties

        public ObservableCollection<Item> Items => _itemsManager.Items;
        public ICollectionView ItemsView => _itemsManager.ItemsView;

        public string SearchTerm
        {
            get => _itemsManager.SearchTerm;
            set => _itemsManager.SearchTerm = value;
        }

        public bool ShowOnlyFavorites
        {
            get => _itemsManager.ShowOnlyFavorites;
            set => _itemsManager.ShowOnlyFavorites = value;
        }

        public string SelectedCategory
        {
            get => _itemsManager.SelectedCategory;
            set
            {
                if (_itemsManager.SelectedCategory != value)
                {
                    _itemsManager.SelectedCategory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCategoryFilterActive));
                }
            }
        }

        public bool IsCategoryFilterActive => _itemsManager.IsCategoryFilterActive;

        public ObservableCollection<string> AllCategories => _categoryManager.AllCategories;

        public string NewCategory
        {
            get => _categoryManager.NewCategory;
            set => _categoryManager.NewCategory = value;
        }

        public Item SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsItemSelected));
                OnPropertyChanged(nameof(SelectedItemCategories));

                _launchActionsViewModel.ParentItem = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsItemSelected => SelectedItem != null;
        public ObservableCollection<string> SelectedItemCategories => SelectedItem?.Categories;

        public LaunchActionsViewModel LaunchActionsViewModel => _launchActionsViewModel;

        #endregion

        #region Commands
        public ICommand AddCategoryWithResetCommand { get; private set; }
        public ICommand AddItemCommand { get; private set; }
        public ICommand RemoveItemCommand { get; private set; }
        public ICommand LaunchItemCommand { get; private set; }
        public ICommand SelectImageCommand { get; private set; }
        public ICommand SelectItemPathCommand { get; private set; }
        public ICommand AddCategoryCommand { get; private set; }
        public ICommand RemoveCategoryCommand { get; private set; }
        public ICommand ToggleFavoriteCommand { get; private set; }
        public ICommand ClearCategoryFilterCommand { get; private set; }
        public ICommand SaveNotesCommand { get; private set; }
        public ICommand SelectWorkingDirectoryCommand { get; private set; }
        public ICommand StopItemCommand { get; private set; }
        public ICommand ExportLibraryCommand { get; private set; }
        public ICommand ImportLibraryCommand { get; private set; }
        public ICommand LaunchOrStopItemCommand { get; private set; }
        public ICommand ShowAboutCommand { get; private set; }
        public ICommand AutoDetectAppsCommand { get; private set; }
        public ICommand ImportFromLaunchersCommand { get; private set; }
        #endregion

        #region Command Methods

        private void AddItem()
        {
            var item = _itemsManager.CreateNewItem();
            SelectedItem = item;
        }

        private bool CanRemoveItem()
        {
            return SelectedItem != null && !SelectedItem.IsRunning;
        }

        private void RemoveItem()
        {
            if (SelectedItem != null)
            {
                string usageTimeText;
                if (SelectedItem.UsageTime.TotalHours >= 1)
                {
                    usageTimeText = $"{SelectedItem.UsageTime.TotalHours:F1} hours";
                }
                else if (SelectedItem.UsageTime.TotalMinutes >= 1)
                {
                    usageTimeText = $"{SelectedItem.UsageTime.TotalMinutes:F0} minutes";
                }
                else if (SelectedItem.UsageTime.TotalSeconds >= 1)
                {
                    usageTimeText = $"{SelectedItem.UsageTime.TotalSeconds:F0} seconds";
                }
                else
                {
                    usageTimeText = "0 time";
                }

                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete '{SelectedItem.Title}'?\n\n" +
                    "This will remove:\n" +
                    $"• All statistics ({SelectedItem.LaunchCount} launches, {usageTimeText})\n" +
                    "• Notes and categories\n" +
                    "• Launch settings\n\n" +
                    "This action cannot be undone.",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _itemsManager.RemoveItem(SelectedItem);
                    SelectedItem = null;
                }
            }
        }

        private bool CanLaunchItem()
        {
            return _launchManager.CanLaunchItem(SelectedItem);
        }

        private void LaunchItem()
        {
            _launchManager.LaunchItem(SelectedItem);
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanStopItem()
        {
            return _launchManager.CanStopItem(SelectedItem);
        }

        private void StopItem()
        {
            _launchManager.StopItem(SelectedItem);
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanLaunchOrStopItem()
        {
            if (SelectedItem == null)
                return false;

            if (SelectedItem.IsRunning)
                return true;

            return _launchManager.CanLaunchItem(SelectedItem);
        }

        private void LaunchOrStopItem()
        {
            _launchManager.LaunchOrStopItem(SelectedItem);
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanSelectImage()
        {
            return SelectedItem != null;
        }

        private void SelectImage()
        {
            _utilityManager.SelectImage(SelectedItem);
        }

        private void SelectItemPath()
        {
            _utilityManager.SelectItemPath(SelectedItem);
        }

        private void SelectWorkingDirectory()
        {
            _utilityManager.SelectWorkingDirectory(SelectedItem);
        }

        private void SaveNotes()
        {
            _utilityManager.SaveNotes(SelectedItem);
        }

        private bool CanAddCategory()
        {
            return _categoryManager.CanAddCategory(SelectedItem);
        }

        private void AddCategory()
        {
            SyncCategoryProperties();
            _categoryManager.AddCategory(SelectedItem);
            OnPropertyChanged(nameof(SelectedItemCategories));
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanRemoveCategory()
        {
            SyncCategoryProperties();
            return _categoryManager.CanRemoveCategory(SelectedItem, SelectedCategory);
        }

        private void RemoveCategory()
        {
            _categoryManager.RemoveCategory(SelectedItem, SelectedCategory);
            OnPropertyChanged(nameof(SelectedItemCategories));
            CommandManager.InvalidateRequerySuggested();
        }

        private void ClearCategoryFilter()
        {
            _itemsManager.ClearCategoryFilter();
            SyncCategoryProperties();
        }

        private void ToggleFavorite()
        {
            _itemsManager.ToggleFavorite(SelectedItem);
        }

        private async void AutoDetectApps()
        {
            var detectionService = new AppDetectionService();

            System.Windows.MessageBox.Show(
                "Scanning your computer for applications...\n\n" +
                "This may take a moment. Click OK to start.",
                "Scanning",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            var detectedApps = await System.Threading.Tasks.Task.Run(() => detectionService.ScanForApplications());

            if (detectedApps.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "No new applications were found.\n\n" +
                    "All detectable applications may already be in your library.",
                    "Scan Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            var detectionWindow = new Views.AppDetectionWindow();
            detectionWindow.Owner = System.Windows.Application.Current.MainWindow;
            detectionWindow.DetectedApps = new ObservableCollection<AppDetectionService.DetectedApp>(detectedApps);

            if (detectionWindow.ShowDialog() == true && detectionWindow.ImportConfirmed)
            {
                int imported = detectionService.ImportDetectedApps(
                    detectionWindow.DetectedApps.ToList(),
                    Items,
                    (item) => _itemsManager.AddItem(item));

                UpdateAllCategories();

                if (imported > 0)
                {
                    System.Windows.MessageBox.Show(
                        $"Successfully imported {imported} application(s) to your library!",
                        "Import Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "No applications were imported.\n" +
                        "They may already exist in your library.",
                        "Import Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private async void ImportFromLaunchers()
        {
            var importService = new GameLauncherImportService();

            System.Windows.MessageBox.Show(
                "Detecting installed game launchers...\n\n" +
                "This will scan for Steam, Epic Games, and GOG Galaxy.\n" +
                "Click OK to start.",
                "Scanning Launchers",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            var launcherInfo = await System.Threading.Tasks.Task.Run(() => importService.DetectInstalledLaunchers());
            var detectedGames = await System.Threading.Tasks.Task.Run(() => importService.DetectAllGames());
            var existingPaths = new HashSet<string>(
                Items.Select(item => item.Path?.ToLower() ?? string.Empty),
                StringComparer.OrdinalIgnoreCase
            );
            var newGames = detectedGames.Where(game => !existingPaths.Contains(game.Path.ToLower())).ToList();

            if (newGames.Count == 0)
            {
                var installedLaunchers = launcherInfo.Where(l => l.IsInstalled).Select(l => l.Name).ToList();
                var message = installedLaunchers.Count > 0
                    ? $"No new games were found.\n\n" +
                      $"Detected launchers: {string.Join(", ", installedLaunchers)}\n\n" +
                      "All games from these launchers may already be in your library."
                    : "No game launchers were detected on your system.\n\n" +
                      "Make sure you have Steam, Epic Games, or GOG Galaxy installed.";

                System.Windows.MessageBox.Show(
                    message,
                    "Scan Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            var importWindow = new Views.GameLauncherImportWindow();
            importWindow.Owner = System.Windows.Application.Current.MainWindow;
            importWindow.LauncherStatus = launcherInfo;
            importWindow.DetectedGames = new ObservableCollection<GameLauncherImportService.DetectedGame>(newGames);

            if (importWindow.ShowDialog() == true && importWindow.ImportConfirmed)
            {
                var selectedGames = importWindow.DetectedGames.Where(g => g.IsSelected).ToList();
                int imported = 0;

                foreach (var game in selectedGames)
                {
                    var newItem = new Item
                    {
                        Title = game.Name,
                        Path = game.Path,
                        UsageTime = TimeSpan.Zero
                    };

                    newItem.AddCategory(game.Launcher);

                    if (!game.Path.StartsWith("steam://") && System.IO.File.Exists(game.Path))
                    {
                        _utilityManager.ExtractIconForItem(newItem);
                    }

                    _itemsManager.AddItem(newItem);
                    imported++;
                }

                UpdateAllCategories();

                if (imported > 0)
                {
                    System.Windows.MessageBox.Show(
                        $"Successfully imported {imported} game(s) to your library!\n\n" +
                        "Games have been categorized by their launcher.",
                        "Import Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Public Methods

        public void SaveItems()
        {
            _itemsManager.SaveItems();
        }

        public void LoadItems()
        {
            _itemsManager.LoadItems();
            SyncCategoryProperties();
        }

        public void UpdateAllCategories()
        {
            var currentSelected = SelectedCategory;

            _categoryManager.UpdateAllCategories();

            if (!string.IsNullOrEmpty(currentSelected) && _categoryManager.AllCategories.Contains(currentSelected))
            {
                _itemsManager.SelectedCategory = currentSelected;
            }
            else
            {
                _itemsManager.SelectedCategory = "All categories";
            }

            SyncCategoryProperties();
        }

        public bool IsItemRunning(Item item)
        {
            return _launchManager.IsItemRunning(item);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}