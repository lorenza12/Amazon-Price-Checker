using System.Windows;
using System.Windows.Input;

namespace Amazon_Price_Checker.NotificationTray.TaskIconCommands
{
    /// <summary>
    /// Hides the main window.
    /// </summary>
    public class HideWindowCommand : CommandBase<HideWindowCommand>
    {
        public override void Execute(object parameter)
        {
            GetTaskbarWindow(parameter).Hide();
            CommandManager.InvalidateRequerySuggested();
        }


        public override bool CanExecute(object parameter)
        {
            Window win = GetTaskbarWindow(parameter);
            return win != null && win.IsVisible;
        }
    }
}