using System;

namespace Amazon_Price_Checker
{
    public class AmazonWatchItem
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public float Price { get; set; }

        public float DesiredPrice { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public DateTime LastNotifiedDate { get; set; }

        public AmazonWatchItem(int id, string title, string url, float price, float desiredPrice, DateTime createDate, DateTime lastModifiedDate, DateTime lastNotifiedDate)
        {
            this.Id = id;
            this.Title = title;
            this.Url = url;
            this.Price = price;
            this.DesiredPrice = desiredPrice;
            this.CreateDate = createDate;
            this.LastModifiedDate = lastModifiedDate;
            this.LastNotifiedDate = lastNotifiedDate;
        }

    }
}
