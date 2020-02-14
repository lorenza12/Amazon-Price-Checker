using System.Windows.Interactivity;
using System.Windows.Controls;
using System.Windows.Input;


namespace Amazon_Price_Checker.Behaviors
{
    public class TextBoxBindingUpdateOnEnterBehavior : Behavior<TextBox>
    {

        protected override void OnAttached()
        {
            AssociatedObject.KeyDown += OnTextBoxKeyDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.KeyDown -= OnTextBoxKeyDown;
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var txtBox = sender as TextBox;
                string url = txtBox.Text;

                txtBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();

                ////Check if user is watching the current url
                //if (db.IsWatchingUrl(CommonFunctions.StrippedAmazonUrl(((MainWindow)System.Windows.Application.Current.MainWindow).urlAddress_txtbox.Text)))
                //{
                //    ((MainWindow)System.Windows.Application.Current.MainWindow).priceWatch_btn.Background = Brushes.Red;
                //}
                //else
                //{
                //    ((MainWindow)System.Windows.Application.Current.MainWindow).priceWatch_btn.Background = Brushes.Transparent;
                //}

            }
        }

    }
}
