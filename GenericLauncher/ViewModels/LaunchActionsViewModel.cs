using GenericLauncher.Models;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GenericLauncher.ViewModels
{
    public class LaunchActionsViewModel : INotifyPropertyChanged
    {
        private Item _parentItem;
        private MainViewModel _mainViewModel;

        public LaunchActionsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            SelectPreLaunchScriptCommand = new RelayCommand(SelectPreLaunchScript, () => ParentItem != null);
            SelectPostExitScriptCommand = new RelayCommand(SelectPostExitScript, () => ParentItem != null);
            SelectAppsToCloseCommand = new RelayCommand(SelectAppsToClose, () => ParentItem != null);
            TestPreLaunchCommand = new RelayCommand(TestPreLaunchActions, CanTestPreLaunchActions);
            TestPostExitCommand = new RelayCommand(TestPostExitActions, CanTestPostExitActions);
        }

        public Item ParentItem
        {
            get => _parentItem;
            set
            {
                _parentItem = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #region Commands

        public ICommand SelectPreLaunchScriptCommand { get; private set; }
        public ICommand SelectPostExitScriptCommand { get; private set; }
        public ICommand SelectAppsToCloseCommand { get; private set; }
        public ICommand TestPreLaunchCommand { get; private set; }
        public ICommand TestPostExitCommand { get; private set; }

        private void SelectPreLaunchScript()
        {
            if (ParentItem == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Script files|*.bat;*.cmd;*.ps1;*.vbs;*.js|All files|*.*",
                Title = "Select pre-launch script"
            };

            if (dialog.ShowDialog() == true)
            {
                ParentItem.LaunchActions.PreLaunchScriptPath = dialog.FileName;
                _mainViewModel.SaveItems();
            }
        }

        private void SelectPostExitScript()
        {
            if (ParentItem == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Script files|*.bat;*.cmd;*.ps1;*.vbs;*.js|All files|*.*",
                Title = "Select post-exit script"
            };

            if (dialog.ShowDialog() == true)
            {
                ParentItem.LaunchActions.PostExitScriptPath = dialog.FileName;
                _mainViewModel.SaveItems();
            }
        }

        private void SelectAppsToClose()
        {
            if (ParentItem == null) return;

            try
            {
                Process[] runningProcesses = Process.GetProcesses();
                var processNames = runningProcesses
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                    .Select(p => p.ProcessName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                string processListStr = string.Join(", ", processNames);

                MessageBox.Show($"Running processes: {processListStr}\n\n" +
                               "Copy the names you want to close and separate them with commas in the text field.",
                               "Running processes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting the process list: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanTestPreLaunchActions()
        {
            return ParentItem != null &&
                   (ParentItem.LaunchActions.CloseOtherApps ||
                    (ParentItem.LaunchActions.RunPreLaunchScript &&
                     File.Exists(ParentItem.LaunchActions.PreLaunchScriptPath)));
        }

        private void TestPreLaunchActions()
        {
            if (ParentItem == null) return;

            try
            {
                if (ParentItem.LaunchActions.CloseOtherApps && !string.IsNullOrWhiteSpace(ParentItem.LaunchActions.AppsToClose))
                {
                    CloseSpecifiedApplications(ParentItem.LaunchActions.AppsToClose);
                }

                if (ParentItem.LaunchActions.RunPreLaunchScript &&
                    File.Exists(ParentItem.LaunchActions.PreLaunchScriptPath))
                {
                    RunScript(ParentItem.LaunchActions.PreLaunchScriptPath);
                }

                MessageBox.Show("Pre-launch actions executed successfully.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing pre-launch actions: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanTestPostExitActions()
        {
            return ParentItem != null &&
                   ParentItem.LaunchActions.RunPostExitScript &&
                   File.Exists(ParentItem.LaunchActions.PostExitScriptPath);
        }

        private void TestPostExitActions()
        {
            if (ParentItem == null) return;

            try
            {
                if (ParentItem.LaunchActions.RunPostExitScript &&
                    File.Exists(ParentItem.LaunchActions.PostExitScriptPath))
                {
                    RunScript(ParentItem.LaunchActions.PostExitScriptPath);
                }

                MessageBox.Show("Post-exit actions executed successfully.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing post-exit actions: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        public void ExecutePreLaunchActions()
        {
            if (ParentItem == null) return;

            try
            {
                if (ParentItem.LaunchActions.CloseOtherApps &&
                    !string.IsNullOrWhiteSpace(ParentItem.LaunchActions.AppsToClose))
                {
                    CloseSpecifiedApplications(ParentItem.LaunchActions.AppsToClose);
                }

                if (ParentItem.LaunchActions.RunPreLaunchScript &&
                    File.Exists(ParentItem.LaunchActions.PreLaunchScriptPath))
                {
                    RunScript(ParentItem.LaunchActions.PreLaunchScriptPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing pre-launch actions: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExecutePostExitActions()
        {
            if (ParentItem == null) return;

            try
            {
                if (ParentItem.LaunchActions.RunPostExitScript &&
                    File.Exists(ParentItem.LaunchActions.PostExitScriptPath))
                {
                    RunScript(ParentItem.LaunchActions.PostExitScriptPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing post-exit actions: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseSpecifiedApplications(string appList)
        {
            if (string.IsNullOrWhiteSpace(appList)) return;

            string[] appsToClose = appList.Split(',', ';')
                                         .Select(a => a.Trim())
                                         .Where(a => !string.IsNullOrEmpty(a))
                                         .ToArray();

            foreach (string appName in appsToClose)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(appName);
                    foreach (Process process in processes)
                    {
                        if (!process.HasExited)
                        {
                            process.CloseMainWindow();
                            if (!process.WaitForExit(2000))
                            {
                                process.Kill();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cerrar {appName}: {ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RunScript(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath)) return;

            string extension = Path.GetExtension(scriptPath).ToLower();
            Process process = new Process();

            switch (extension)
            {
                case ".bat":
                case ".cmd":
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/c \"{scriptPath}\"";
                    break;
                case ".ps1":
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
                    break;
                case ".vbs":
                    process.StartInfo.FileName = "wscript.exe";
                    process.StartInfo.Arguments = $"\"{scriptPath}\"";
                    break;
                case ".js":
                    process.StartInfo.FileName = "wscript.exe";
                    process.StartInfo.Arguments = $"\"{scriptPath}\"";
                    break;
                default:
                    process.StartInfo.FileName = scriptPath;
                    break;
            }

            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}