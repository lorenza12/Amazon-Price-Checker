using log4net;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Media;

namespace Amazon_Price_Checker.Common
{
    class CommonFunctions
    {
        public static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Settings UserSettings = new Settings();

        public static readonly string ItemsConnectionString = ConfigurationManager.ConnectionStrings["ItemsConnection"].ConnectionString;


        public static void UpdateLogLevel(string logLevel)
        {
            try
            {
                Log.Info($"Setting log level to '{logLevel}'");
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = Log.Logger.Repository.LevelMap[logLevel];
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error($"Error changing log level to '{logLevel}'", ex);
            }
        }

        public static float StringPriceToFloat(string price)
        {
            try
            {
                if (!string.IsNullOrEmpty(price))
                {
                    price = price.Trim().Replace("$", "").Replace(",", "");
                    float.TryParse(price, out float priceF);
                    return priceF;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Unable to convert '{price}' to float", e);
            }
            return -1;
        }

        public static string RemoveSQLCharacters(string s)
        {
            try
            {
                s = System.Net.WebUtility.HtmlDecode(s.Replace("'", ""));

                string strippedString = s.Trim();

                Log.Debug($"Removing special characters from {s}:= {strippedString}");

                return strippedString;
            }
            catch (Exception e)
            {
                Log.Error("Unable to replace string characters", e);
                return "";
            }
        }

        public static string StrippedAmazonUrl(string url)
        {
            try
            {
                if (url.Trim().Contains("ref"))
                {
                    string strippedAmazonUrl = url.Substring(0, url.IndexOf("ref")).Trim();
                    Log.Debug($"Stripping amazon url {url}:= {strippedAmazonUrl}");
                    //return the bare amazon url 
                    return strippedAmazonUrl;
                }
            }
            catch (Exception e)
            {
                Log.Error("Unable to strip amazon url", e);
            }
            //Not looking at an item so just return the url
            return url;
        }

        public static string AmazonProductFromUrl(string url)
        {
            try
            {
                if (url.Trim().Contains("/dp/"))
                {
                    var productStart = url.IndexOf("/dp/") + 4;

                    int productEnd;
                    if (url.IndexOf("/", productStart) == -1)
                    {
                        productEnd = url.IndexOf("?", productStart);
                    }
                    else
                    {
                        productEnd = url.IndexOf("/", productStart);
                    }

                    var productLength = productEnd - productStart;

                    var product = url.Substring(productStart, productLength).Trim();
                    Log.Debug($"Amazon product from url: '{product}'");

                    //return the amazon product - the product is located like so - /dp/XXXXXX/
                    return product;
                }
            }
            catch (Exception e)
            {
                Log.Error("Unable to get amazon product from url", e);
            }
            //Not looking at an item so just return the url
            return url;
        }

        public static int DaysToMilliseconds(int days)
        {
            int daysInMilliseconds = HoursToMilliseconds(days * 24);
            Log.Debug($"Converting {days} days to milliseconds:= {daysInMilliseconds}");
            return daysInMilliseconds;
        }

        public static int HoursToMilliseconds(int hours)
        {
            int hoursInMilliseconds = hours * 3600000;
            Log.Debug($"Converting {hours} hours to milliseconds:= {hoursInMilliseconds}");
            //1 hour = 60 minutes = 60 × 60 seconds = 3600 seconds = 3600 × 1000 milliseconds = 3,600,000 ms
            return hoursInMilliseconds;
        }

        public static int MinutesToMilliseconds(int minutes)
        {
            int minutesInMilliseconds = minutes * 60000;
            Log.Debug($"Converting {minutes} minutes to milliseconds:= {minutesInMilliseconds}");
            return minutesInMilliseconds;
        }

        public static void UpdatePriceWatchButton(bool isWatching)
        {
            if (isWatching)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).priceWatch_btn.Content = ((MainWindow)Application.Current.MainWindow).FindResource("WatchingItem_img");
                    ((MainWindow)Application.Current.MainWindow).priceWatch_btn.Background = Brushes.Gold;
                    ((MainWindow)Application.Current.MainWindow).priceWatch_btn.ToolTip = "Edit desired price";
                }));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).priceWatch_btn.Content = ((MainWindow)Application.Current.MainWindow).FindResource("PriceWatch_img");
                    ((MainWindow)Application.Current.MainWindow).priceWatch_btn.Background = Brushes.Transparent;
                    ((MainWindow)Application.Current.MainWindow).priceWatch_btn.ToolTip = "Watch this item";
                }));
            }

        }

    }
}