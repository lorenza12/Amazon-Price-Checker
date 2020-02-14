using System.Windows;
using System.Windows.Input;

namespace Amazon_Price_Checker.NotificationTray.TaskIconCommands
{
    /// <summary>
    /// Shows the main window.
    /// </summary>
    public class ShowWindowCommand : CommandBase<ShowWindowCommand>
    {
        public override void Execute(object parameter)
        {
            GetTaskbarWindow(parameter).Show();
            CommandManager.InvalidateRequerySuggested();

            //Make the window visible if it was previously hidden
            ((MainWindow)Application.Current.MainWindow).WindowState = WindowState.Normal;

        }


        public override bool CanExecute(object parameter)
        {
            Window win = GetTaskbarWindow(parameter);
            return win != null && !win.IsVisible;
        }
    }
}