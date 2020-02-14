using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Amazon_Price_Checker.NotificationTray.TaskIconCommands
{
    class ExitApplicationCommand : CommandBase<ExitApplicationCommand>
    {

        public override void Execute(object parameter)
        {
            MainWindow.IsExiting = true;
            GetTaskbarWindow(parameter).Close();
            System.Windows.Application.Current.Shutdown();
        }


        public override bool CanExecute(object parameter)
        {
            Window win = GetTaskbarWindow(parameter);
            return win != null;
        }
    }
}
