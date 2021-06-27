using QuantConnect.Brokerages.TDAmeritrade;
using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDAmeritradeApi.Client;
using TDAmeritradeApi.Client.Models.MarketData;

namespace QuantConnect.ToolBox.TDAmeritradeDownloader
{
    public class TDAmeritradeDownloader : IDataDownloader
    {
        private readonly TDAmeritradeClient tdClient;

        public TDAmeritradeDownloader(string clientID, string redirectUri, ICredentials tdCredentials)
        {
            tdClient = new TDAmeritradeClient(clientID, redirectUri);
            tdClient.LogIn(tdCredentials).Wait();
        }

        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            return TDAmeritradeBrokerage.GetPriceHistory(tdClient, symbol, resolution, startUtc, endUtc);
        }
    }
}
