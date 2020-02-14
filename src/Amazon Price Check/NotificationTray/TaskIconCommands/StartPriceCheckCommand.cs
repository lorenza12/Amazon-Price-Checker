using System;
using System.Windows;

namespace Amazon_Price_Checker.NotificationTray.TaskIconCommands
{
    public class StartPriceCheckCommand : CommandBase<StartPriceCheckCommand>
    {

        public override void Execute(object parameter)
        {

            ((MainWindow)Application.Current.MainWindow).StartPriceCheck();
        }

        public override bool CanExecute(object parameter)
        {
            Window win = GetTaskbarWindow(parameter);
            return win != null && !PriceChecker.CheckingPrices;
        }
    }
}
