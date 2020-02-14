using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using Amazon_Price_Checker.Common;
using HtmlAgilityPack;

namespace Amazon_Price_Checker
{
    class AmazonScraper
    {

        public string Url { get; set; }
        public string Title { get; set; }
        public string Price { get; set; }

        public AmazonScraper(string url)
        {
            this.Url = url;

        }

        public void RequestHtml()
        {
            if (!string.IsNullOrEmpty(this.Url))
            {
                try
                {
                    //Amazon uses gzip encoding so have to decode it
                    //https://weblog.west-wind.com/posts/2007/jun/29/httpwebrequest-and-gzip-http-responses
                    //article on decoding gzip

                    HttpWebRequest http = (HttpWebRequest)WebRequest.Create(Url);
                    http.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

                    HttpWebResponse webResponse = (HttpWebResponse)http.GetResponse();

                    Stream responseStream = responseStream = webResponse.GetResponseStream();
                    if (webResponse.ContentEncoding.ToLower().Contains("gzip"))
                    {
                        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    }
                    else if (webResponse.ContentEncoding.ToLower().Contains("deflate"))
                    {
                        responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                    }

                    StreamReader reader = new StreamReader(responseStream, Encoding.Default);

                    string responseHtml = reader.ReadToEnd();

                    webResponse.Close();
                    responseStream.Close();

                    var html = responseHtml;

                    if (!string.IsNullOrEmpty(html))
                    {
                        try
                        {
                            var htmlDocument = new HtmlDocument();
                            htmlDocument.LoadHtml(html);

                            SetAttributes(htmlDocument);
                        }
                        catch (Exception setHtmlException)
                        {
                            CommonFunctions.Log.Error($"Unable to set html for {this.Url}", setHtmlException);
                        }
                    }
                    else
                    {
                        CommonFunctions.Log.Error($"Unable to retrieve html for {this.Url}");
                    }

                }
                catch (Exception requestHtmlException)
                {
                    CommonFunctions.Log.Error("Error: AmazonScraper - RequestHtml", requestHtmlException);
                }
            }
            else
            {
                CommonFunctions.Log.Warn("No url provided");
            }
        }

        private void SetAttributes(HtmlDocument htmlDoc)
        {
            GetTilte(htmlDoc);
            GetPrice(htmlDoc);
        }

        private void GetTilte(HtmlDocument htmlDoc)
        {
            try
            {
                this.Title = htmlDoc.GetElementbyId("productTitle").InnerText.Trim();
            }
            catch (Exception titleException)
            {
                CommonFunctions.Log.Error($"Unable to find title for {this.Url}", titleException);
            }
        }

        private void GetPrice(HtmlDocument htmlDoc)
        {
            //First try the most common price options by id
            GetPriceByID(htmlDoc);

            //If we still can't find a price, try looking at classes as music and books sometimes use this
            if (string.IsNullOrEmpty(this.Price))
            {
                GetPriceByClass(htmlDoc);
            }


            if (string.IsNullOrEmpty(this.Price))
            {
                CommonFunctions.Log.Warn($"Unable to get price for item: {this.Title ?? this.Url}");
            }

        }

        private void GetPriceByID(HtmlDocument htmlDoc)
        {
            //Prices can be under multiple different html ids, so get the first price that exists, if any
            var priceIdOptions = new List<string> { "price_inside_buybox", "priceblock_saleprice", "priceblock_dealprice", "priceblock_ourprice" };

            int attempt = 0;

            do
            {
                try
                {
                    this.Price = htmlDoc.GetElementbyId(priceIdOptions[attempt]).InnerText.Trim();
                }
                catch (NullReferenceException priceIdException)
                {
                    //Don't really need to log as it can be quite common these are null - Use for debugging purposes
                    //CommonFunctions.log.Debug($"{priceIdOptions[attempt]} did not exist for item: {this.Title ?? this.Url}", priceIdException);
                }
                catch (Exception e)
                {
                    CommonFunctions.Log.Error($"{priceIdOptions[attempt]} threw an error for item: {this.Title ?? this.Url}", e);
                }
                attempt++;

            } while (string.IsNullOrEmpty(this.Price) && attempt < priceIdOptions.Count);
        }

        private void GetPriceByClass(HtmlDocument htmlDoc)
        {

            //Prices can also be under multiple different classes (books, music, etc.), so get the first price that exists, if any
            var priceClassOptions = new List<string> { "inlineBlock-display" };

            int classAttempt = 0;

            do
            {
                try
                {
                    var priceClass = htmlDoc.DocumentNode.SelectNodes($"//div[contains(@class, '{priceClassOptions[classAttempt]}')]");
                    this.Price = priceClass[0].InnerText.Trim();
                }
                catch (NullReferenceException priceClassException)
                {
                    CommonFunctions.Log.Error($"{priceClassOptions[classAttempt]} did not exist for item: {this.Title ?? this.Url}", priceClassException);
                }
                catch (Exception e)
                {
                    CommonFunctions.Log.Error($"{priceClassOptions[classAttempt]} threw an error for item: {this.Title ?? this.Url}", e);
                }
                classAttempt++;

            } while (string.IsNullOrEmpty(this.Price) && classAttempt < priceClassOptions.Count);

        }


    }
}
