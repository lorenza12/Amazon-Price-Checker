using System;
using Microsoft.Win32;

namespace Amazon_Price_Checker.Common
{
    class Settings
    {
        private static string startupKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        #region Behavior - Getters Setters
        public bool StartOnStartup { get; set; }

        public bool StartInTray { get; set; }

        public bool MinimizeToTray { get; set; }

        public bool MinimizeOnClose { get; set; }

        public bool PriceCheckOnLaunch { get; set; }

        public bool LimitNotifications { get; set; }

        public bool ResizeBrowser { get; set; }

        public bool ResizeBrowserFullScreen { get; set; }

        public bool ResizeBrowserToFit { get; set; }

        public bool NoResizingTabs { get; set; }

        public bool ResizeTabsToPrevious { get; set; }

        public bool ResizeTabsToDefault { get; set; }

        public string LogLevel { get; set; }

        public string AccentColor { get; set; }

        #endregion

        #region Schedule - Getters Setters

        public enum ScheduleType { Hours, Days };

        public bool SchedulerEnabled { get; set; }

        internal ScheduleType SchedulerOption { get; set; }

        public int ScheduleTime { get; set; }

        public DateTime LastPriceCheck { get; set; }

        public DateTime NextScheduledPriceCheck { get; private set; }

        #endregion

        #region Notifications - Getters Setters
        public bool ReceiveNotifications { get; set; }

        public bool EmailNotifications { get; set; }

        public string EmailAddress { get; set; }

        public bool TextNotifications { get; set; }

        public string PhoneNumber { get; set; }

        public string CarrierAddress { get; set; }

        public bool PopupNotifications { get; set; }

        #endregion


        public Settings()
        {
            FillSettings();
        }

        public bool SaveSettings(bool startOnStartup, bool startInTray, bool minimizeToTray, bool minimizeOnClose,
                                 bool priceCheckOnLaunch, bool limitNotifications, bool resizeBrowser, bool resizeBrowserFullScreen, bool resizeBrowserToFit,
                                 bool resizeTabsToPrevious, bool resizeTabsToDefault, bool resizeTabs, string logLevel, string accentColor, bool schedulerEnabled, int scheduleTime,
                                 string schedulerOption, bool receiveNotifications, bool emailNotifications, string emailAddress, bool textNotifications,
                                 string phoneNumber, string carrierAddress, bool popupNotifications)
        {
            bool saved = true;
            try
            {
                //Add or remove from registry if the startup checkbox changed
                if (Properties.Settings.Default.StartOnStartup != startOnStartup)
                    ToggleStartupRegistry(startOnStartup);

                Properties.Settings.Default.StartOnStartup = startOnStartup;
                Properties.Settings.Default.StartInTray = startInTray;
                Properties.Settings.Default.MinimizeToTray = minimizeToTray;
                Properties.Settings.Default.MinimizeOnClose = minimizeOnClose;
                Properties.Settings.Default.PriceCheckOnLaunch = priceCheckOnLaunch;
                Properties.Settings.Default.LimitNotifications = limitNotifications;
                Properties.Settings.Default.ResizeBrowser = resizeBrowser;
                Properties.Settings.Default.ResizeBrowserFullScreen = resizeBrowserFullScreen;
                Properties.Settings.Default.ResizeBrowserToFit = resizeBrowserToFit;
                Properties.Settings.Default.ResizeTabsToPrevious = resizeTabsToPrevious;
                Properties.Settings.Default.ResizeTabsToDefault = resizeTabsToDefault;
                Properties.Settings.Default.NoResizingTabs = resizeTabs;
                Properties.Settings.Default.LogLevel = logLevel;
                Properties.Settings.Default.AccentColor = accentColor;

                Properties.Settings.Default.SchedulerEnabled = schedulerEnabled;
                Properties.Settings.Default.ScheduleTime = scheduleTime;
                Properties.Settings.Default.SchedulerOption = schedulerOption;

                Properties.Settings.Default.ReceiveNotifications = receiveNotifications;
                Properties.Settings.Default.EmailNotifications = emailNotifications;
                Properties.Settings.Default.EmailAddress = emailAddress;
                Properties.Settings.Default.TextNotifications = textNotifications;
                Properties.Settings.Default.PhoneNumber = phoneNumber;
                Properties.Settings.Default.CarrierAddress = carrierAddress;
                Properties.Settings.Default.PopupNotifications = popupNotifications;

                Properties.Settings.Default.Save();

                FillSettings();
                CommonFunctions.Log.Debug("Saving Settings");
                LogSettings();


            }
            catch (Exception)
            {
                CommonFunctions.Log.Error("Error saving user settings");
                saved = false;
            }
            return saved;
        }

        public void SetLastExecutedPriceCheck(DateTime lastExecutedTime)
        {
            try
            {
                CommonFunctions.Log.Debug($"Setting last executed price check to {lastExecutedTime.ToString()}");
                this.LastPriceCheck = lastExecutedTime;
                Properties.Settings.Default.LastPriceCheck = lastExecutedTime;
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Error saving last executed price check", e);
            }

        }

        public void SetNextScheduledPriceCheck(DateTime nextExecutionTime)
        {
            try
            {
                CommonFunctions.Log.Debug($"Setting next scheduled price check to {nextExecutionTime.ToString()}");
                this.NextScheduledPriceCheck = nextExecutionTime;
                Properties.Settings.Default.NextScheduledPriceCheck = nextExecutionTime;
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Error saving next scheduled runtime", e);
            }
        }

        public bool DoSettingsDiffer(bool startOnStartup, bool startInTray, bool minimizeToTray, bool minimizeOnClose,
                                     bool priceCheckOnLaunch, bool limitNotifications, bool resizeBrowser, bool resizeBrowserFullScreen, bool resizeBrowserToFit,
                                     bool resizeTabsToPrevious, bool resizeTabsToDefault, bool resizeTabs, string logLevel, string accentColor, bool schedulerEnabled, int scheduleTime,
                                     string schedulerOption, bool receiveNotifications, bool emailNotifications, string emailAddress, bool textNotifications,
                                     string phoneNumber, string carrierAddress, bool popupNotifications)
        {
            try
            {

                if (this.StartOnStartup != startOnStartup)
                    return true;

                if (this.StartInTray != startInTray)
                    return true;

                if (this.MinimizeToTray != minimizeToTray)
                    return true;

                if (this.MinimizeOnClose != minimizeOnClose)
                    return true;

                if (this.PriceCheckOnLaunch != priceCheckOnLaunch)
                    return true;

                if (this.LimitNotifications != limitNotifications)
                    return true;

                if (this.ResizeBrowser != resizeBrowser)
                    return true;

                if (this.ResizeBrowserFullScreen != resizeBrowserFullScreen)
                    return true;

                if (this.ResizeBrowserToFit != resizeBrowserToFit)
                    return true;

                if (this.NoResizingTabs != resizeTabs)
                    return true;

                if (this.ResizeTabsToPrevious != resizeTabsToPrevious)
                    return true;

                if (this.ResizeTabsToDefault != resizeTabsToDefault)
                    return true;

                if (this.LogLevel != logLevel)
                    return true;

                if (this.AccentColor != accentColor)
                    return true;




                if (this.SchedulerEnabled != schedulerEnabled)
                    return true;

                if (this.ScheduleTime != scheduleTime)
                    return true;

                if (this.SchedulerOption.ToString() != schedulerOption)
                    return true;



                if (this.ReceiveNotifications != receiveNotifications)
                    return true;

                if (this.EmailNotifications != emailNotifications)
                    return true;

                if (this.EmailAddress != emailAddress)
                    return true;

                if (this.TextNotifications != textNotifications)
                    return true;

                if (this.PhoneNumber != phoneNumber)
                    return true;

                if (this.CarrierAddress != carrierAddress)
                    return true;

                if (this.PopupNotifications != popupNotifications)
                    return true;


            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error checking if settings differ", ex);
            }

            return false;
        }


        private void FillSettings()
        {
            this.StartOnStartup = Properties.Settings.Default.StartOnStartup;
            this.StartInTray = Properties.Settings.Default.StartInTray;
            this.MinimizeToTray = Properties.Settings.Default.MinimizeToTray;
            this.MinimizeOnClose = Properties.Settings.Default.MinimizeOnClose;
            this.PriceCheckOnLaunch = Properties.Settings.Default.PriceCheckOnLaunch;
            this.LimitNotifications = Properties.Settings.Default.LimitNotifications;
            this.ResizeBrowser = Properties.Settings.Default.ResizeBrowser;
            this.ResizeBrowserFullScreen = Properties.Settings.Default.ResizeBrowserFullScreen;
            this.ResizeBrowserToFit = Properties.Settings.Default.ResizeBrowserToFit;
            this.NoResizingTabs = Properties.Settings.Default.NoResizingTabs;
            this.ResizeTabsToPrevious = Properties.Settings.Default.ResizeTabsToPrevious;
            this.ResizeTabsToDefault = Properties.Settings.Default.ResizeTabsToDefault;
            this.LogLevel = Properties.Settings.Default.LogLevel;
            this.AccentColor = Properties.Settings.Default.AccentColor;

            this.SchedulerEnabled = Properties.Settings.Default.SchedulerEnabled;
            this.SchedulerOption = Properties.Settings.Default.SchedulerOption == "Hours" ? ScheduleType.Hours : ScheduleType.Days;
            this.ScheduleTime = Properties.Settings.Default.ScheduleTime;
            this.LastPriceCheck = Properties.Settings.Default.LastPriceCheck;
            this.NextScheduledPriceCheck = Properties.Settings.Default.NextScheduledPriceCheck;

            this.ReceiveNotifications = Properties.Settings.Default.ReceiveNotifications;
            this.EmailNotifications = Properties.Settings.Default.EmailNotifications;
            this.EmailAddress = Properties.Settings.Default.EmailAddress;
            this.TextNotifications = Properties.Settings.Default.TextNotifications;
            this.PhoneNumber = Properties.Settings.Default.PhoneNumber;
            this.CarrierAddress = Properties.Settings.Default.CarrierAddress;
            this.PopupNotifications = Properties.Settings.Default.PopupNotifications;

            UpdateLogLevel(this.LogLevel); //Must set the log level on startup because by defualt it is set to ALL

        }

        private void ToggleStartupRegistry(bool isAdding)
        {
            try
            {

                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;


                RegistryKey key = Registry.CurrentUser.OpenSubKey(startupKeyPath, true);

                if (isAdding)
                {
                    key.SetValue("AmazonPriceCheck", exePath);
                    CommonFunctions.Log.Debug($"Adding {exePath} to registry");
                }

                else
                {
                    key.DeleteValue("AmazonPriceCheck", false);
                    CommonFunctions.Log.Debug($"Removing {exePath} from registry");
                }

            }
            catch (Exception ex)
            {
                string verb = isAdding ? "adding" : "removing";
                CommonFunctions.Log.Error($"Error {verb} startup registry key", ex);
            }

        }

        private void UpdateLogLevel(string logLevel)
        {
            CommonFunctions.UpdateLogLevel(logLevel);
        }

        public void LogSettings()
        {
            CommonFunctions.Log.Debug($"StartOnStartup:= {Properties.Settings.Default.StartOnStartup}");
            CommonFunctions.Log.Debug($"StartInTray:= {Properties.Settings.Default.StartInTray}");
            CommonFunctions.Log.Debug($"MinimizeToTray:= {Properties.Settings.Default.MinimizeToTray}");
            CommonFunctions.Log.Debug($"MinimizeOnClose:= {Properties.Settings.Default.MinimizeOnClose}");
            CommonFunctions.Log.Debug($"PriceCheckOnLaunch:= {Properties.Settings.Default.PriceCheckOnLaunch}");
            CommonFunctions.Log.Debug($"LimitNotifications:= {Properties.Settings.Default.LimitNotifications}");
            CommonFunctions.Log.Debug($"ResizeBrowser:= {Properties.Settings.Default.ResizeBrowser}");
            CommonFunctions.Log.Debug($"ResizeBrowserFullScreen:= {Properties.Settings.Default.ResizeBrowserFullScreen}");
            CommonFunctions.Log.Debug($"ResizeBrowserToFit:= {Properties.Settings.Default.ResizeBrowserToFit}");
            CommonFunctions.Log.Debug($"ResizeTabsToPrevious:= {Properties.Settings.Default.ResizeTabsToPrevious}");
            CommonFunctions.Log.Debug($"ResizeTabsToDefault:= {Properties.Settings.Default.ResizeTabsToDefault}");
            CommonFunctions.Log.Debug($"NoResizingTabs:= {Properties.Settings.Default.NoResizingTabs}");
            CommonFunctions.Log.Debug($"LogLevel:= {Properties.Settings.Default.LogLevel}");
            CommonFunctions.Log.Debug($"AccentColor:= {Properties.Settings.Default.AccentColor}");
            CommonFunctions.Log.Debug($"SchedulerEnabled:= {Properties.Settings.Default.SchedulerEnabled}");
            CommonFunctions.Log.Debug($"ScheduleTime:= {Properties.Settings.Default.ScheduleTime}");
            CommonFunctions.Log.Debug($"SchedulerOption:= {Properties.Settings.Default.SchedulerOption}");
            CommonFunctions.Log.Debug($"ReceiveNotifications:= {Properties.Settings.Default.ReceiveNotifications}");
            CommonFunctions.Log.Debug($"EmailNotifications:= {Properties.Settings.Default.EmailNotifications}");
            CommonFunctions.Log.Debug($"EmailAddress:= {Properties.Settings.Default.EmailAddress}");
            CommonFunctions.Log.Debug($"TextNotifications:= {Properties.Settings.Default.TextNotifications}");
            CommonFunctions.Log.Debug($"PhoneNumber:= {Properties.Settings.Default.PhoneNumber}");
            CommonFunctions.Log.Debug($"CarrierAddress:= {Properties.Settings.Default.CarrierAddress}");
            CommonFunctions.Log.Debug($"PopupNotifications:= {Properties.Settings.Default.PopupNotifications}");
        }


        private void DefaultSettings()
        {
            this.StartOnStartup = false;
            this.StartInTray = false;
            this.MinimizeToTray = false;
            this.MinimizeOnClose = false;
            this.PriceCheckOnLaunch = false;
            this.LimitNotifications = false;
            this.ResizeBrowser = false;
            this.ResizeBrowserFullScreen = false;
            this.ResizeBrowserToFit = true;
            this.NoResizingTabs = true;
            this.ResizeTabsToPrevious = false;
            this.ResizeTabsToDefault = false;
            this.LogLevel = "ERROR";
            this.AccentColor = "#FF1E90FF";

            this.SchedulerEnabled = false;
            this.SchedulerOption = ScheduleType.Hours;
            this.ScheduleTime = 12;

            this.ReceiveNotifications = false;
            this.EmailNotifications = false;
            this.EmailAddress = string.Empty;
            this.TextNotifications = false;
            this.PhoneNumber = string.Empty;
            this.CarrierAddress = string.Empty;
            this.PopupNotifications = false;
        }





    }
}
