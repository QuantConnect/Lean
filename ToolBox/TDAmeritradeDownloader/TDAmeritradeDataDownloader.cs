using QuantConnect.Brokerages.TDAmeritrade;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using TDAmeritradeApi.Client;

namespace QuantConnect.ToolBox.TDAmeritradeDownloader
{
    public class TDAmeritradeDataDownloader : IDataDownloader
    {
        private static TDAmeritradeClient tdClient;

        public TDAmeritradeDataDownloader()
        {
            var clientID = Config.Get("td-client-id", "");
            var redirectUri = Config.Get("td-redirect-uri", "");
            var tdCredentials = Composer.Instance.GetExportedValueByTypeName<ICredentials>(Config.Get("td-credentials-provider", "QuantConnect.Brokerages.TDAmeritrade.TDCliCredentialProvider"));
            TDAmeritradeBrokerage.InitializeClient(clientID, redirectUri, tdCredentials);
        }

        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            return TDAmeritradeBrokerage.GetPriceHistory(symbol, resolution, startUtc, endUtc, TimeZones.NewYork);
        }
    }
}
