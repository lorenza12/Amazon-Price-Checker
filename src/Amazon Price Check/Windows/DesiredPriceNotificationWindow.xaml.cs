using Amazon_Price_Checker.Common;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Wpf;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Amazon_Price_Checker.Windows
{
    /// <summary>
    /// Interaction logic for DesiredPriceNotificationWindow.xaml
    /// </summary>
    public partial class DesiredPriceNotificationWindow : Window
    {

        private readonly string html;
        private bool loaded = false;
        public DesiredPriceNotificationWindow(string htmlContent)
        {
            InitializeComponent();

            this.html = htmlContent;


            NotificationBrowser.MenuHandler = new NotificationBrowserCustomMenuHandler();
            NotificationBrowser.RequestHandler = new NotificationBrowserRequestHandler();
            NotificationBrowser.LoadingStateChanged += OnLoadingStateChanged;


        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {

            if (this.loaded)
                Application.Current.Dispatcher.Invoke(() => NotificationBrowser.RequestHandler.OnBeforeBrowse(NotificationBrowser.WebBrowser,
                                                                                                              NotificationBrowser.GetBrowser(),
                                                                                                              NotificationBrowser.GetFocusedFrame(),
                                                                                                              null, false, true));

            else if (!args.IsLoading && !this.loaded)
            {
                // Page has finished loading, do whatever you want here
                NotificationBrowser.LoadHtml(this.html);

                //Had to resort to using a loaded bool as the html loads so fast the LoadingStateChanged kept on firing
                this.loaded = true;

            }

        }

    }



    #region RequestHandler
    public class NotificationBrowserRequestHandler : RequestHandler
    {

        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            try
            {
                if (request != null && request.Url.Contains("amazon.co") && !request.Url.Contains("data:text/html"))
                {
                    Process.Start(request.Url);
                    return true;
                }

                else if (request == null)
                    return true;


            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error checking url", ex);
            }


            //returning false allows the browser to continue navigating to the url, true cancels
            return false;

        }
    }
    #endregion

    #region ContextMenuHandler
    public class NotificationBrowserCustomMenuHandler : IContextMenuHandler
    {
        //Disable so user can't go forwards and backwards
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
        }

        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {

        }

        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }
    #endregion

}
