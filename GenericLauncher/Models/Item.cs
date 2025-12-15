using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace GenericLauncher.Models
{
    public class Item : INotifyPropertyChanged
    {
        private string _title;
        private string _path;
        private string _imageUrl;
        private string _iconUrl;
        private TimeSpan _usageTime;
        private DateTime? _lastUsed;
        private BitmapImage _image;
        private BitmapImage _icon;
        private ObservableCollection<string> _categories;
        private bool _isFavorite;
        private string _notes;
        private DateTime? _firstUsed;
        private int _launchCount;
        private string _launchParameters;
        private string _workingDirectory;
        private bool _runAsAdmin;
        private bool _closeLauncherOnStart;
        private bool _isRunning;
        private LaunchActions _launchActions;

        public Item()
        {
            _categories = new ObservableCollection<string>();
            _launchCount = 0;
            _runAsAdmin = false;
            _runHighPriority = false;
            _closeLauncherOnStart = false;
            _launchActions = new LaunchActions();
        }

        public LaunchActions LaunchActions
        {
            get
            {
                if (_launchActions == null)
                {
                    _launchActions = new LaunchActions();
                }
                return _launchActions;
            }
            set
            {
                _launchActions = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public TimeSpan AverageSessionTime
        {
            get
            {
                if (LaunchCount <= 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromTicks(UsageTime.Ticks / LaunchCount);
            }
        }

        [JsonIgnore]
        public int DaysOwned
        {
            get
            {
                if (!FirstUsed.HasValue)
                    return 0;

                return (DateTime.Now - FirstUsed.Value).Days + 1;
            }
        }

        [JsonIgnore]
        public string UsageFrequencyText
        {
            get
            {
                if (!FirstUsed.HasValue || LaunchCount <= 0)
                    return "Never used";

                int daysOwned = DaysOwned;
                if (daysOwned <= 0)
                    return "First day";

                double launchesPerDay = (double)LaunchCount / daysOwned;

                if (launchesPerDay >= 1.0)
                    return $"{launchesPerDay:F1} times per day";
                else if (launchesPerDay >= 0.143)
                    return $"{launchesPerDay * 7:F1} times per week";
                else if (launchesPerDay >= 0.033)
                    return $"{launchesPerDay * 30:F1} times per month";
                else
                    return "Rarely used";
            }
        }

        [JsonIgnore]
        public double UsagePercentage
        {
            get
            {
                if (!FirstUsed.HasValue)
                    return 0;

                TimeSpan totalTimeOwned = DateTime.Now - FirstUsed.Value;
                if (totalTimeOwned.TotalHours <= 0)
                    return 0;

                return (UsageTime.TotalHours / totalTimeOwned.TotalHours) * 100;
            }
        }

        public string Notes
        {
            get => _notes ?? string.Empty;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        public DateTime? FirstUsed
        {
            get => _firstUsed;
            set
            {
                _firstUsed = value;
                OnPropertyChanged();
            }
        }

        public int LaunchCount
        {
            get => _launchCount;
            set
            {
                _launchCount = value;
                OnPropertyChanged();
            }
        }

        public string LaunchParameters
        {
            get => _launchParameters ?? string.Empty;
            set
            {
                _launchParameters = value;
                OnPropertyChanged();
            }
        }

        public string WorkingDirectory
        {
            get => _workingDirectory ?? string.Empty;
            set
            {
                _workingDirectory = value;
                OnPropertyChanged();
            }
        }

        public bool RunAsAdmin
        {
            get => _runAsAdmin;
            set
            {
                _runAsAdmin = value;
                OnPropertyChanged();
            }
        }

        private bool _runHighPriority;

        public bool RunHighPriority
        {
            get => _runHighPriority;
            set
            {
                _runHighPriority = value;
                OnPropertyChanged();
            }
        }

        public bool CloseLauncherOnStart
        {
            get => _closeLauncherOnStart;
            set
            {
                _closeLauncherOnStart = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPathValid));
                OnPropertyChanged(nameof(PathValidationMessage));
            }
        }

        [JsonIgnore]
        public bool IsPathValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_path))
                    return false;

                if (_path.Contains("://"))
                    return true;

                if (!File.Exists(_path))
                    return false;

                return System.IO.Path.GetExtension(_path).Equals(".exe", StringComparison.OrdinalIgnoreCase);
            }
        }

        [JsonIgnore]
        public string PathValidationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_path))
                    return "Path is empty";

                if (_path.Contains("://"))
                    return "Protocol URL (valid)";

                if (!File.Exists(_path))
                    return "File not found";

                if (!System.IO.Path.GetExtension(_path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    return "Not an executable file (.exe)";

                return "Path is valid";
            }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                OnPropertyChanged();

                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        LoadImage(value);
                    }
                    catch
                    {

                    }
                }
            }
        }

        public string IconUrl
        {
            get => _iconUrl;
            set
            {
                _iconUrl = value;
                OnPropertyChanged();

                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        LoadIcon(value);
                    }
                    catch
                    {

                    }
                }
            }
        }

        public TimeSpan UsageTime
        {
            get => _usageTime;
            set
            {
                _usageTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastUsed
        {
            get => _lastUsed;
            set
            {
                _lastUsed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastUsedText));
            }
        }

        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string LastUsedText
        {
            get
            {
                if (!LastUsed.HasValue)
                    return "Never";

                var diff = DateTime.Now - LastUsed.Value;

                if (diff.TotalDays < 1)
                    return "Today";
                else if (diff.TotalDays < 2)
                    return "Yesterday";
                else if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays} days ago";
                else if (diff.TotalDays < 30)
                    return $"{(int)(diff.TotalDays / 7)} weeks ago";
                else if (diff.TotalDays < 365)
                    return $"{(int)(diff.TotalDays / 30)} months ago";
                else
                    return $"{(int)(diff.TotalDays / 365)} years ago";
            }
        }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string CategoriesText => Categories.Count > 0
            ? string.Join(", ", Categories)
            : "No categories";

        [JsonIgnore]
        public BitmapImage Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasImage));
            }
        }

        [JsonIgnore]
        public bool HasImage => Image != null;

        [JsonIgnore]
        public BitmapImage Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasIcon));
            }
        }

        [JsonIgnore]
        public bool HasIcon => Icon != null;

        private void LoadImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !File.Exists(imageUrl))
                return;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imageUrl, UriKind.RelativeOrAbsolute);
            image.EndInit();
            image.Freeze();

            Image = image;
        }

        public void LoadImageFromUrl()
        {
            if (!string.IsNullOrEmpty(ImageUrl))
            {
                try
                {
                    LoadImage(ImageUrl);
                }
                catch
                {

                }
            }
        }

        private void LoadIcon(string iconUrl)
        {
            if (string.IsNullOrEmpty(iconUrl) || !File.Exists(iconUrl))
                return;

            var icon = new BitmapImage();
            icon.BeginInit();
            icon.CacheOption = BitmapCacheOption.OnLoad;
            icon.UriSource = new Uri(iconUrl, UriKind.RelativeOrAbsolute);
            icon.DecodePixelWidth = 24;
            icon.EndInit();
            icon.Freeze();

            Icon = icon;
        }

        public void LoadIconFromUrl()
        {
            if (!string.IsNullOrEmpty(IconUrl))
            {
                try
                {
                    LoadIcon(IconUrl);
                }
                catch
                {

                }
            }
        }

        public void AddCategory(string category)
        {
            if (!string.IsNullOrWhiteSpace(category) && !Categories.Contains(category))
            {
                Categories.Add(category);
                OnPropertyChanged(nameof(CategoriesText));
            }
        }

        public void RemoveCategory(string category)
        {
            if (Categories.Contains(category))
            {
                Categories.Remove(category);
                OnPropertyChanged(nameof(CategoriesText));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}