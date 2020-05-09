using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Amazon_Price_Checker.Common;
using Amazon_Price_Checker.Handlers;
using Amazon_Price_Checker.Notifications;
using Amazon_Price_Checker.NotificationTray;
using Amazon_Price_Checker.Windows;
using CefSharp;
using Hardcodet.Wpf.TaskbarNotification;

namespace Amazon_Price_Checker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Used when the user selects the option to auto reset tabs back to previous size when coming from the browser
        private double previousTabWidth;
        private double previousTabHeight;

        private static ScheduledPriceCheck scheduler = new ScheduledPriceCheck();

        public static DataGrid LogDataGrid;

        //Used to differentiate from an actual exit or minimize
        public static bool IsExiting { get; set; } = false;


        public MainWindow()
        {
            InitializeComponent();

            FillToolTips();

            FillWatchList();

            LogDataGrid = runtimeLog_dataGrid;


            Browser.RequestHandler = new BrowserRequestHandler();
            Browser.LoadingStateChanged += Browser_LoadingStateChanged;


            UpdateSettingsOnLoad();

            if (CommonFunctions.UserSettings.SchedulerEnabled)
                StartScheduler();

            SetWindowState();

        }

        #region Home


        private void StartPriceCheck_click(object sender, RoutedEventArgs e)
        {
            StartPriceCheck();
        }

        public void StartPriceCheck()
        {
            this.Dispatcher.Invoke(() =>
            {
                startPriceCheck_btn.IsEnabled = false;
                startPriceCheck_btn.Content = "Checking Prices...";
            });

            PriceChecker.CheckPrices(GetWatchItems());
        }

        public void RefreshLastPriceCheck()
        {
            this.Dispatcher.Invoke(() =>
            {
                lastExecutedRuntime_txt.Text = CommonFunctions.UserSettings.LastPriceCheck.ToString();
            });

        }

        #endregion


        #region Settings

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                string logFile = "AmazonPriceChecker.log";
                var logDir = Path.Combine(baseDir, logFile);
                CommonFunctions.Log.Debug($"Opening log file:= {logDir}");

                Process.Start(logDir);
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error opening log file location", ex);
            }
        }

        private void StartScheduler()
        {
            try
            {
                //Convert slider time to milliseconds
                int intervalMilliseconds = 0;

                switch (CommonFunctions.UserSettings.SchedulerOption)
                {
                    case (Settings.ScheduleType.Hours):
                        intervalMilliseconds = CommonFunctions.HoursToMilliseconds(CommonFunctions.UserSettings.ScheduleTime);
                        break;

                    case (Settings.ScheduleType.Days):
                        intervalMilliseconds = CommonFunctions.DaysToMilliseconds(CommonFunctions.UserSettings.ScheduleTime);
                        break;
                }

                //Set scheduler execution time to the conversion
                scheduler.MillisecondsUntilExecution = intervalMilliseconds;

                //Start the scheduler
                scheduler.StartSchedule();

                //Update the next scheduled runtime settings
                CommonFunctions.UserSettings.SetNextScheduledPriceCheck(GetSchedulerNextRunTime());

            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Error starting the scheduler", e);
            }

        }

        private void UpdatedScheduler(bool schedulerEnabledChanged, bool scheduleTimeChanged, bool scheduleOptionChanged)
        {
            try
            {
                if (schedulerEnabledChanged)
                {
                    if (CommonFunctions.UserSettings.SchedulerEnabled)
                        StartScheduler();
                    else
                    {
                        scheduler.CancelSchedule();
                    }
                }

                //If the scheduler enabled has changed, everything will be taken care of so only need 1 of the other 2 options to have changed
                else if (scheduleTimeChanged || scheduleOptionChanged && !schedulerEnabledChanged)
                {
                    //Convert slider time to milliseconds
                    int intervalMilliseconds = 0;

                    switch (CommonFunctions.UserSettings.SchedulerOption)
                    {
                        case (Settings.ScheduleType.Hours):
                            intervalMilliseconds = CommonFunctions.HoursToMilliseconds(CommonFunctions.UserSettings.ScheduleTime);
                            break;

                        case (Settings.ScheduleType.Days):
                            intervalMilliseconds = CommonFunctions.DaysToMilliseconds(CommonFunctions.UserSettings.ScheduleTime);
                            break;
                    }

                    scheduler.MillisecondsUntilExecution = intervalMilliseconds;
                    scheduler.UpdateScheduleTime(intervalMilliseconds);

                    //Update the next scheduled runtime settings
                    CommonFunctions.UserSettings.SetNextScheduledPriceCheck(GetSchedulerNextRunTime());
                }
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Error updating the scheduler", e);
            }
        }



        private void UpdateScheduleSliderTicks()
        {
            switch (scheduleTypeSelect_cmbobox.SelectedIndex)
            {
                //hours
                case 0:
                    schedule_sldr.Minimum = 1;
                    schedule_sldr.Maximum = 24;
                    break;

                //days - Timer.Interval has a maximum time of 24.8 days so must limit to 20 days
                case 1:
                    schedule_sldr.Minimum = 1;
                    schedule_sldr.Maximum = 20;
                    break;
            }
        }

        private void ScheduleTimeSelect_dropDownClosed(object sender, EventArgs e)
        {
            UpdateScheduleSliderTicks();
        }



        private void UpdateSettingsOnLoad()
        {
            try
            {
                CommonFunctions.Log.Debug($"Loading Settings");
                CommonFunctions.UserSettings.LogSettings();

                lastExecutedRuntime_txt.Text = CommonFunctions.UserSettings.LastPriceCheck.Year == 1 ? "" : CommonFunctions.UserSettings.LastPriceCheck.ToString();

                startOnStartup_chbx.IsChecked = CommonFunctions.UserSettings.StartOnStartup;
                startInTray_chbx.IsChecked = CommonFunctions.UserSettings.StartInTray;
                minimizeToTray_chbx.IsChecked = CommonFunctions.UserSettings.MinimizeToTray;
                closeMinimizes_chbx.IsChecked = CommonFunctions.UserSettings.MinimizeOnClose;
                priceCheckOnLaunch_chbx.IsChecked = CommonFunctions.UserSettings.PriceCheckOnLaunch;
                limitNotifications_chbx.IsChecked = CommonFunctions.UserSettings.LimitNotifications;

                autoResizeBrowser_chbx.IsChecked = CommonFunctions.UserSettings.ResizeBrowser;
                browserFullScreenResize_rdbtn.IsChecked = CommonFunctions.UserSettings.ResizeBrowserFullScreen;
                browserFitWindowResize_rdbtn.IsChecked = CommonFunctions.UserSettings.ResizeBrowserToFit;
                resizeTabsBackToPrevious_rdbtn.IsChecked = CommonFunctions.UserSettings.ResizeTabsToPrevious;
                resizeTabsBackToDefault_rdbtn.IsChecked = CommonFunctions.UserSettings.ResizeTabsToDefault;
                noResizeTabs_rdbtn.IsChecked = CommonFunctions.UserSettings.NoResizingTabs;
                logLevel_cmbobox.SelectedItem = logLevel_cmbobox.Items.Cast<ComboBoxItem>().Where(e => e.Content.ToString() == CommonFunctions.UserSettings.LogLevel).FirstOrDefault();
                colorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(CommonFunctions.UserSettings.AccentColor);

                schedulerEnabled_chbx.IsChecked = CommonFunctions.UserSettings.SchedulerEnabled;
                schedule_sldr.Value = CommonFunctions.UserSettings.ScheduleTime;
                scheduleTypeSelect_cmbobox.SelectedIndex = CommonFunctions.UserSettings.SchedulerOption == Settings.ScheduleType.Hours ? 0 : 1;
                schedule_txt.Text = CommonFunctions.UserSettings.ScheduleTime.ToString();

                UpdateScheduleSliderTicks(); //Update the ticks corresponding to if it was days or hours selected

                notifications_chbx.IsChecked = CommonFunctions.UserSettings.ReceiveNotifications;
                email_chbx.IsChecked = CommonFunctions.UserSettings.EmailNotifications;
                email_txtbx.Text = CommonFunctions.UserSettings.EmailAddress;
                textMessages_chbx.IsChecked = CommonFunctions.UserSettings.TextNotifications;
                phoneNumber_txtbx.Text = CommonFunctions.UserSettings.PhoneNumber;
                carrier_cmbobox.SelectedItem = carrier_cmbobox.Items.Cast<ComboBoxItem>().Where(e => e.Tag.ToString() == CommonFunctions.UserSettings.CarrierAddress).FirstOrDefault();
                popupNotification_chbx.IsChecked = CommonFunctions.UserSettings.PopupNotifications;
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error Loading Settings", ex);
            }
        }


        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private async void SaveSettings()
        {
            bool valid = VerifyValidSettings();

            if (valid)
            {
                this.Dispatcher.Invoke(() => saveSettings_btn.IsEnabled = false);

                //check to see if the scheduler was updated.
                bool schedulerEnabledChanged = CommonFunctions.UserSettings.SchedulerEnabled != schedulerEnabled_chbx.IsChecked;
                bool scheduleTimeChanged = CommonFunctions.UserSettings.ScheduleTime != Convert.ToInt32(schedule_sldr.Value);
                bool scheduleOptionChanged = CommonFunctions.UserSettings.SchedulerOption.ToString() != ((ComboBoxItem)scheduleTypeSelect_cmbobox.SelectedItem).Content.ToString();

                bool settingsSaved = CommonFunctions.UserSettings.SaveSettings(startOnStartup_chbx.IsChecked ?? false,
                                                              startInTray_chbx.IsChecked ?? false,
                                                              minimizeToTray_chbx.IsChecked ?? false,
                                                              closeMinimizes_chbx.IsChecked ?? false,
                                                              priceCheckOnLaunch_chbx.IsChecked ?? false,
                                                              limitNotifications_chbx.IsChecked ?? false,
                                                              autoResizeBrowser_chbx.IsChecked ?? false,
                                                              browserFullScreenResize_rdbtn.IsChecked ?? false,
                                                              browserFitWindowResize_rdbtn.IsChecked ?? false,
                                                              resizeTabsBackToPrevious_rdbtn.IsChecked ?? false,
                                                              resizeTabsBackToDefault_rdbtn.IsChecked ?? false,
                                                              noResizeTabs_rdbtn.IsChecked ?? false,
                                                              ((ComboBoxItem)logLevel_cmbobox.SelectedItem).Content.ToString(),
                                                              colorPicker.SelectedColor.ToString(),
                                                              schedulerEnabled_chbx.IsChecked ?? false,
                                                              Convert.ToInt32(schedule_sldr.Value),
                                                              ((ComboBoxItem)scheduleTypeSelect_cmbobox.SelectedItem).Content.ToString(),
                                                              notifications_chbx.IsChecked ?? false,
                                                              email_chbx.IsChecked ?? false,
                                                              email_txtbx.Text.Trim(),
                                                              textMessages_chbx.IsChecked ?? false,
                                                              phoneNumber_txtbx.Text.Trim(),
                                                              ((ComboBoxItem)carrier_cmbobox.SelectedItem).Tag.ToString(),
                                                              popupNotification_chbx.IsChecked ?? false
                                                              );

                saveSettings_btn.Content = "Saved";
                await Task.Delay(1000);
                this.Dispatcher.Invoke(() =>
                {
                    saveSettings_btn.IsEnabled = true;
                    saveSettings_btn.Content = "Save";
                });

                if (schedulerEnabledChanged || scheduleTimeChanged || scheduleOptionChanged)
                    UpdatedScheduler(schedulerEnabledChanged, scheduleTimeChanged, scheduleOptionChanged);


                if (!settingsSaved)
                {
                    CommonFunctions.Log.Warn("Unable to save settings");
                }

            }

        }

        private bool VerifyValidSettings()
        {
            bool valid = true;
            try
            {
                bool emailEnabled = email_chbx.IsChecked ?? false;
                bool textEnabled = textMessages_chbx.IsChecked ?? false;

                bool isEmailAddressEntered = !string.IsNullOrWhiteSpace(email_txtbx.Text);

                //Email enabled but there is no email address inputted
                if (emailEnabled && !isEmailAddressEntered)
                {
                    valid = false;

                    StringBuilder message = new StringBuilder();
                    message.AppendLine("Email notifications are enabled but no email address has been entered.\n");
                    message.AppendLine("Please enter your email address in the email address textbox.");

                    InfoWindow emailInfo = new InfoWindow("Email Address Error", message.ToString());
                    if (emailInfo.ShowDialog() == true)
                    {
                        //close window
                    }

                }

                bool isPhoneNumberEntered = !string.IsNullOrWhiteSpace(phoneNumber_txtbx.Text);
                bool isCarrierSelectedEntered = carrier_cmbobox.SelectedIndex != 0;

                //Text messages enabled but there is no phone number and/or carrier selected
                if ((textEnabled && !isPhoneNumberEntered) || textEnabled && !isCarrierSelectedEntered)
                {
                    if (!isPhoneNumberEntered && isCarrierSelectedEntered)
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Text notifications are enabled but no phone number has been entered.\n");
                        message.AppendLine("Please enter your phone number in the phone number textbox.");

                        InfoWindow textInfo = new InfoWindow("Text Message Phone Number Error", message.ToString());
                        if (textInfo.ShowDialog() == true)
                        {
                            //close window
                        }

                    }
                    else if (isPhoneNumberEntered && !isCarrierSelectedEntered)
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Text notifications are enabled but no carrier has been selected.\n");
                        message.AppendLine("Please select your carrier from the carrier drop down.");

                        InfoWindow textInfo = new InfoWindow("Text Message Carrier Error", message.ToString());
                        if (textInfo.ShowDialog() == true)
                        {
                            //close window
                        }

                    }
                    else //both are left blank
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Text notifications are enabled but no phone number has been entered and the carrier is left blank.\n");
                        message.AppendLine("Please enter your phone number in the phone number textbox and select your carrier from the carrier drop down.");

                        InfoWindow textInfo = new InfoWindow("Text Message Error", message.ToString());
                        if (textInfo.ShowDialog() == true)
                        {
                            //close window
                        }

                    }


                    valid = false;
                }

            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error validating settings", ex);
                valid = false;
            }
            return valid;
        }

        private void PhoneNumbertxtbx_previewTextinput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1) || phoneNumber_txtbx.Text.Length > 9)
                e.Handled = true;

        }

        private DateTime GetSchedulerNextRunTime()
        {
            DateTime nextRunTime = DateTime.Now;


            switch (CommonFunctions.UserSettings.SchedulerOption)
            {
                case (Settings.ScheduleType.Hours):
                    return nextRunTime.AddHours(CommonFunctions.UserSettings.ScheduleTime);

                case (Settings.ScheduleType.Days):
                    return nextRunTime.AddDays(CommonFunctions.UserSettings.ScheduleTime);
            }
            return nextRunTime;
        }

        private void FillToolTips()
        {
            startOnStartup_chbx.ToolTip = "Start amazon price checker when windows starts";
            startInTray_chbx.ToolTip = "Start Amazon Price Checker in windows tray";
            minimizeToTray_chbx.ToolTip = "Minimize the application to the windows tray rather than the task bar";
            closeMinimizes_chbx.ToolTip = "Minimize the application to the windows tray rather than closing application";
            priceCheckOnLaunch_chbx.ToolTip = "Automatically check for prices when the application launches";
            limitNotifications_chbx.ToolTip = "An item will only notify you once a day if the price is at the desired price when a price check is ran again";
            autoResizeBrowser_chbx.ToolTip = "Resize the application window to better fit the browser when looking at new items";
            browserFullScreenResize_rdbtn.ToolTip = "Resize the application to full screen when clicking the browser tab";
            browserFitWindowResize_rdbtn.ToolTip = "Resize the application to best fit when clicking the browser tab";
            resizeTabsBackToDefault_rdbtn.ToolTip = "Resize all other tabs back to default after navigating from the browser tab";
            resizeTabsBackToPrevious_rdbtn.ToolTip = "Resize all other tabs back to their previous size when navigating from the browser tab";
            noResizeTabs_rdbtn.ToolTip = "Do not resize the tabs back after navigating from the browser tab";
            logLevel_cmbobox.ToolTip = "Loggin level for the application";
            colorPicker.ToolTip = "Change the application's accent color";

            schedulerEnabled_chbx.ToolTip = "Enable the scheduler";
            scheduleTypeSelect_cmbobox.ToolTip = "Choose a schedule type";

            notifications_chbx.ToolTip = "Enable notifications";
            email_chbx.ToolTip = "Enable email notifications";
            email_txtbx.ToolTip = "Enter the email address notifications should be sent to";

            textMessages_chbx.ToolTip = "Enable text notifications";
            phoneNumber_txtbx.ToolTip = "Enter the phone number texts should be sent to";
            carrier_cmbobox.ToolTip = "Choose your mobile carrier";
            popupNotification_chbx.ToolTip = "Enable popup notifications";

            testNotifications_btn.ToolTip = "Send a test notification";

        }

        private async void TestNotifications_click(object sender, RoutedEventArgs e)
        {
            try
            {
                //First save the settings so the new info is saved before testing
                SaveSettings();

                this.Dispatcher.Invoke(() =>
                {
                    testNotifications_btn.Content = "Testing..";
                    testNotifications_btn.IsEnabled = false;
                });

                List<AmazonWatchItem> testItemList = new List<AmazonWatchItem>()
                {
                    new AmazonWatchItem(-1, "Item One", "http://www.amazon.com", (float)14.99, (float)20.00, DateTime.Now, DateTime.Now, DateTime.MinValue),
                    new AmazonWatchItem(-1, "Item Two", "http://www.amazon.com", (float)74.99, (float)100.00, DateTime.Now, DateTime.Now, DateTime.MinValue)
                };

                Notifier testNotifier = new Notifier(testItemList);

                testNotifier.Notify();

            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Unable to send test notification(s)", ex);
            }
            finally
            {
                await Task.Delay(1500);

                this.Dispatcher.Invoke(() =>
                {
                    testNotifications_btn.IsEnabled = true;
                    testNotifications_btn.Content = "Test Notifications";
                });

            }
        }

        private bool DoSettingsDiffer()
        {
            bool differ = false;

            try
            {
                differ = CommonFunctions.UserSettings.DoSettingsDiffer(startOnStartup_chbx.IsChecked ?? false,
                                         startInTray_chbx.IsChecked ?? false,
                                         minimizeToTray_chbx.IsChecked ?? false,
                                         closeMinimizes_chbx.IsChecked ?? false,
                                         priceCheckOnLaunch_chbx.IsChecked ?? false,
                                         limitNotifications_chbx.IsChecked ?? false,
                                         autoResizeBrowser_chbx.IsChecked ?? false,
                                         browserFullScreenResize_rdbtn.IsChecked ?? false,
                                         browserFitWindowResize_rdbtn.IsChecked ?? false,
                                         resizeTabsBackToPrevious_rdbtn.IsChecked ?? false,
                                         resizeTabsBackToDefault_rdbtn.IsChecked ?? false,
                                         noResizeTabs_rdbtn.IsChecked ?? false,
                                          ((ComboBoxItem)logLevel_cmbobox.SelectedItem).Content.ToString(),
                                         colorPicker.SelectedColor.ToString(),
                                         schedulerEnabled_chbx.IsChecked ?? false,
                                         Convert.ToInt32(schedule_sldr.Value),
                                         ((ComboBoxItem)scheduleTypeSelect_cmbobox.SelectedItem).Content.ToString(),
                                         notifications_chbx.IsChecked ?? false,
                                         email_chbx.IsChecked ?? false,
                                         email_txtbx.Text.Trim(),
                                         textMessages_chbx.IsChecked ?? false,
                                         phoneNumber_txtbx.Text.Trim(),
                                         ((ComboBoxItem)carrier_cmbobox.SelectedItem).Tag.ToString(),
                                         popupNotification_chbx.IsChecked ?? false
                                         );

            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Unable to check if settings differ", ex);
            }

            return differ;
        }

        private void Expander_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Expanders do not specifically have a closed trigger - they have an expanded trigger but only triggers when opening.
            //Since this is a preview click, the expander clicked isn't already open/closed when we check so we specifically have to
            //check each case to check the opposite case of the current expander clicked
            try
            {
                FrameworkElement fe = e.OriginalSource as FrameworkElement;
                if (fe is ToggleButton && fe.Name == "HeaderSite")
                {
                    var clickedExpander = ((FrameworkElement)sender).Name;
                    bool isOpening = !((Expander)sender).IsExpanded;

                    //If we are opening the expander we already know to reduce opacity and don't need to check others
                    //If we are closing, we specifically have to check all the others if they are closed
                    if (isOpening)
                    {
                        settings_scrollViewer.Background.Opacity = 0.1;
                    }
                    else
                    {
                        bool behaviorExpanderExpanded = behavior_expander.IsExpanded;
                        bool schedulerExpanderExpanded = scheduler_expander.IsExpanded;
                        bool notificationsExpanderExpanded = notifications_expander.IsExpanded;

                        switch (clickedExpander)
                        {
                            case "behavior_expander":
                                if (behaviorExpanderExpanded && !schedulerExpanderExpanded && !notificationsExpanderExpanded)
                                    settings_scrollViewer.Background.Opacity = 1;
                                else
                                    settings_scrollViewer.Background.Opacity = 0.1;
                                break;

                            case "scheduler_expander":
                                if (!behaviorExpanderExpanded && schedulerExpanderExpanded && !notificationsExpanderExpanded)
                                    settings_scrollViewer.Background.Opacity = 1;
                                else
                                    settings_scrollViewer.Background.Opacity = 0.1;
                                break;

                            case "notifications_expander":
                                if (!behaviorExpanderExpanded && !schedulerExpanderExpanded && notificationsExpanderExpanded)
                                    settings_scrollViewer.Background.Opacity = 1;
                                else
                                    settings_scrollViewer.Background.Opacity = 0.1;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error Checking Expanders", ex);
            }
        }

        #endregion


        #region Watch List

        public List<AmazonWatchItem> GetWatchItems()
        {
            List<AmazonWatchItem> itemList = new List<AmazonWatchItem>();

            foreach (AmazonWatchItem item in watchListItems_datagrid.Items)
            {
                itemList.Add(item);
            }
            return itemList;

        }

        public void FillWatchList()
        {
            watchListItems_datagrid.ItemsSource = null;
            watchListItems_datagrid.Items.Refresh();
            List<AmazonWatchItem> watchList = new List<AmazonWatchItem>();

            DataTable itemsDT = DBHelper.GetItemInformation(CommonFunctions.ItemsConnectionString);

            foreach (DataRow row in itemsDT.Rows)
            {
                int itemID = Int32.Parse(row["ItemID"].ToString());
                string title = row["Title"].ToString();
                string url = row["Url"].ToString();
                float amazonPrice = float.Parse(row["AmazonPrice"].ToString());
                float desiredPrice = float.Parse(row["DesiredPrice"].ToString());
                DateTime.TryParse(row["CreateDate"].ToString(), out DateTime createDate);
                DateTime.TryParse(row["LastModifiedDate"].ToString(), out DateTime lastModifiedDate);
                DateTime.TryParse(row["LastNotifiedDate"].ToString(), out DateTime lastNotifiedDate);

                CommonFunctions.Log.Debug($"Adding item to watch list:= '{itemID}', '{title}', '{url}', '{amazonPrice}', '{desiredPrice}', '{createDate}', '{lastModifiedDate}', '{lastNotifiedDate}'");
                AmazonWatchItem item = new AmazonWatchItem(itemID, title, url, amazonPrice, desiredPrice, createDate, lastModifiedDate, lastNotifiedDate);
                watchList.Add(item);
            }
            if (watchList.Count > 0)
            {
                watchListItems_datagrid.ItemsSource = watchList;
                watchListItems_datagrid.Items.Refresh();
            }
        }

        private void AmazonItemEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AmazonWatchItem itemToEdit = (AmazonWatchItem)watchListItems_datagrid.SelectedItem;

                if (itemToEdit != null)
                {

                    NumericInputDialog priceInput = new NumericInputDialog("Enter your desired price:");
                    if (priceInput.ShowDialog() == true)
                    {
                        float.TryParse(priceInput.Answer.Trim(), out float desiredPrice);

                        if (desiredPrice > 0)
                        {
                            CommonFunctions.Log.Debug($"Updaing price on {itemToEdit.Title} from {itemToEdit.DesiredPrice.ToString()} to {desiredPrice.ToString()}");
                            DBHelper.UpdateDesiredPrice(CommonFunctions.ItemsConnectionString, itemToEdit.Id, desiredPrice);
                            FillWatchList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error editing item price", ex);
            }
        }

        private void watchListItems_RowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AmazonItemEdit_Click(sender, e);
        }



        private void AmazonItemDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AmazonWatchItem itemToDelete = (AmazonWatchItem)watchListItems_datagrid.SelectedItem;
                CommonFunctions.Log.Debug($"Deleting item {itemToDelete.Title}");

                DBHelper.DeleteItem(CommonFunctions.ItemsConnectionString, itemToDelete.Id);
                FillWatchList();
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error deleting item", ex);
            }
        }

        private void EditItemCntxMnu_Click(object sender, RoutedEventArgs e)
        {
            AmazonItemEdit_Click(sender, e);
        }

        private void DeleteItemCntxMnu_Click(object sender, RoutedEventArgs e)
        {
            AmazonItemDelete_Click(sender, e);
        }

        private void OpenInBrowserCntxMnu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = (AmazonWatchItem)watchListItems_datagrid.SelectedItem;
                string url = selectedItem.Url;

                urlAddress_txtbox.Text = url;
                browser_tab.IsSelected = true;

                Browser.Address = url;
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error opening watch list item in browser", ex);
            }
        }


        private void watchListItemsDatagrid_CntxMnuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                if (watchListItems_datagrid.SelectedIndex < 0)
                {
                    openInBrowser_mnuItem.IsEnabled = false;
                    deleteItem_mnuItem.IsEnabled = false;
                    editItem_mnuItem.IsEnabled = false;
                }
                else
                {
                    openInBrowser_mnuItem.IsEnabled = true;
                    deleteItem_mnuItem.IsEnabled = true;
                    editItem_mnuItem.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error enabling/disabling watch list context menu items", ex);
            }
        }

        #endregion


        #region Browser

        private async void PriceWatch_Click(object sender, RoutedEventArgs e)
        {
            //verify we are on an amazon site
            if (urlAddress_txtbox.Text.Contains("amazon.co"))
            {

                if (!IsWatchingItem())
                {
                    NumericInputDialog priceInput = new Amazon_Price_Checker.NumericInputDialog("Enter your desired price:");
                    if (priceInput.ShowDialog() == true)
                    {
                        try
                        {
                            float.TryParse(priceInput.Answer.Trim(), out float desiredPrice);

                            if (desiredPrice > 0)
                            {
                                string itemTitle = string.Empty;
                                string itemPrice = string.Empty;

                                await Browser.EvaluateScriptAsync(@"document.getElementById('productTitle').innerHTML").ContinueWith(x =>
                                {
                                    var response = x.Result;

                                    if (response.Success && response.Result != null)
                                    {
                                        var result = response.Result;

                                        itemTitle = (string)result;

                                    }

                                });

                                await Browser.EvaluateScriptAsync(@"document.getElementById('price_inside_buybox').innerHTML").ContinueWith(x =>
                                {
                                    var response = x.Result;

                                    if (response.Success && response.Result != null)
                                    {
                                        var result = response.Result;

                                        itemPrice = (string)result;
                                    }

                                });

                                string url = urlAddress_txtbox.Text.Trim();
                                float itemPriceF = CommonFunctions.StringPriceToFloat(itemPrice.Trim());


                                bool itemInserted = DBHelper.InsertItem(CommonFunctions.ItemsConnectionString, CommonFunctions.RemoveSQLCharacters(itemTitle.Trim()), CommonFunctions.StrippedAmazonUrl(url), itemPriceF, desiredPrice);
                                CommonFunctions.Log.Debug($"Price watching {itemTitle} for {desiredPrice.ToString()}");
                                CommonFunctions.UpdatePriceWatchButton(itemInserted);

                                UpdateBrowserStatusText($"{itemTitle.Trim()} is now being watched", "There was an error watching this item at this time. Please try again later", itemInserted);

                                //Now update watch list tab
                                FillWatchList();

                            }
                            else
                            {
                                UpdateBrowserStatusText("", "Desired price must be greater than 0", false);
                            }

                        }
                        catch (Exception userPriceException)
                        {
                            CommonFunctions.Log.Error($"Error watching item {urlAddress_txtbox.Text.Trim()}", userPriceException);
                            UpdateBrowserStatusText("", "There was an error updating your desired price", false);
                        }
                    }
                }
                else
                {

                    NumericInputDialog priceInput = new Amazon_Price_Checker.NumericInputDialog("Enter your updated desired price:");
                    if (priceInput.ShowDialog() == true)
                    {
                        try
                        {
                            float.TryParse(priceInput.Answer.Trim(), out float desiredPrice);

                            if (desiredPrice > 0)
                            {
                                DataTable itemIdTable = DBHelper.GetItemID(CommonFunctions.ItemsConnectionString, CommonFunctions.AmazonProductFromUrl(CommonFunctions.StrippedAmazonUrl(urlAddress_txtbox.Text.Trim())));
                                if (itemIdTable.Rows.Count > 0)
                                {
                                    int itemID = Int32.Parse(itemIdTable.Rows[0]["ItemID"].ToString());
                                    bool updated = DBHelper.UpdateDesiredPrice(CommonFunctions.ItemsConnectionString, itemID, desiredPrice);

                                    UpdateBrowserStatusText("The desired price has been updated", "There was an error updating your desired price", updated);
                                    FillWatchList();
                                }
                            }
                        }
                        catch (Exception updateDesiredPriceException)
                        {
                            CommonFunctions.Log.Error($"Error updating desired price {urlAddress_txtbox.Text.Trim()}", updateDesiredPriceException);
                            UpdateBrowserStatusText("", "There was an error updating your desired price", false);
                        }
                    }

                }

            }
            else
            {
                MessageBox.Show("Amazon items are the only items able to me monitored",
                                "Price Watch Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);

                Browser.Address = "www.amazon.com";


            }

        }

        private async void UpdateBrowserStatusText(string successText, string failText, bool isSuccess)
        {
            try
            {
                helperWebBrowser_txt.Text = isSuccess ? successText : failText;
                helperWebBrowser_txt.Foreground = isSuccess ? Brushes.Green : Brushes.Red;

                await Task.Delay(10000);
                this.Dispatcher.Invoke(() =>
                {
                    helperWebBrowser_txt.Text = string.Empty;

                });
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error changing browser status text", ex);
            }
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            //When looking at a product on Amazon, clicking on a variation of that product (color, size, etc.) does NOT automatically fire the OnBeforeBrowse method.
            //Manually calling it here so we can price check different variations of products. 
            this.Dispatcher.Invoke(() => Browser.RequestHandler.OnBeforeBrowse(Browser.WebBrowser, Browser.GetBrowser(), Browser.GetFocusedFrame(), null, false, true));
        }

        private bool IsWatchingItem()
        {
            if (priceWatch_btn.Content == FindResource("WatchingItem_img"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion


        #region Tabs

        private void Tab_click(object sender, MouseButtonEventArgs e)
        {

            var tabLabel = sender as Label;
            TabItem currentTab = windowTabs_tabcntrl.SelectedItem as TabItem;


            if (tabLabel.Content.ToString() == "Browser")
            {
                BrowserTabClicked();
            }
            else if (currentTab.Name.ToString() == "browser_tab")
            {
                TabClicked();
            }
        }

        private void BrowserTabClicked()
        {

            if (CommonFunctions.UserSettings.ResizeTabsToPrevious)
            {
                previousTabHeight = Application.Current.MainWindow.Height;
                previousTabWidth = Application.Current.MainWindow.Width;

                CommonFunctions.Log.Debug($"Saving application size of {previousTabHeight} x {previousTabWidth}");
            }

            if (CommonFunctions.UserSettings.ResizeBrowser)
            {
                if (CommonFunctions.UserSettings.ResizeBrowserToFit)
                {
                    Application.Current.MainWindow.Width = 1075;
                    Application.Current.MainWindow.Height = 800;

                    CommonFunctions.Log.Debug($"Resizing application to 1075 x 800");
                }
                else if (CommonFunctions.UserSettings.ResizeBrowserFullScreen)
                {
                    WindowState = WindowState.Maximized;
                    CommonFunctions.Log.Debug($"Resizing application to fullscreen");
                }
            }
        }

        private void TabClicked()
        {

            if (CommonFunctions.UserSettings.ResizeTabsToPrevious)
            {
                WindowState = WindowState.Normal;
                Application.Current.MainWindow.Width = previousTabWidth;
                Application.Current.MainWindow.Height = previousTabHeight;
                CommonFunctions.Log.Debug($"Resizing application to {previousTabHeight} x {previousTabWidth}");
            }
            else if (CommonFunctions.UserSettings.ResizeTabsToDefault)
            {
                WindowState = WindowState.Normal;
                Application.Current.MainWindow.Width = 650;
                Application.Current.MainWindow.Height = 800;
                CommonFunctions.Log.Debug($"Resizing application to 650 x 800");
            }
        }



        #endregion


        #region MainWindow


        private void SetWindowState()
        {
            if (CommonFunctions.UserSettings.StartInTray)
            {
                //For some reason if you don't call show here first the taskbar context menu 
                //buttons wont be enabled until you bring up the window first.
                this.Show();
                this.Visibility = Visibility.Hidden;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            try
            {
                switch (this.WindowState)
                {
                    case WindowState.Maximized:
                        break;
                    case WindowState.Minimized:
                        if (CommonFunctions.UserSettings.MinimizeToTray)
                        {
                            CommonFunctions.Log.Debug($"Hiding application");
                            this.Visibility = Visibility.Hidden;

                            TaskBarNotification popupNotification = new TaskBarNotification();
                            popupNotification.BalloonText = this.Title;

                            TaskBarIcon.ShowCustomBalloon(popupNotification, PopupAnimation.Slide, 3500);
                        }
                        break;
                    case WindowState.Normal:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Window_StateChanged Error", ex);
            }
        }


        private void AmazonPriceChecker_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Since this method fires whenever the application begin to close, we must specifically check
            //if the user is meaning to exit the application so the Balloon Tip doesn't re-fire on exit
            try
            {
                if (CommonFunctions.UserSettings.MinimizeOnClose && !IsExiting)
                {
                    CommonFunctions.Log.Debug($"Hiding application on close");
                    this.Visibility = Visibility.Hidden;
                    e.Cancel = true;

                    TaskBarIcon.ShowBalloonTip(this.Title, "App is still running in the background", BalloonIcon.Info);
                }
                else
                {
                    if (DoSettingsDiffer())
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Your settings have not been saved.\n");
                        message.AppendLine("Would you like to save your settings before closing?");
                        ConfirmationWindow shouldSaveWindow = new ConfirmationWindow("Save Settings?", message.ToString());
                        if (shouldSaveWindow.ShowDialog() == true)
                        {
                            bool shouldSave = shouldSaveWindow.Confirmed;

                            if (shouldSave)
                                SaveSettings();


                            //clean up tray icon
                            TaskBarIcon.Dispose();
                            IsExiting = true;
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                    else
                    {
                        //clean up tray icon
                        TaskBarIcon.Dispose();
                        IsExiting = true;
                    }

                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error closing application", ex);
            }

        }


        private void TaskBarIcon_doubleClick(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Hidden || this.Visibility == Visibility.Collapsed)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.Hide();
                this.Visibility = Visibility.Hidden;
            }

        }





        #endregion

    }
}
