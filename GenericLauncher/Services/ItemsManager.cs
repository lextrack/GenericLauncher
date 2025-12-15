using GenericLauncher.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;

namespace GenericLauncher.Services
{
    public class ItemsManager : INotifyPropertyChanged
    {
        private string _dataPath = "data.json";
        private ObservableCollection<Item> _items;
        private ICollectionView _itemsView;
        private string _searchTerm;
        private bool _showOnlyFavorites;
        private Action _updateCategoriesCallback;

        public ItemsManager(Action updateCategoriesCallback)
        {
            _updateCategoriesCallback = updateCategoriesCallback;
            Items = new ObservableCollection<Item>();
            _itemsView = CollectionViewSource.GetDefaultView(Items);
            _itemsView.Filter = FilterItems;
        }

        public ObservableCollection<Item> Items
        {
            get => _items;
            private set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView ItemsView => _itemsView;

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                if (_itemsView != null)
                {
                    _itemsView.Refresh();
                }
            }
        }

        public bool ShowOnlyFavorites
        {
            get => _showOnlyFavorites;
            set
            {
                _showOnlyFavorites = value;
                OnPropertyChanged();
                if (_itemsView != null)
                {
                    _itemsView.Refresh();
                }
            }
        }

        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    value = "All categories";
                }

                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    if (_itemsView != null)
                    {
                        _itemsView.Refresh();
                    }
                    OnPropertyChanged(nameof(IsCategoryFilterActive));
                }
            }
        }

        public bool IsCategoryFilterActive => !string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All categories";

        public void ClearCategoryFilter()
        {
            SelectedCategory = "All categories";
        }

        private bool FilterItems(object item)
        {
            if (item is not Item appItem)
                return false;

            if (ShowOnlyFavorites && !appItem.IsFavorite)
                return false;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                bool matchesSearch = appItem.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);

                if (!matchesSearch)
                {
                    matchesSearch = appItem.Categories.Any(c => c.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                                   (appItem.Notes != null && appItem.Notes.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                                   (appItem.Path != null && appItem.Path.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (!matchesSearch)
                    return false;
            }

            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All categories")
            {
                return appItem.Categories.Contains(SelectedCategory);
            }

            return true;
        }

        public void AddItem(Item item)
        {
            Items.Add(item);
            SaveItems();
        }

        public void RemoveItem(Item item)
        {
            Items.Remove(item);
            _updateCategoriesCallback();
            SaveItems();
        }

        public void ToggleFavorite(Item item)
        {
            if (item != null)
            {
                item.IsFavorite = !item.IsFavorite;
                _itemsView?.Refresh();
                SaveItems();
            }
        }

        public Item CreateNewItem()
        {
            var item = new Item { Title = "New item", UsageTime = TimeSpan.Zero };
            AddItem(item);
            return item;
        }

        public void LoadItems()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    string json = File.ReadAllText(_dataPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var loadedItems = JsonSerializer.Deserialize<ObservableCollection<Item>>(json, options);

                    if (loadedItems != null)
                    {
                        Items.Clear();
                        foreach (var item in loadedItems)
                        {
                            if (!string.IsNullOrEmpty(item.ImageUrl) &&
                                !Path.IsPathRooted(item.ImageUrl) &&
                                item.ImageUrl.StartsWith("Resources"))
                            {
                                item.ImageUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, item.ImageUrl);
                            }

                            if (!string.IsNullOrEmpty(item.IconUrl) &&
                                !Path.IsPathRooted(item.IconUrl) &&
                                item.IconUrl.StartsWith("Resources"))
                            {
                                item.IconUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, item.IconUrl);
                            }

                            if (item.LaunchActions == null)
                            {
                                item.LaunchActions = new LaunchActions();
                            }

                            item.LoadImageFromUrl();
                            item.LoadIconFromUrl();
                            Items.Add(item);
                        }

                        _updateCategoriesCallback();
                    }
                }
            }
            catch (JsonException)
            {
                MessageBox.Show(
                    "The library file is corrupted or in an invalid format.\n\n" +
                    "Your data file may have been modified incorrectly.\n" +
                    "If you have a backup, you can restore it using 'Import Library'.",
                    "Library Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Access denied when trying to read the library file.\n\n" +
                    "Generic Launcher doesn't have permission to access the data file.\n" +
                    "Try running as administrator or check file permissions.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while loading your library:\n\n{ex.Message}\n\n" +
                    "Your library data may not have loaded correctly.",
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void SaveItems()
        {
            try
            {
                var itemsToSave = new ObservableCollection<Item>();

                foreach (var item in Items)
                {
                    var itemCopy = new Item
                    {
                        Title = item.Title,
                        Path = item.Path,
                        UsageTime = item.UsageTime,
                        LastUsed = item.LastUsed,
                        FirstUsed = item.FirstUsed,
                        LaunchCount = item.LaunchCount,
                        IsFavorite = item.IsFavorite,
                        LaunchParameters = item.LaunchParameters,
                        WorkingDirectory = item.WorkingDirectory,
                        RunAsAdmin = item.RunAsAdmin,
                        RunHighPriority = item.RunHighPriority,
                        CloseLauncherOnStart = item.CloseLauncherOnStart,
                        Notes = item.Notes,
                        LaunchActions = new LaunchActions
                        {
                            CloseOtherApps = item.LaunchActions.CloseOtherApps,
                            AppsToClose = item.LaunchActions.AppsToClose,
                            RunPreLaunchScript = item.LaunchActions.RunPreLaunchScript,
                            PreLaunchScriptPath = item.LaunchActions.PreLaunchScriptPath,
                            RunPostExitScript = item.LaunchActions.RunPostExitScript,
                            PostExitScriptPath = item.LaunchActions.PostExitScriptPath
                        }
                    };

                    foreach (var category in item.Categories)
                    {
                        itemCopy.Categories.Add(category);
                    }

                    if (!string.IsNullOrEmpty(item.ImageUrl) &&
                        Path.IsPathRooted(item.ImageUrl) &&
                        item.ImageUrl.Contains(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources")))
                    {
                        itemCopy.ImageUrl = item.ImageUrl.Replace(
                            AppDomain.CurrentDomain.BaseDirectory, "").TrimStart('\\', '/');
                    }
                    else
                    {
                        itemCopy.ImageUrl = item.ImageUrl;
                    }

                    if (!string.IsNullOrEmpty(item.IconUrl) &&
                        Path.IsPathRooted(item.IconUrl) &&
                        item.IconUrl.Contains(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources")))
                    {
                        itemCopy.IconUrl = item.IconUrl.Replace(
                            AppDomain.CurrentDomain.BaseDirectory, "").TrimStart('\\', '/');
                    }
                    else
                    {
                        itemCopy.IconUrl = item.IconUrl;
                    }

                    itemsToSave.Add(itemCopy);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(itemsToSave, options);
                File.WriteAllText(_dataPath, json);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Cannot save library: Access denied.\n\n" +
                    "Generic Launcher doesn't have permission to write to the data file.\n" +
                    "Your changes may not be saved.",
                    "Save Error - Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show(
                    $"Cannot save library: File I/O error.\n\n" +
                    $"{ioEx.Message}\n\n" +
                    "This may happen if the disk is full or the file is locked by another program.",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while saving your library:\n\n{ex.Message}\n\n" +
                    "Your changes may not have been saved.",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void SetItems(ObservableCollection<Item> items)
        {
            if (items != null)
            {
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}