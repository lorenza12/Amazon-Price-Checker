using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Amazon_Price_Checker.NotificationTray
{
    /// <summary>
    /// Interaction logic for TaskBarNotification.xaml
    /// </summary>
    public partial class TaskBarNotification : UserControl
    {
        private bool isClosing = false;

        #region BalloonText dependency property


        public static readonly DependencyProperty BalloonTextProperty =
            DependencyProperty.Register(nameof(BalloonText),
                typeof(string),
                typeof(TaskBarNotification),
                new FrameworkPropertyMetadata(string.Empty));

        public string BalloonText
        {
            get { return (string)GetValue(BalloonTextProperty); }
            set { SetValue(BalloonTextProperty, value); }
        }

        #endregion

        public TaskBarNotification()
        {
            InitializeComponent();
            TaskbarIcon.AddBalloonClosingHandler(this, OnBalloonClosing);
        }


        private void OnBalloonClosing(object sender, RoutedEventArgs e)
        {
            e.Handled = true; //suppresses the popup from being closed immediately
            isClosing = true;
        }


        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        private void grid_MouseEnter(object sender, MouseEventArgs e)
        {
            //if we're already running the fade-out animation, do not interrupt anymore
            //(makes things too complicated for the sample)
            if (isClosing) return;

            //the tray icon assigned this attached property to simplify access
            //TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            //taskbarIcon.ResetBalloonCloseTimer();
        }


        private void OnFadeOutCompleted(object sender, EventArgs e)
        {
            Popup pp = (Popup)Parent;
            pp.IsOpen = false;
        }
    }
}