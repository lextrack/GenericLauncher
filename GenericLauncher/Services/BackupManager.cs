using GenericLauncher.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace GenericLauncher.Services
{
    public class BackupManager
    {
        private readonly string _dataPath;
        private readonly Action _loadItemsCallback;
        private readonly Action _saveItemsCallback;
        private readonly Func<ObservableCollection<Item>> _getItemsFunc;
        private readonly Action<ObservableCollection<Item>> _setItemsAction;
        private readonly Action _updateAllCategoriesCallback;

        public BackupManager(
            string dataPath,
            Action loadItemsCallback,
            Action saveItemsCallback,
            Func<ObservableCollection<Item>> getItemsFunc,
            Action<ObservableCollection<Item>> setItemsAction,
            Action updateAllCategoriesCallback)
        {
            _dataPath = dataPath;
            _loadItemsCallback = loadItemsCallback;
            _saveItemsCallback = saveItemsCallback;
            _getItemsFunc = getItemsFunc;
            _setItemsAction = setItemsAction;
            _updateAllCategoriesCallback = updateAllCategoriesCallback;
        }

        public void ExportLibrary()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Export Game Library",
                    FileName = "game_library_backup.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _saveItemsCallback();

                    File.Copy(_dataPath, saveDialog.FileName, true);

                    MessageBox.Show(
                        "Library exported successfully.",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error exporting library: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void ImportLibrary()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Import Game Library"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "Do you want to replace your current library or merge with the imported one?\n\n" +
                        "• Yes = Replace current library\n" +
                        "• No = Merge and keep existing items\n" +
                        "• Cancel = Abort import",
                        "Import Options",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    if (result == MessageBoxResult.Yes)
                    {
                        File.Copy(openDialog.FileName, _dataPath, true);
                        _loadItemsCallback();

                        MessageBox.Show(
                            "Library replaced successfully.",
                            "Import Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        MergeLibraries(openDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error importing library: {ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void MergeLibraries(string importFilePath)
        {
            try
            {
                string json = File.ReadAllText(importFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var importedItems = JsonSerializer.Deserialize<ObservableCollection<Item>>(json, options);

                if (importedItems == null || importedItems.Count == 0)
                {
                    MessageBox.Show(
                        "The imported file doesn't contain any valid items.",
                        "Import Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var currentItems = _getItemsFunc();
                int newItemsCount = 0;
                int updatedItemsCount = 0;

                var items = new ObservableCollection<Item>(currentItems);

                foreach (var importedItem in importedItems)
                {
                    var existingItem = items.FirstOrDefault(i =>
                        !string.IsNullOrEmpty(i.Path) &&
                        !string.IsNullOrEmpty(importedItem.Path) &&
                        i.Path.Equals(importedItem.Path, StringComparison.OrdinalIgnoreCase));

                    if (existingItem != null)
                    {
                        if (!existingItem.Title.Equals(importedItem.Title, StringComparison.OrdinalIgnoreCase))
                        {
                            var updateResult = MessageBox.Show(
                                $"Item with path '{importedItem.Path}' already exists but has a different name:\n\n" +
                                $"Existing: {existingItem.Title}\n" +
                                $"Imported: {importedItem.Title}\n\n" +
                                "Do you want to update the existing item with the imported data?",
                                "Conflict Detected",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (updateResult == MessageBoxResult.Yes)
                            {
                                UpdateExistingItem(existingItem, importedItem);
                                updatedItemsCount++;
                            }
                        }
                    }
                    else
                    {
                        items.Add(importedItem);
                        newItemsCount++;
                    }
                }

                _setItemsAction(items);

                _updateAllCategoriesCallback();
                _saveItemsCallback();

                MessageBox.Show(
                    $"Library merged successfully!\n\n" +
                    $"• Added {newItemsCount} new items\n" +
                    $"• Updated {updatedItemsCount} existing items",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error merging libraries: {ex.Message}",
                    "Merge Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateExistingItem(Item existingItem, Item importedItem)
        {
            existingItem.Title = importedItem.Title;

            if (importedItem.UsageTime > existingItem.UsageTime)
                existingItem.UsageTime = importedItem.UsageTime;

            if (importedItem.LastUsed > existingItem.LastUsed)
                existingItem.LastUsed = importedItem.LastUsed;

            if (importedItem.FirstUsed.HasValue &&
                (!existingItem.FirstUsed.HasValue || importedItem.FirstUsed < existingItem.FirstUsed))
                existingItem.FirstUsed = importedItem.FirstUsed;

            existingItem.LaunchCount += importedItem.LaunchCount;

            existingItem.IsFavorite = existingItem.IsFavorite || importedItem.IsFavorite;

            if (!string.IsNullOrWhiteSpace(importedItem.Notes))
            {
                if (string.IsNullOrWhiteSpace(existingItem.Notes))
                    existingItem.Notes = importedItem.Notes;
                else
                    existingItem.Notes += "\n\n--- Imported Notes ---\n" + importedItem.Notes;
            }

            foreach (var category in importedItem.Categories)
            {
                if (!existingItem.Categories.Contains(category))
                    existingItem.Categories.Add(category);
            }

            existingItem.LaunchParameters = importedItem.LaunchParameters;
            existingItem.WorkingDirectory = importedItem.WorkingDirectory;
            existingItem.RunAsAdmin = importedItem.RunAsAdmin;
            existingItem.RunHighPriority = importedItem.RunHighPriority;
            existingItem.CloseLauncherOnStart = importedItem.CloseLauncherOnStart;

            existingItem.LaunchActions.CloseOtherApps = importedItem.LaunchActions.CloseOtherApps;
            existingItem.LaunchActions.AppsToClose = importedItem.LaunchActions.AppsToClose;
            existingItem.LaunchActions.RunPreLaunchScript = importedItem.LaunchActions.RunPreLaunchScript;
            existingItem.LaunchActions.PreLaunchScriptPath = importedItem.LaunchActions.PreLaunchScriptPath;
            existingItem.LaunchActions.RunPostExitScript = importedItem.LaunchActions.RunPostExitScript;
            existingItem.LaunchActions.PostExitScriptPath = importedItem.LaunchActions.PostExitScriptPath;
        }
    }
}