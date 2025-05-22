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
            // Create instances of services in correct order (to avoid circular dependencies)
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
                _itemsManager.RemoveItem(SelectedItem);
                SelectedItem = null;
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
            _categoryManager.UpdateAllCategories();
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