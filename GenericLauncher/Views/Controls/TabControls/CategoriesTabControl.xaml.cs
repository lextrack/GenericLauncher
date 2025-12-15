using System.Windows.Controls;

namespace GenericLauncher.Views.Controls.TabControls
{
    /// <summary>
    /// Interaction logic for CategoriesTabControl.xaml
    /// </summary>
    public partial class CategoriesTabControl : UserControl
    {
        public CategoriesTabControl()
        {
            InitializeComponent();

            if (this.FindName("AddCategoryButton") is Button addButton)
            {
                addButton.Click += (sender, e) =>
                {
                    if (this.FindName("NewCategoryTextBox") is TextBox textBox)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            textBox.Clear();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                };
            }
        }
    }
}

