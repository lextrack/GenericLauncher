using GenericLauncher.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GenericLauncher.Services
{
    public class CategoryManager : INotifyPropertyChanged
    {
        private ObservableCollection<string> _allCategories;
        private string _newCategory;
        private Func<ObservableCollection<Item>> _getItemsFunc;
        private Action _saveItemsCallback;

        public CategoryManager(Func<ObservableCollection<Item>> getItemsFunc, Action saveItemsCallback)
        {
            _getItemsFunc = getItemsFunc;
            _saveItemsCallback = saveItemsCallback;
            _allCategories = new ObservableCollection<string>();
            _allCategories.Add("All categories");
        }

        public ObservableCollection<string> AllCategories
        {
            get => _allCategories;
            private set
            {
                _allCategories = value;
                OnPropertyChanged();
            }
        }

        public string NewCategory
        {
            get => _newCategory;
            set
            {
                if (_newCategory != value)
                {
                    _newCategory = value;
                    OnPropertyChanged();
                }
            }
        }

        public void UpdateAllCategories()
        {
            var categories = new HashSet<string>();
            var items = _getItemsFunc();

            foreach (var item in items)
            {
                foreach (var category in item.Categories)
                {
                    categories.Add(category);
                }
            }

            string currentSelected = null;
            if (AllCategories.Count > 0)
            {
                currentSelected = AllCategories.FirstOrDefault(c => c == "All categories") ?? AllCategories[0];
            }

            AllCategories.Clear();
            AllCategories.Add("All categories");

            foreach (var category in categories.OrderBy(c => c))
            {
                AllCategories.Add(category);
            }

            OnPropertyChanged(nameof(AllCategories));
        }

        public bool CanAddCategory(Item selectedItem)
        {
            return selectedItem != null &&
                   !string.IsNullOrWhiteSpace(NewCategory) &&
                   !selectedItem.Categories.Contains(NewCategory);
        }

        public void AddCategory(Item selectedItem)
        {
            if (CanAddCategory(selectedItem))
            {
                selectedItem.AddCategory(NewCategory);

                if (!AllCategories.Contains(NewCategory))
                {
                    AllCategories.Add(NewCategory);
                }

                _newCategory = string.Empty;

                OnPropertyChanged(nameof(NewCategory));

                _saveItemsCallback();
            }
        }

        public bool CanRemoveCategory(Item selectedItem, string categoryToRemove)
        {
            return selectedItem != null &&
                   !string.IsNullOrEmpty(categoryToRemove) &&
                   categoryToRemove != "All categories" &&
                   selectedItem.Categories.Contains(categoryToRemove);
        }

        public void RemoveCategory(Item selectedItem, string categoryToRemove)
        {
            if (CanRemoveCategory(selectedItem, categoryToRemove))
            {
                selectedItem.RemoveCategory(categoryToRemove);
                UpdateAllCategories();
                _saveItemsCallback();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}