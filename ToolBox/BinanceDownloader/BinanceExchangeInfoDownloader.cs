using Newtonsoft.Json;
using QuantConnect.ToolBox.BinanceDownloader.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace QuantConnect.ToolBox.BinanceDownloader
{
    /// <summary>
    /// Binance implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class BinanceExchangeInfoDownloader : IExchangeInfoDownloader
    {
        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.Binance;

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.binance.com/api/v3/exchangeInfo");

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfo>(reader.ReadToEnd());

                foreach (var symbol in exchangeInfo.Symbols)
                {
                    var priceFilter = symbol.Filters
                        .First(f => f.GetValue("filterType").ToString() == "PRICE_FILTER")
                        .GetValue("tickSize");

                    var lotFilter = symbol.Filters
                        .First(f => f.GetValue("filterType").ToString() == "LOT_SIZE")
                        .GetValue("stepSize");


                    yield return $"binance,{symbol.Name},crypto,{symbol.Name},{symbol.QuoteAsset},1,{priceFilter.ToString()},{lotFilter.ToString()}";
                }
            }
        }
    }
}
