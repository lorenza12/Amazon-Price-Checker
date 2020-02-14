using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Amazon_Price_Checker
{
    /// <summary>
    /// Interaction logic for NumericInputDialog.xaml
    /// </summary>
    public partial class NumericInputDialog : Window
    {
        public NumericInputDialog(string question, string defaultAnswer = "0")
        {
            InitializeComponent();
            question_lbl.Content = question;
            answer_txtbox.Text = defaultAnswer;

        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            answer_txtbox.SelectAll();
            answer_txtbox.Focus();
        }

        public string Answer
        {
            get { return answer_txtbox.Text; }
        }

        private void desiredTextInput_preview(object sender, TextCompositionEventArgs e)
        {

            // Here e.Text is string so we need to convert it into char
            char ch = e.Text[0];

            if ((Char.IsDigit(ch) || ch == '.'))
            {
                //Here TextBox1.Text is name of your TextBox
                if (ch == '.' && answer_txtbox.Text.Contains('.'))
                    e.Handled = true;
            }
            else
                e.Handled = true;
        }

        private void spaceCheck_preview(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Space)
            {
                e.Handled = true;
            }
    
        }
    }


}



