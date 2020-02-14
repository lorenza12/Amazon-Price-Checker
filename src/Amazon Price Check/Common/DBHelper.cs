using System;
using System.Data;
using System.Data.SqlClient;


namespace Amazon_Price_Checker.Common
{
    class DBHelper
    {

        public static DataTable GetItemInformation(string connectionString)
        {
            string Command = "SELECT ItemID, Title, Url, AmazonPrice, DesiredPrice, CreateDate, LastModifiedDate, LastNotifiedDate FROM AmazonItems";

            DataTable dtResult = new DataTable();

            if (!string.IsNullOrEmpty(connectionString))
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        using (SqlDataAdapter myDataAdapter = new SqlDataAdapter(Command, connection))
                        {
                            myDataAdapter.Fill(dtResult);
                        }
                    }
                    catch (Exception queryException)
                    {
                        CommonFunctions.Log.Error("Error querying DB", queryException);
                    }
                }
            }
            return dtResult;
        }

        public static bool InsertItem(string connectionString, string title, string url, float price, float desiredPrice)
        {
            string commandText = $"INSERT INTO AmazonItems (Title, Url, AmazonPrice, DesiredPrice) VALUES ('{title}', '{url}', {price}, {desiredPrice});";

            try
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(commandText, connection))
                        {
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception insertException)
            {
                CommonFunctions.Log.Error("Error inserting item into DB", insertException);
                return false;
            }
            return true;
        }

        public static void UpdateItem(string connectionString, int itemID, string title, float price, DateTime updateTime)
        {
            string commandText = $"UPDATE AmazonItems Set  Title = '{title}', AmazonPrice = {price}, LastModifiedDate = '{updateTime}' WHERE ItemID = {itemID};";

            try
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(commandText, connection))
                        {
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception updateException)
            {
                CommonFunctions.Log.Error($"Error updating item: {title}", updateException);
            }
        }

        public static bool UpdateDesiredPrice(string connectionString, int itemID, float newPrice)
        {
            string commandText = $"UPDATE AmazonItems Set  DesiredPrice = {newPrice} WHERE ItemID = {itemID};";

            try
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(commandText, connection))
                        {
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception updatePriceException)
            {
                CommonFunctions.Log.Error($"Error updating itemID: {itemID}", updatePriceException);
                return false;
            }
            return true;
        }

        public static DataTable GetItemID(string connectionString, string product)
        {
            string Command = $"SELECT ItemID FROM AmazonItems WHERE Url like '%{product}%'";
            DataTable dtResult = new DataTable();
            if (!string.IsNullOrEmpty(connectionString))
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        using (SqlDataAdapter myDataAdapter = new SqlDataAdapter(Command, connection))
                        {
                            myDataAdapter.Fill(dtResult);
                        }
                    }
                    catch (Exception queryException)
                    {
                        CommonFunctions.Log.Error("Error getting itemID", queryException);
                        return null;
                    }
                }
            }
            return dtResult;
        }

        public static void DeleteItem(string connectionString, int itemID)
        {
            string commandText = $"DELETE FROM AmazonItems WHERE ItemID = {itemID};";

            try
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(commandText, connection))
                        {
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception deleteException)
            {
                CommonFunctions.Log.Error($"Error deleting itemID: {itemID}", deleteException);
            }
        }

        public static bool IsWatchingUrl(string connectionString, string product)
        {
            string Command = $"SELECT ItemID FROM AmazonItems WHERE Url like '%{product}%'";

            DataTable dtResult = new DataTable();

            if (!string.IsNullOrEmpty(connectionString))
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        using (SqlDataAdapter myDataAdapter = new SqlDataAdapter(Command, connection))
                        {
                            myDataAdapter.Fill(dtResult);
                        }
                    }
                    catch (Exception queryException)
                    {
                        CommonFunctions.Log.Error("Error querying DB", queryException);
                    }
                }
            }
            return dtResult.Rows.Count > 0;
        }

    }


}

