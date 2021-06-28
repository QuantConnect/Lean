using QuantConnect.Brokerages.TDAmeritrade;
using QuantConnect.Configuration;
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
    public class TDAmeritradeDataDownloader : IDataDownloader
    {
        private readonly TDAmeritradeClient tdClient;

        public TDAmeritradeDataDownloader()
        {
            var clientID = Config.Get("td-client-id", "");
            var redirectUri = Config.Get("td-redirect-uri", "");
            var tdCredentials = new TDCliCredentialProvider();
            tdClient = new TDAmeritradeClient(clientID, redirectUri);
            tdClient.LogIn(tdCredentials).Wait();
        }

        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            return TDAmeritradeBrokerage.GetPriceHistory(tdClient, symbol, resolution, startUtc, endUtc);
        }
    }
}
