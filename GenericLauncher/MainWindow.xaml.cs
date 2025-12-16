using GenericLauncher.Models;
using GenericLauncher.ViewModels;
using System.IO;
using System.Windows;

namespace GenericLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Drop += MainWindow_Drop;
            this.DragOver += MainWindow_DragOver;
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                bool hasExeFile = false;
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        hasExeFile = true;
                        break;
                    }
                }

                e.Effects = hasExeFile ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (DataContext is MainViewModel viewModel)
                {
                    int importedCount = 0;

                    foreach (string file in files)
                    {
                        if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                            File.Exists(file))
                        {
                            var newItem = new Item
                            {
                                Title = Path.GetFileNameWithoutExtension(file),
                                Path = file,
                                UsageTime = TimeSpan.Zero
                            };

                            var utilityManager = new Services.UtilityManager(viewModel.SaveItems);
                            utilityManager.ExtractIconForItem(newItem);

                            viewModel.Items.Add(newItem);
                            importedCount++;
                        }
                    }

                    if (importedCount > 0)
                    {
                        viewModel.SaveItems();
                        viewModel.UpdateAllCategories();
                    }
                }
            }
        }
    }
}