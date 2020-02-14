using CefSharp;
using System;
using CefSharp.Handler;
using Amazon_Price_Checker.Common;

namespace Amazon_Price_Checker.Handlers
{
    public class BrowserRequestHandler : RequestHandler
    {

        private string url = string.Empty;

        protected override bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl,
            WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            try
            {
                bool newUrl = IsNavigatingToNewUrl(frame.Url);

                if (newUrl)
                {
                    this.url = frame.Url;
                    string strippedUrl = CommonFunctions.StrippedAmazonUrl(this.url);
                    string product = CommonFunctions.AmazonProductFromUrl(strippedUrl);

                    if (strippedUrl != "www.amazon.com/" && strippedUrl != "http://www.amazon.com/" && strippedUrl != "https://www.amazon.com/")
                    {
                        ////Check if user is watching the current url as long as we aren't just on the amazon main page
                        if (DBHelper.IsWatchingUrl(CommonFunctions.ItemsConnectionString, product))
                        {
                            CommonFunctions.UpdatePriceWatchButton(true);
                        }
                        else
                        {
                            CommonFunctions.UpdatePriceWatchButton(false);
                        }
                    }
                    else
                    {
                        CommonFunctions.UpdatePriceWatchButton(false);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error checking url", ex);
            }


            //returning false allows the browser to continue navigating to the url, true cancels
            return false;

        }


        private bool IsNavigatingToNewUrl(string navUrl)
        {
            //We are navigating to a new url:
            //OnBeforeBrowse fires multiple times when loading a page but we only want to check the DB once
            if (this.url != navUrl && !string.IsNullOrEmpty(navUrl) && (navUrl.Contains("https://www.amazon.co") || navUrl.Contains("http://www.amazon.co")))
            {
                return true;
            }
            else
            {
                //Loading the same page or not loading an item so don't do anything
                return false;
            }
        }

    }
}