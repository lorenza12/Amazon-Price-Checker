using Amazon_Price_Checker.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Windows;
using System.Text;
using Amazon_Price_Checker.Windows;

namespace Amazon_Price_Checker.Notifications
{
    class Notifier
    {

        private string emailHtml;

        private List<AmazonWatchItem> notifyItemsList;


        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/
        static string[] Scopes = { GmailService.Scope.GmailSend };
        static string ApplicationName = "Amazon Price Check";



        public Notifier(List<AmazonWatchItem> notifyItems)
        {
            notifyItemsList = notifyItems;
        }


        public void Notify()
        {
            if (CommonFunctions.UserSettings.ReceiveNotifications)
            {
                if (CommonFunctions.UserSettings.EmailNotifications)
                {
                    GetEmailHtml();

                    if (!string.IsNullOrWhiteSpace(this.emailHtml))
                    {
                        bool htmlUpdated = FillHtml();

                        if (htmlUpdated)
                        {

                            UserCredential credentials = GetGmailCredentials();

                            if (credentials != null)
                            {
                                SendEmail(credentials);
                            }
                            else
                            {
                                //Purge token so they can try authenticating again
                                PurgeUserToken();

                                StringBuilder message = new StringBuilder();
                                message.AppendLine("Unable to get Gmail credentials.");
                                message.AppendLine("Your user token has now been reset.\n");
                                message.AppendLine("Please try again.");

                                InfoWindow badCredentialsWindow = new InfoWindow("Error Sending Email", message.ToString());

                                if (badCredentialsWindow.ShowDialog() == true)
                                {

                                }
                            }
                        }
                        else
                        {
                            CommonFunctions.Log.Warn("Unable to update email html");
                        }
                    }
                    else
                    {
                        CommonFunctions.Log.Warn("Unable to create email html");
                    }
                }

                if (CommonFunctions.UserSettings.TextNotifications)
                {
                    UserCredential credentials = GetGmailCredentials();

                    if (credentials != null)
                    {
                        SendText(credentials);
                    }
                    else
                    {
                        //Purge token so they can try authenticating again
                        PurgeUserToken();

                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Unable to get Gmail credentials.");
                        message.AppendLine("Your user token has now been reset.\n");
                        message.AppendLine("Please try again.");

                        InfoWindow badCredentialsWindow = new InfoWindow("Error Sending Text Message", message.ToString());

                        if (badCredentialsWindow.ShowDialog() == true)
                        {

                        }
                    }
                }


                if (CommonFunctions.UserSettings.PopupNotifications)
                {
                    OpenPopup();
                }

                //Finally, change the last notified date
                UpdateLastNotifiedDate();
            }

        }

        public void SendEmail(UserCredential credentials)
        {
            try
            {
                string emailAddress = CommonFunctions.UserSettings.EmailAddress;
                CommonFunctions.Log.Debug($"Sending email notification to {emailAddress}");

                //Create Message
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("fromemailaddress@gmail.com");
                mail.To.Add(new MailAddress(emailAddress));

                mail.IsBodyHtml = true;

                mail.Subject = "Amazon Price Check - Price Drop!";

                mail.Body = this.emailHtml;

                MimeKit.MimeMessage mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mail);

                Message message = new Message();
                message.Raw = Base64UrlEncode(mimeMessage.ToString());


                // Create Gmail API service.
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = ApplicationName,
                });

                //Send Email
                var result = service.Users.Messages.Send(message, "me").Execute();
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error Sending Email", ex);
            }

        }

        public void SendText(UserCredential credentials)
        {
            try
            {
                CommonFunctions.Log.Debug($"Sending text notification to {CommonFunctions.UserSettings.PhoneNumber + CommonFunctions.UserSettings.CarrierAddress}");
                //Create Message
                MailMessage mail = new MailMessage();
                mail.Subject = "Amazon Price Check - Price Drop!";
                mail.From = new MailAddress("fromemailaddress@gmail.com");
                mail.To.Add(new MailAddress(CommonFunctions.UserSettings.PhoneNumber + CommonFunctions.UserSettings.CarrierAddress));
                mail.IsBodyHtml = false;

                StringBuilder textMessage = FillTextMessage();
                mail.Body = textMessage.ToString();

                MimeKit.MimeMessage mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mail);

                Message message = new Message();
                message.Raw = Base64UrlEncode(mimeMessage.ToString());


                // Create Gmail API service.
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = ApplicationName,
                });

                //Send Text
                var result = service.Users.Messages.Send(message, "me").Execute();
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error Sending Email", ex);
            }

        }

        private void OpenPopup()
        {
            try
            {

                GetEmailHtml();

                if (!string.IsNullOrWhiteSpace(this.emailHtml))
                {
                    CommonFunctions.Log.Debug($"Opening popup notification window");
                    bool htmlUpdated = FillHtml();

                    if (htmlUpdated)
                    {
                        //TODO possibly make window flash?
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DesiredPriceNotificationWindow desiredPriceWindow = new DesiredPriceNotificationWindow(this.emailHtml);
                            desiredPriceWindow.Show();

                        });

                    }
                }
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error("Error creating popup notification", e);
            }
        }



        private UserCredential GetGmailCredentials()
        {
            try
            {
                UserCredential credential;
                CommonFunctions.Log.Debug("Getting Google Credentials");

                using (var stream =
                    new FileStream("GoogleApi.json", FileMode.Open, FileAccess.Read))
                {
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    string credPath = "token.json";
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }

                if (!string.IsNullOrEmpty(credential.Token.AccessToken) && !string.IsNullOrEmpty(credential.Token.RefreshToken))
                {
                    CommonFunctions.Log.Debug("Refresh and Access token successfully retrieved.");
                    CommonFunctions.Log.Info("Access token expires " + credential.Token.IssuedUtc.AddSeconds(credential.Token.ExpiresInSeconds.Value).ToLocalTime().ToString());
                }


                if (credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default))
                {
                    CommonFunctions.Log.Debug("Access token needs refreshing.");
                    var refreshResult = credential.RefreshTokenAsync(CancellationToken.None).Result;
                }

                return credential;

            }
            catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException ex)
            {
                if (ex.Error.Error == "access_denied")
                {
                    string denied = "Text and email notifications will not work if you don't allow it access to your Gmail.";

                    InfoWindow noAccessWindow = new InfoWindow("Gmail Access Denied", denied);

                    CommonFunctions.Log.Warn("User did not provide authorisation code. Notifications will not be able to work.");
                }
                else
                {
                    CommonFunctions.Log.Error("Unable to authenticate with Google. The following error occurred:", ex);
                }
            }
            catch (Exception e)
            {
                CommonFunctions.Log.Error($"Error Getting Gmail Credentials: {e.Message}", e);
            }

            return null;
        }

        private string Base64UrlEncode(string input)
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }

        private void GetEmailHtml()
        {
            try
            {
                var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                var notificationsDir = @"Notifications\EmailHtml.html";

                var htmlFile = Path.Combine(baseDir, notificationsDir);

                using (var reader = new StreamReader(htmlFile))
                {
                    emailHtml = reader.ReadToEnd();

                }

            }

            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error setting the email html template", ex);
            }
        }

        private bool FillHtml()
        {
            bool success = true;
            try
            {
                StringBuilder itemHtml = new StringBuilder();

                int itemCounter = 0;
                foreach (AmazonWatchItem item in this.notifyItemsList)
                {
                    if (itemCounter % 2 != 0)
                    {
                        HtmlOddItem(item, itemHtml);
                    }
                    else
                    {
                        HtmlEvenItem(item, itemHtml);
                    }

                    itemCounter++;
                }

                this.emailHtml = this.emailHtml.Replace("#ItemRows#", itemHtml.ToString());
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error filling email html", ex);
                success = false;
            }
            return success;
        }

        private void HtmlEvenItem(AmazonWatchItem item, StringBuilder sb)
        {
            sb.AppendLine("        <div class=\"col\">");
            sb.AppendLine("            <div class=\"box\">");
            sb.AppendLine("              <ul>");
            sb.AppendLine($"                <li class=\"header header-color\" style=\"list-style-type: none\">{item.Title}</li>");
            sb.AppendLine($"                <li><strong class=\"price\">Current Price:</strong> ${item.Price.ToString()}</li>");
            sb.AppendLine($"                <li><strong class=\"price\">Desired Price:</strong> ${item.DesiredPrice.ToString()}</li>");
            sb.AppendLine($"                <li><a href = \"{item.Url}\" class=\"button\">Item Link</a></li>");
            sb.AppendLine("              </ul>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
            sb.AppendLine(Environment.NewLine);
        }

        private void HtmlOddItem(AmazonWatchItem item, StringBuilder sb)
        {
            sb.AppendLine("        <div class=\"col\">");
            sb.AppendLine("            <div class=\"box\">");
            sb.AppendLine("              <ul>");
            sb.AppendLine($"                <li class=\"header header-color-odd\">{item.Title}</li>");
            sb.AppendLine($"                <li><strong class=\"price\">Current Price:</strong> ${item.Price.ToString()}</li>");
            sb.AppendLine($"                <li><strong class=\"price\">Desired Price:</strong> ${item.DesiredPrice.ToString()}</li>");
            sb.AppendLine($"                <li><a href = \"{item.Url}\" class=\"button\">Item Link</a></li>");
            sb.AppendLine("              </ul>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
            sb.AppendLine(Environment.NewLine);
        }

        private StringBuilder FillTextMessage()
        {
            StringBuilder textMessage = new StringBuilder();

            try
            {
                foreach (AmazonWatchItem item in this.notifyItemsList)
                {
                    textMessage.AppendLine($"Item: {item.Title}");
                    textMessage.AppendLine($"Current Price: {item.Price}");
                    textMessage.AppendLine($"Desired Price: {item.DesiredPrice}");
                    textMessage.AppendLine($"Link: {item.Url}");
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error filling text message", ex);
            }

            return textMessage;
        }

        private void PurgeUserToken()
        {
            try
            {
                if (Directory.Exists("token.json"))
                    Directory.Delete("token.json", true);
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error purging token.json credentials", ex);
            }

        }

        private void UpdateLastNotifiedDate()
        {
            try
            {
                CommonFunctions.Log.Debug($"Updating last notified dates for items");

                foreach (AmazonWatchItem item in notifyItemsList)
                {
                    try
                    {
                        DBHelper.UpdateLastNotifiedDate(CommonFunctions.ItemsConnectionString, item.Id, DateTime.Now);
                    }
                    catch (Exception updateEx)
                    {
                        CommonFunctions.Log.Error($"Error upating last notified date for {item.Title}", updateEx);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.Log.Error("Error upating last notified date for items", ex);
            }
        }

    }
}
