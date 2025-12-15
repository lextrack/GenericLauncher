using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GenericLauncher.Models
{
    public class LaunchActions : INotifyPropertyChanged
    {
        private bool _closeOtherApps;
        private string _appsToClose;
        private bool _runPreLaunchScript;
        private string _preLaunchScriptPath;
        private bool _runPostExitScript;
        private string _postExitScriptPath;

        public bool CloseOtherApps
        {
            get => _closeOtherApps;
            set
            {
                _closeOtherApps = value;
                OnPropertyChanged();
            }
        }

        public string AppsToClose
        {
            get => _appsToClose ?? string.Empty;
            set
            {
                _appsToClose = value;
                OnPropertyChanged();
            }
        }

        public bool RunPreLaunchScript
        {
            get => _runPreLaunchScript;
            set
            {
                _runPreLaunchScript = value;
                OnPropertyChanged();
            }
        }

        public string PreLaunchScriptPath
        {
            get => _preLaunchScriptPath ?? string.Empty;
            set
            {
                _preLaunchScriptPath = value;
                OnPropertyChanged();
            }
        }

        public bool RunPostExitScript
        {
            get => _runPostExitScript;
            set
            {
                _runPostExitScript = value;
                OnPropertyChanged();
            }
        }

        public string PostExitScriptPath
        {
            get => _postExitScriptPath ?? string.Empty;
            set
            {
                _postExitScriptPath = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}