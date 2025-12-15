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
            if (item == null || string.IsNullOrEmpty(item.Path) || item.IsRunning)
                return false;

            if (item.Path.Contains("://"))
                return true;

            return File.Exists(item.Path);
        }

        public bool CanStopItem(Item item)
        {
            return item != null && item.IsRunning;
        }

        public async void LaunchItem(Item item)
        {
            if (item == null)
            {
                MessageBox.Show(
                    "No application selected. Please select an application from the list.",
                    "No Application Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(item.Path))
            {
                MessageBox.Show(
                    $"The application '{item.Title}' doesn't have a file path configured.\n\n" +
                    "Please click 'Select Path' in the Information tab to choose the executable file.",
                    "Path Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool isProtocolUrl = item.Path.Contains("://");

            if (!isProtocolUrl && !File.Exists(item.Path))
            {
                var result = MessageBox.Show(
                    $"The file for '{item.Title}' was not found at:\n{item.Path}\n\n" +
                    "The file may have been moved, renamed, or deleted.\n\n" +
                    "Would you like to update the path now?",
                    "File Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    return;
                }
                return;
            }

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

                ProcessStartInfo processStartInfo;

                if (isProtocolUrl)
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = item.Path,
                        UseShellExecute = true
                    };
                }
                else
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = item.Path,
                        Arguments = item.LaunchParameters ?? string.Empty,
                        WorkingDirectory = !string.IsNullOrEmpty(item.WorkingDirectory) && Directory.Exists(item.WorkingDirectory)
                                          ? item.WorkingDirectory
                                          : Path.GetDirectoryName(item.Path),
                        UseShellExecute = true
                    };
                }

                if (item.RunAsAdmin)
                {
                    processStartInfo.Verb = "runas";
                }

                Process? process = null;
                try
                {
                    process = Process.Start(processStartInfo);
                }
                catch (System.ComponentModel.Win32Exception win32Ex)
                {
                    if (item.RunAsAdmin && win32Ex.NativeErrorCode == 1223)
                    {
                        MessageBox.Show(
                            $"Administrator access was cancelled for '{item.Title}'.\n\n" +
                            "The application requires administrator privileges to run.\n" +
                            "If you don't want to run as admin, disable the 'Run as Administrator' option.",
                            "Administrator Access Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }
                    throw;
                }

                if (isProtocolUrl)
                {
                    item.IsRunning = true;
                    var protocolStopwatch = new Stopwatch();
                    protocolStopwatch.Start();

                    await MonitorProtocolLaunchedGame(item, protocolStopwatch);
                    return;
                }

                if (process == null)
                {
                    MessageBox.Show(
                        $"Failed to start '{item.Title}'.\n\n" +
                        "The application process could not be created. This might happen if:\n" +
                        "• The file is corrupted\n" +
                        "• Missing dependencies or runtime\n" +
                        "• Insufficient system resources",
                        "Launch Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (item.RunHighPriority)
                {
                    try
                    {
                        process.PriorityClass = ProcessPriorityClass.High;
                    }
                    catch
                    {

                    }
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
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"Access denied when trying to launch '{item.Title}'.\n\n" +
                    "You don't have permission to run this application.\n" +
                    "Try enabling 'Run as Administrator' in the Launch Options tab.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while launching '{item.Title}':\n\n" +
                    $"{ex.Message}\n\n" +
                    "Please verify the application path and launch settings are correct.",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task MonitorProtocolLaunchedGame(Item item, Stopwatch itemStopwatch)
        {
            await Task.Delay(3000);

            Process? gameProcess = null;

            for (int attempts = 0; attempts < 10; attempts++)
            {
                try
                {
                    var allProcesses = Process.GetProcesses();

                    gameProcess = allProcesses.FirstOrDefault(p =>
                    {
                        try
                        {
                            var processName = p.ProcessName.ToLower();
                            var gameTitle = item.Title.ToLower();
                            var titleWords = gameTitle.Split(new[] { ' ', '-', '_', ':', '.' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var word in titleWords)
                            {
                                if (word.Length >= 4 && processName.Contains(word))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    if (gameProcess != null && !gameProcess.HasExited)
                    {
                        break;
                    }
                }
                catch { }

                await Task.Delay(1000);
            }

            if (gameProcess == null)
            {
                await Task.Delay(4000);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.IsRunning = false;
                    item.UsageTime += itemStopwatch.Elapsed;
                    _saveItemsCallback();
                    OnPropertyChanged(nameof(RunningItems));
                });
                return;
            }

            _runningItems[item] = (gameProcess, itemStopwatch);

            await Task.Run(() =>
            {
                try
                {
                    gameProcess.WaitForExit();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_runningItems.TryGetValue(item, out var itemData))
                        {
                            itemData.Stopwatch.Stop();
                            item.UsageTime += itemData.Stopwatch.Elapsed;
                            item.IsRunning = false;

                            _runningItems.Remove(item);

                            _saveItemsCallback();

                            _launchActionsViewModel.ParentItem = item;
                            _launchActionsViewModel.ExecutePostExitActions();

                            OnPropertyChanged(nameof(RunningItems));
                        }
                    });
                }
                catch { }
            });
        }

        public void StopItem(Item item)
        {
            if (item == null)
                return;

            if (!_runningItems.TryGetValue(item, out var itemData))
            {
                MessageBox.Show(
                    $"'{item.Title}' is not currently running.",
                    "Not Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                if (!itemData.Process.HasExited)
                {
                    itemData.Process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(
                    $"'{item.Title}' has already been closed.",
                    "Already Closed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                    $"Could not stop '{item.Title}'.\n\n" +
                    "The application may require administrator privileges to terminate.\n" +
                    "Try closing it manually or restart Generic Launcher as administrator.",
                    "Cannot Stop Application",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while stopping '{item.Title}':\n\n{ex.Message}",
                    "Stop Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void LaunchOrStopItem(Item item)
        {
            if (item == null)
                return;

            if (item.IsRunning)
            {
                var result = MessageBox.Show(
                    $"'{item.Title}' is currently running.\n\n" +
                    "Stopping it will force the application to close immediately.\n" +
                    "Any unsaved work may be lost.\n\n" +
                    "Are you sure you want to stop it?",
                    "Confirm Stop Application",
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