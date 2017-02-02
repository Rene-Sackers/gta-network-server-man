using System.Windows.Controls;

namespace ServerMan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            textBox?.ScrollToEnd();
        }
    }
}
