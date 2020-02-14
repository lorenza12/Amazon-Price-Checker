using System;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Amazon_Price_Checker.Common
{
    class DisplayLogItem
    {
        public BitmapImage LogImage { get; set; }
        public string LogText { get; set; }

        public enum LogIcon { Start, Error, Success, Warning, Info, Notification, Buy, Item };


        public DisplayLogItem(string logText, LogIcon logImage)
        {
            this.LogImage = GetLogIcon(logImage);
            this.LogText = logText;
        }

        public DisplayLogItem(string logText)
        {
            this.LogImage = null;
            this.LogText = logText;
        }

        public DisplayLogItem()
        {
            this.LogImage = null;
            this.LogText = string.Empty;
        }



        public static string CreateLogText(params string[] linesList)
        {
            StringBuilder logString = new StringBuilder();

            foreach (string line in linesList)
            {
                logString.AppendLine(line);
            }

            return logString.ToString().TrimEnd();
        }

        public void Log()
        {
            if (this.LogImage != null)
                LogImage.Freeze(); //Must freeze the image first before updating UI

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MainWindow.LogDataGrid.Items.Add(this);
                MainWindow.LogDataGrid.UpdateLayout();
                MainWindow.LogDataGrid.ScrollIntoView(this);
            }));

        }

        public static BitmapImage GetLogIcon(LogIcon image)
        {
            switch (image)
            {
                case LogIcon.Error:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Error.png"));

                case LogIcon.Start:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Start.png"));

                case LogIcon.Success:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Success.png"));

                case LogIcon.Warning:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Warning.png"));

                case LogIcon.Info:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Info.png"));

                case LogIcon.Buy:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Buy.png"));

                case LogIcon.Item:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Item.png"));

                case LogIcon.Notification:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Images/LogIcons/Notification.png"));
            }

            return null;
        }

    }
}
