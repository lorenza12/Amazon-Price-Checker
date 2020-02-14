using System.Windows;

namespace Amazon_Price_Checker.Windows
{
    /// <summary>
    /// Interaction logic for ConfirmationWindow.xaml
    /// </summary>
    public partial class ConfirmationWindow : Window
    {
        private bool confirmed;
        public ConfirmationWindow(string header, string message)
        {
            InitializeComponent();
            header_txt.Text = header;
            message_lbl.Text = message;
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            confirmed = true;
            this.DialogResult = true;
        }

        private void denyButton_Click(object sender, RoutedEventArgs e)
        {
            confirmed = false;
            this.DialogResult = true;
        }

        public bool Confirmed
        {
            get { return confirmed; }
        }
    }
}
