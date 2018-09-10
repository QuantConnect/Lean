using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Factory method to create binance Websockets brokerage
    /// </summary>
    public class BinanceBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public BinanceBrokerageFactory() : base(typeof(BinanceBrokerage))
        {
        }

        /// <summary>
        /// Not required
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// provides brokerage connection data
        /// </summary>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "binance-rest" , Config.Get("binance-rest", "https://api.binance.com")},
            { "binance-wss" , Config.Get("binance-wss", "wss://stream.binance.com:9443")},
            { "binance-api-key", Config.Get("binance-api-key")},
            { "binance-api-secret", Config.Get("binance-api-secret")}
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        public override IBrokerageModel BrokerageModel => new BinanceBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] { "binance-rest", "binance-wss", "binance-api-secret", "binance-api-key" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"BinanceBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var priceProvider = new ApiPriceProvider(job.UserId, job.UserToken);

            var brokerage = new BinanceBrokerage(
                job.BrokerageData["binance-wss"],
                job.BrokerageData["binance-rest"],
                job.BrokerageData["binance-api-key"],
                job.BrokerageData["binance-api-secret"],
                algorithm,
                priceProvider);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
