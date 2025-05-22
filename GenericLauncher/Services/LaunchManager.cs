using GenericLauncher.Models;
using GenericLauncher.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GenericLauncher.Services
{
    public class LaunchManager : INotifyPropertyChanged
    {
        private Dictionary<Item, (Process Process, Stopwatch Stopwatch)> _runningItems = new Dictionary<Item, (Process, Stopwatch)>();
        private Action _saveItemsCallback;
        private readonly LaunchActionsViewModel _launchActionsViewModel;

        public LaunchManager(Action saveItemsCallback, LaunchActionsViewModel launchActionsViewModel)
        {
            _saveItemsCallback = saveItemsCallback;
            _launchActionsViewModel = launchActionsViewModel;
        }

        public bool IsItemRunning(Item item)
        {
            return _runningItems.ContainsKey(item);
        }

        public IEnumerable<Item> RunningItems => _runningItems.Keys;

        public bool CanLaunchItem(Item item)
        {
            return item != null &&
                   !string.IsNullOrEmpty(item.Path) &&
                   File.Exists(item.Path) &&
                   !item.IsRunning;
        }

        public bool CanStopItem(Item item)
        {
            return item != null && item.IsRunning;
        }

        public async void LaunchItem(Item item)
        {
            if (item == null || string.IsNullOrEmpty(item.Path) || !File.Exists(item.Path))
                return;

            try
            {
                item.LaunchCount++;
                _launchActionsViewModel.ParentItem = item;
                _launchActionsViewModel.ExecutePreLaunchActions();

                if (!item.FirstUsed.HasValue)
                {
                    item.FirstUsed = DateTime.Now;
                }

                item.LastUsed = DateTime.Now;

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = item.Path,
                    Arguments = item.LaunchParameters ?? string.Empty,
                    WorkingDirectory = !string.IsNullOrEmpty(item.WorkingDirectory) && Directory.Exists(item.WorkingDirectory)
                                      ? item.WorkingDirectory
                                      : Path.GetDirectoryName(item.Path),
                    UseShellExecute = true
                };

                if (item.RunAsAdmin)
                {
                    processStartInfo.Verb = "runas";
                }

                var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    MessageBox.Show("The application could not be started", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (item.RunHighPriority)
                {
                    try
                    {
                        process.PriorityClass = ProcessPriorityClass.High;
                    }
                    catch { }
                }

                var itemStopwatch = new Stopwatch();
                itemStopwatch.Start();

                item.IsRunning = true;

                _runningItems[item] = (process, itemStopwatch);

                if (item.CloseLauncherOnStart)
                {
                    Application.Current.MainWindow.Close();
                    return;
                }

                var itemToMonitor = item;
                await Task.Run(() =>
                {
                    try
                    {
                        Process itemProcess = process;
                        itemProcess.WaitForExit();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_runningItems.TryGetValue(itemToMonitor, out var itemData))
                            {
                                itemData.Stopwatch.Stop();
                                itemToMonitor.UsageTime += TimeSpan.FromMilliseconds(itemData.Stopwatch.ElapsedMilliseconds);
                                _runningItems.Remove(itemToMonitor);
                                itemToMonitor.IsRunning = false;
                                _saveItemsCallback();

                                _launchActionsViewModel.ParentItem = itemToMonitor;
                                _launchActionsViewModel.ExecutePostExitActions();
                            }
                        });
                    }
                    catch (Exception) { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting the application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopItem(Item item)
        {
            if (item != null && _runningItems.TryGetValue(item, out var itemData))
            {
                try
                {
                    if (!itemData.Process.HasExited)
                    {
                        itemData.Process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error stopping the application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LaunchOrStopItem(Item item)
        {
            if (item == null)
                return;

            if (item.IsRunning)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to stop {item.Title}? This could cause loss of unsaved progress.",
                    "Stop",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    StopItem(item);
                }
            }
            else
            {
                LaunchItem(item);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}