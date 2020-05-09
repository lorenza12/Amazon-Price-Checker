using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using Amazon_Price_Checker.Common;
using System.Windows;
using System.Data;
using Amazon_Price_Checker.Notifications;

namespace Amazon_Price_Checker
{
    class PriceCheckWorker
    {

        private List<AmazonWatchItem> itemList = new List<AmazonWatchItem>();

        public PriceCheckWorker(List<AmazonWatchItem> amazonItemsList)
        {
            itemList = amazonItemsList;
        }

        public List<AmazonWatchItem> CheckPrices()
        {
            List<AmazonWatchItem> updatedItemsList = new List<AmazonWatchItem>();

            try
            {
                Parallel.ForEach<AmazonWatchItem>(itemList, item =>
                {
                    updatedItemsList.Add(CheckPriceTask(item));
                });

            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Error checking prices", e);
            }
            return updatedItemsList;
        }

        private AmazonWatchItem CheckPriceTask(AmazonWatchItem item)
        {
            try
            {

                var itemId = item.Id;
                var currentItemTitle = item.Title;
                var itemUrl = item.Url;
                var currentItemPrice = item.Price;
                var itemLastModifiedDate = item.LastModifiedDate;
                var itemCreateDate = item.CreateDate;
                var itemDesiredPrice = item.DesiredPrice;

                AmazonScraper scraper = new AmazonScraper(itemUrl);
                scraper.RequestHtml();

                var updatedTitle = scraper.Title;
                var updatedPrice = CommonFunctions.StringPriceToFloat(scraper.Price);

                AmazonWatchItem updatedItem = new AmazonWatchItem(itemId,
                                                                  !string.IsNullOrWhiteSpace(updatedTitle) ? updatedTitle : currentItemTitle,
                                                                  itemUrl,
                                                                  updatedPrice,
                                                                  itemDesiredPrice,
                                                                  itemCreateDate,
                                                                  !string.IsNullOrWhiteSpace(updatedTitle) && updatedPrice > 0 ? DateTime.Now : itemLastModifiedDate,
                                                                  default(DateTime));

                return updatedItem;
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("CheckPriceTask: Error checking prices", e);
            }
            return null;
        }

    }


    class ScheduledPriceCheck
    {

        private static System.Timers.Timer executionTimer;

        public int MillisecondsUntilExecution { get; set; }

        //the interval will be passed in on when the job should run

        public ScheduledPriceCheck()
        {
            MillisecondsUntilExecution = CommonFunctions.HoursToMilliseconds(24); //default starting at 24hrs
        }

        public void StartSchedule()
        {
            SetTimer();
        }

        private void SetTimer()
        {
            executionTimer = new Timer(this.MillisecondsUntilExecution);
            executionTimer.Elapsed += CheckPrices;
            executionTimer.AutoReset = CommonFunctions.UserSettings.SchedulerEnabled;
            executionTimer.Enabled = CommonFunctions.UserSettings.SchedulerEnabled;
        }

        public void CancelSchedule()
        {
            try
            {
                executionTimer.Stop();
                executionTimer.Enabled = false;
                executionTimer.AutoReset = false;
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Couldn't stop current timer", e);
            }
        }

        public void UpdateScheduleTime(int millisecondsUntilExecution)
        {
            //Will reset the timer
            if (millisecondsUntilExecution > 0)
            {
                executionTimer.Interval = millisecondsUntilExecution;
                CommonFunctions.UserSettings.SetNextScheduledPriceCheck(DateTime.Now.AddMilliseconds(millisecondsUntilExecution));
            }
            else
            {
                CommonFunctions.Log.Error("Timer interval must be greater than 0");
            }
        }



        private void CheckPrices(Object source, ElapsedEventArgs e)
        {
            if (!PriceChecker.CheckingPrices)
            {
                try
                {
                    List<AmazonWatchItem> itemList = Application.Current.Dispatcher.Invoke(() => ((MainWindow)Application.Current.MainWindow).GetWatchItems());
                    PriceChecker.CheckPrices(itemList);
                    CommonFunctions.UserSettings.SetNextScheduledPriceCheck(DateTime.Now.AddMilliseconds(this.MillisecondsUntilExecution));
                }
                catch (Exception ex)
                {
                    CommonFunctions.Log.Error("Error checking prices by scheduler", ex);
                }
            }
            else
            {
                try
                {
                    DisplayLogItem rescheduleLog = new DisplayLogItem();

                    //Manually was checking prices when scheduler tried to kicked off. Reschedule the scheduler to the settings time

                    switch (CommonFunctions.UserSettings.SchedulerOption)
                    {
                        case (Settings.ScheduleType.Hours):
                            rescheduleLog.LogText = DisplayLogItem.CreateLogText($"Price checker was already checking prices when the scheduler tried to begin.",
                                                                                 $"Rescheduling for: {CommonFunctions.UserSettings.ScheduleTime} hours");
                            rescheduleLog.LogImage = DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.Warning);
                            break;

                        case (Settings.ScheduleType.Days):
                            rescheduleLog.LogText = DisplayLogItem.CreateLogText($"Price checker was already checking prices when the scheduler tried to begin",
                                                                                 $"Rescheduling for: {CommonFunctions.UserSettings.ScheduleTime} days");
                            rescheduleLog.LogImage = DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.Warning);
                            break;
                    }

                    CommonFunctions.Log.Debug($"Rescheduling price checker for {this.MillisecondsUntilExecution} ms");
                    rescheduleLog.Log();
                    UpdateScheduleTime(this.MillisecondsUntilExecution);
                }
                catch (Exception rescheduleEx)
                {
                    CommonFunctions.Log.Error("Error rescheduling scheduler", rescheduleEx);
                }

            }

        }

    }

    class PriceChecker
    {
        public static bool CheckingPrices { get; set; }

        public static void CheckPrices(List<AmazonWatchItem> currentItems)
        {
            CheckingPrices = true;

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                //Clear previous logs
                MainWindow.LogDataGrid.Items.Clear();
                MainWindow.LogDataGrid.UpdateLayout();

                //Update Last Price Check
                CommonFunctions.UserSettings.SetLastExecutedPriceCheck(DateTime.Now);
                ((MainWindow)Application.Current.MainWindow).RefreshLastPriceCheck();

                //Update button
                ((MainWindow)Application.Current.MainWindow).startPriceCheck_btn.Content = "Checking Prices...";
                ((MainWindow)Application.Current.MainWindow).startPriceCheck_btn.IsEnabled = false;
            }));


            var watch = System.Diagnostics.Stopwatch.StartNew();

            List<AmazonWatchItem> updatedItemList = new List<AmazonWatchItem>();
            DisplayLogItem startLog = new DisplayLogItem(DisplayLogItem.CreateLogText("Starting Price Check",
                                                                                      $"{ DateTime.Now }"),
                                                                                      DisplayLogItem.LogIcon.Start);
            startLog.Log();

            try
            {


                Task.Factory.StartNew(() =>
                {
                    //Check Prices
                    PriceCheckWorker priceChecker = new PriceCheckWorker(currentItems);
                    updatedItemList = priceChecker.CheckPrices();
                })
                .ContinueWith(task =>
                {
                    watch.Stop();
                    var elapsedSeconds = watch.ElapsedMilliseconds * 0.001;
                    CommonFunctions.Log.Debug($"Checking amazon prices");


                    //Update the DB with new prices
                    UpdateItemsDB(updatedItemList);

                    //Compare the prices and notify if necessary
                    ComparePrices();

                    DisplayLogItem finishLog = new DisplayLogItem(DisplayLogItem.CreateLogText($"Price Check Complete",
                                                                           updatedItemList.Count > 1 ? $"{updatedItemList.Count} items have been checked" : $"{updatedItemList.Count} item has been checked",
                                                                           $"Execution time: {elapsedSeconds}s"),
                                                                           DisplayLogItem.LogIcon.Success);
                    finishLog.Log();
                    CommonFunctions.Log.Debug($"Finished checking prices:= {elapsedSeconds}s");

                    //Finally re-enable the start button after the thread has finished and update the watchlist grid since we know the price check is complete
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).startPriceCheck_btn.Content = "Start Price Check";
                        ((MainWindow)Application.Current.MainWindow).startPriceCheck_btn.IsEnabled = true;
                        ((MainWindow)Application.Current.MainWindow).FillWatchList();

                    }));


                });
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Unable to check prices", e);
                DisplayLogItem errorLog = new DisplayLogItem($"An error occurred when checking prices", DisplayLogItem.LogIcon.Error);
                errorLog.Log();

            }
            finally
            {
                CheckingPrices = false;
            }

        }

        private static void UpdateItemsDB(List<AmazonWatchItem> itemList)
        {

            try
            {
                foreach (AmazonWatchItem item in itemList)
                {
                    DBHelper.UpdateItem(CommonFunctions.ItemsConnectionString, item.Id, item.Title, item.Price, item.LastModifiedDate);

                }

            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Unable to update DB with new prices", ex);

                DisplayLogItem errorLog = new DisplayLogItem
                {
                    LogText = "An error occurred when updating the items",
                    LogImage = DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.Error)
                };

                errorLog.Log();
            }

        }

        private static void ComparePrices()
        {
            List<AmazonWatchItem> notificationList = new List<AmazonWatchItem>();

            DataTable itemsTable = DBHelper.GetItemInformation(CommonFunctions.ItemsConnectionString);

            foreach (DataRow row in itemsTable.Rows)
            {
                try
                {
                    DisplayLogItem priceLog = new DisplayLogItem();

                    int itemID = Int32.Parse(row["ItemID"].ToString());
                    string title = row["Title"].ToString();
                    string url = row["Url"].ToString();
                    float amazonPrice = float.Parse(row["AmazonPrice"].ToString());
                    float desiredPrice = float.Parse(row["DesiredPrice"].ToString());
                    DateTime.TryParse(row["CreateDate"].ToString(), out DateTime createDate);
                    DateTime.TryParse(row["LastModifiedDate"].ToString(), out DateTime lastModifiedDate);
                    DateTime.TryParse(row["LastNotifiedDate"].ToString(), out DateTime lastNotifiedDate);

                    CommonFunctions.Log.Debug($"Comparing price for {title}:= ${amazonPrice} vs ${desiredPrice}");

                    if (amazonPrice <= desiredPrice && amazonPrice > 0)
                    {
                        if (!CommonFunctions.UserSettings.LimitNotifications)
                        {
                            AmazonWatchItem notifyItem = new AmazonWatchItem(itemID, title, url, amazonPrice, desiredPrice, createDate, lastModifiedDate, lastNotifiedDate);
                            notificationList.Add(notifyItem);
                        }
                        else if (CommonFunctions.UserSettings.LimitNotifications && lastNotifiedDate.Date != DateTime.Now.Date)
                        {
                            AmazonWatchItem notifyItem = new AmazonWatchItem(itemID, title, url, amazonPrice, desiredPrice, createDate, lastModifiedDate, lastNotifiedDate);
                            notificationList.Add(notifyItem);
                        }

                        DisplayLogItem notifyLog = new DisplayLogItem
                        {
                            LogText = DisplayLogItem.CreateLogText($"{CommonFunctions.RemoveSQLCharacters(title)}",
                                                                               $"is at or below your desired price of ${desiredPrice}! You should consider buying!",
                                                                               $"Current Price: ${amazonPrice}",
                                                                               $"Desired Price: ${desiredPrice}"),
                            LogImage = DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.Buy)
                        };


                        notifyLog.Log();
                    }
                    else
                    {
                        priceLog.LogText = DisplayLogItem.CreateLogText($"{CommonFunctions.RemoveSQLCharacters(title)}",
                                                                        amazonPrice > 0 ? $"Current Price: ${amazonPrice}" : $"Current Price: Unable to find",
                                                                        $"Desired Price: ${desiredPrice}");

                        priceLog.LogImage = amazonPrice > 0 ? DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.Item) : DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.NoPriceItem);
                        priceLog.Log();
                    }
                }
                catch (Exception compareError)
                {
                    CommonFunctions.Log.Error("Error comparing desired price and purchase price", compareError);

                    DisplayLogItem compareErrorLog = new DisplayLogItem("The was an error comparing prices for one of your items", DisplayLogItem.LogIcon.Error);
                    compareErrorLog.Log();
                }


            }

            if (notificationList.Count > 0)
            {
                if (CommonFunctions.UserSettings.EmailNotifications || CommonFunctions.UserSettings.TextNotifications)
                {
                    DisplayLogItem notifyLog = new DisplayLogItem();
                    notifyLog.LogText = DisplayLogItem.CreateLogText("Notifications", $"Sending alert for {notificationList.Count} " + (notificationList.Count > 1 ? "items" : "item"));
                    notifyLog.LogImage = DisplayLogItem.GetLogIcon(DisplayLogItem.LogIcon.Notification);
                    notifyLog.Log();
                }

                Notifier notifier = new Notifier(notificationList);
                notifier.Notify();
            }

        }

    }
}


