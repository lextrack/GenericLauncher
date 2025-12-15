using GenericLauncher.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace GenericLauncher.Views
{
    public partial class AppDetectionWindow : Window
    {
        private ObservableCollection<AppDetectionService.DetectedApp> _detectedApps;

        public ObservableCollection<AppDetectionService.DetectedApp> DetectedApps
        {
            get => _detectedApps;
            set
            {
                _detectedApps = value;
                AppsListBox.ItemsSource = _detectedApps;

                foreach (var app in _detectedApps)
                {
                    app.PropertyChanged += App_PropertyChanged;
                }

                _detectedApps.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (AppDetectionService.DetectedApp app in e.NewItems)
                        {
                            app.PropertyChanged += App_PropertyChanged;
                        }
                    }
                    UpdateCount();
                };

                UpdateCount();
            }
        }

        public bool ImportConfirmed { get; private set; } = false;

        public AppDetectionWindow()
        {
            InitializeComponent();
            _detectedApps = new ObservableCollection<AppDetectionService.DetectedApp>();
        }

        private void App_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppDetectionService.DetectedApp.IsSelected))
            {
                UpdateCount();
            }
        }

        private void UpdateCount()
        {
            int selectedCount = _detectedApps?.Count(app => app.IsSelected) ?? 0;
            int totalCount = _detectedApps?.Count ?? 0;
            CountTextBlock.Text = $"{selectedCount} of {totalCount} selected";
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var app in _detectedApps)
            {
                app.IsSelected = true;
            }
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var app in _detectedApps)
            {
                app.IsSelected = false;
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
