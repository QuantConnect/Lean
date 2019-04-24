using System;
using System.Globalization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using QuantConnect.Data;
using System.Linq;

namespace QuantConnect.Data.Custom.Tiingo
{
/// <summary>
/// Custom Data Type: News data - https://api.tiingo.com/
/// </summary>
    public class News : BaseData
    {
        public int Timestamp = 0;
        public string Description;
        public decimal ID = 0;
        public string PublishedDate;
        public string Source;
        public string[] Tags;
        public string[] Tickers;
        public string Title;
        public string Url;
        public string Datetime;

        public string ticker;

        /// <summary>
        /// 1. DEFAULT CONSTRUCTOR: Custom data types need a default constructor.
        /// We search for a default constructor so please provide one here. It won't be used for data, just to generate the "Factory".
        /// </summary>
        public News()
        {
         
        }

        /// <summary>
        /// 2. Reads a CSV file containing Tiingo News data for a specified symbol.
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {

            string source = "../../../Data/alternative/Tiingo News/" + config.Symbol.Value + ".csv";
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
        }

        /// <summary>
        /// 3. READER METHOD: Read 1 line from data source and convert it into Object.
        /// Each line of the CSV File is presented in here. The backend downloads your file, loads it into memory and then line by line
        /// feeds it into your algorithm.
        /// </summary>
        /// <param name="line">string line from the data source file submitted above</param>
        /// <param name="config">Subscription data, symbol name, data type</param>
        /// <param name="date">Current date we're requesting. This allows you to break up the data source into daily files.</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>New News Object which extends BaseData.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {

            var news = new News();
            // Example Line Format:
            
            // crawlDate                         description                                                                         id         publishedDate         source             tags                                         tickers                             title                                                                   url                                                                                                       dateTime
            // 2019 - 04 - 18T10: 47:53.526096Z  Only a small fraction of Alphabet's revenue is generated outside of the search      16334943   2019-04-17T10:45:00Z  seekingalpha.com   ['Consumer Cyclical', 'Stock',               ['aapl', 'amzn', 'goog', 'googl']   What's Up, Doc? Google Health May Become The Company's Next Home Run    https://seekingalpha.com/article/4254953-doc-google-health-may-become-companys-next-home-run;2019-04-18   10:47:53
            //                                   and ad business, and the company faces a few challenges here. With a number of Ot                                                       'Technology', 'The Investment Strategist']

            try
            {
                string[] data = line.Split(';');
                news.Time = DateTime.Parse(data[0].Substring(0, 19), CultureInfo.InvariantCulture);
                news.Description = Convert.ToString(data[1], CultureInfo.InvariantCulture);
                news.ID = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                news.PublishedDate = Convert.ToString(data[3], CultureInfo.InvariantCulture);
                news.Source = Convert.ToString(data[4], CultureInfo.InvariantCulture);
                news.Tags = Convert.ToString(data[5], CultureInfo.InvariantCulture).Replace("[", "").Replace("]", "").Replace("'", "").Split(',');
                news.Tickers = Convert.ToString(data[6], CultureInfo.InvariantCulture).Replace("[", "").Replace("]", "").Replace("'", "").Replace(" ", "").Split(',');
                news.Title = Convert.ToString(data[7], CultureInfo.InvariantCulture);
                news.Url = Convert.ToString(data[8], CultureInfo.InvariantCulture);
                news.Datetime = Convert.ToString(data[9], CultureInfo.InvariantCulture);

            }
            catch { /* Do nothing, skip first title row */ }

            return news;
        }
    }
}
