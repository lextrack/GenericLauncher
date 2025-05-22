using GenericLauncher.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace GenericLauncher.Services
{
    public class UtilityManager
    {
        private readonly Action _saveItemsCallback;

        public UtilityManager(Action saveItemsCallback)
        {
            _saveItemsCallback = saveItemsCallback;
        }

        public void SelectImage(Item selectedItem)
        {
            if (selectedItem == null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*",
                Title = "Select image for the application"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string resourcesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                    if (!Directory.Exists(resourcesDir))
                    {
                        Directory.CreateDirectory(resourcesDir);
                    }

                    string fileName = Path.GetFileName(dialog.FileName);
                    string extension = Path.GetExtension(fileName);
                    string baseName = Path.GetFileNameWithoutExtension(fileName);
                    string uniqueName = $"{CleanFileName(selectedItem.Title)}_{baseName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{extension}";
                    string destinationPath = Path.Combine(resourcesDir, uniqueName);

                    File.Copy(dialog.FileName, destinationPath, true);

                    selectedItem.ImageUrl = destinationPath;
                    _saveItemsCallback();

                    MessageBox.Show("Image has been saved in the Resources folder.", "Image Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SelectItemPath(Item selectedItem)
        {
            if (selectedItem == null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Executables|*.exe|All files|*.*",
                Title = "Select application executable"
            };

            if (dialog.ShowDialog() == true)
            {
                selectedItem.Path = dialog.FileName;

                if (string.IsNullOrEmpty(selectedItem.Title) || selectedItem.Title == "New item")
                {
                    selectedItem.Title = Path.GetFileNameWithoutExtension(dialog.FileName);
                }

                _saveItemsCallback();
            }
        }

        public void SelectWorkingDirectory(Item selectedItem)
        {
            if (selectedItem == null)
                return;

            var dialog = new OpenFileDialog
            {
                Title = "Select working directory",
                CheckFileExists = false,
                FileName = "Folder Selection",
                ValidateNames = false,
                CheckPathExists = true,
                CustomPlaces = new System.Collections.ObjectModel.ObservableCollection<FileDialogCustomPlace>()
            };

            if (!string.IsNullOrEmpty(selectedItem.WorkingDirectory) && Directory.Exists(selectedItem.WorkingDirectory))
            {
                dialog.InitialDirectory = selectedItem.WorkingDirectory;
            }
            else if (!string.IsNullOrEmpty(selectedItem.Path) && File.Exists(selectedItem.Path))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(selectedItem.Path);
            }

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = Path.GetDirectoryName(dialog.FileName);
                selectedItem.WorkingDirectory = selectedPath;
                _saveItemsCallback();
            }
        }

        public void SaveNotes(Item selectedItem)
        {
            if (selectedItem != null)
            {
                _saveItemsCallback();
                MessageBox.Show("Notes saved successfully", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ShowAbout()
        {
            MessageBox.Show(
                "Generic Launcher\n" +
                "Version 1.0.0\n\n" +
                "A simple launcher to organize games and applications\n" +
                "Thank you for using this application!\n\n" +
                "Initial author: Lextrack",
                "About Generic Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private string CleanFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            if (fileName.Length > 30)
            {
                fileName = fileName.Substring(0, 30);
            }

            return fileName;
        }

        public void MigrateExistingImages(System.Collections.ObjectModel.ObservableCollection<Item> items)
        {
            string resourcesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.ImageUrl) && File.Exists(item.ImageUrl) &&
                    !item.ImageUrl.Contains(resourcesDir))
                {
                    try
                    {
                        string fileName = Path.GetFileName(item.ImageUrl);
                        string extension = Path.GetExtension(fileName);
                        string baseName = Path.GetFileNameWithoutExtension(fileName);
                        string uniqueName = $"{CleanFileName(item.Title)}_{baseName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{extension}";
                        string destinationPath = Path.Combine(resourcesDir, uniqueName);

                        File.Copy(item.ImageUrl, destinationPath, true);

                        item.ImageUrl = destinationPath;
                    }
                    catch
                    {
                    }
                }
            }

            _saveItemsCallback();
        }
    }
}