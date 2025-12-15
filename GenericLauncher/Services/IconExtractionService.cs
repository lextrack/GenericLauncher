using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace GenericLauncher.Services
{
    public class IconExtractionService
    {
        private readonly string _iconsDirectory;

        public IconExtractionService()
        {
            _iconsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons");

            if (!Directory.Exists(_iconsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_iconsDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not create Icons directory:\n\n{ex.Message}",
                        "Directory Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        public string? ExtractAndSaveIcon(string exePath, string appTitle)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return null;

            try
            {
                using (var extractedIcon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (extractedIcon == null)
                        return null;

                    string safeTitle = CleanFileName(appTitle);
                    string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
                    string iconFileName = $"{safeTitle}_icon_{uniqueId}.png";
                    string iconPath = Path.Combine(_iconsDirectory, iconFileName);

                    using (var bitmap = extractedIcon.ToBitmap())
                    {
                        bitmap.Save(iconPath, ImageFormat.Png);
                    }

                    return iconPath;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Access denied when trying to extract icon.\n\n" +
                    "Generic Launcher doesn't have permission to save the icon file.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }
            catch (IOException ioEx)
            {
                MessageBox.Show(
                    $"Could not save icon file:\n\n{ioEx.Message}",
                    "File Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unexpected error extracting icon:\n\n{ex.Message}",
                    "Icon Extraction Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }
        }

        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "app";

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

        public void DeleteIcon(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
                return;

            try
            {
                if (iconPath.Contains(Path.Combine("Resources", "Icons")))
                {
                    File.Delete(iconPath);
                }
            }
            catch
            {

            }
        }
    }
}
