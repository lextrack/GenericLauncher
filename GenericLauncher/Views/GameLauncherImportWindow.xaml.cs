using GenericLauncher.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace GenericLauncher.Views
{
    public partial class GameLauncherImportWindow : Window
    {
        private ObservableCollection<GameLauncherImportService.DetectedGame> _detectedGames;

        public ObservableCollection<GameLauncherImportService.DetectedGame> DetectedGames
        {
            get => _detectedGames;
            set
            {
                _detectedGames = value;
                GamesListBox.ItemsSource = _detectedGames;

                foreach (var game in _detectedGames)
                {
                    game.PropertyChanged += Game_PropertyChanged;
                }

                _detectedGames.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (GameLauncherImportService.DetectedGame game in e.NewItems)
                        {
                            game.PropertyChanged += Game_PropertyChanged;
                        }
                    }
                    UpdateCount();
                };

                UpdateCount();
            }
        }

        public List<GameLauncherImportService.LauncherInfo> LauncherStatus
        {
            set
            {
                LauncherStatusList.ItemsSource = value;
            }
        }

        public bool ImportConfirmed { get; private set; } = false;

        public GameLauncherImportWindow()
        {
            InitializeComponent();
            _detectedGames = new ObservableCollection<GameLauncherImportService.DetectedGame>();
        }

        private void Game_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameLauncherImportService.DetectedGame.IsSelected))
            {
                UpdateCount();
            }
        }

        private void UpdateCount()
        {
            int selectedCount = _detectedGames?.Count(game => game.IsSelected) ?? 0;
            int totalCount = _detectedGames?.Count ?? 0;
            CountTextBlock.Text = $"{selectedCount} of {totalCount} selected";
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var game in _detectedGames)
            {
                game.IsSelected = true;
            }
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var game in _detectedGames)
            {
                game.IsSelected = false;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            ImportConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ImportConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
