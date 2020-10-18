using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Factory method to create Zerodha Websockets brokerage
    /// </summary>
    public class ZerodhaBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Factory constructor
        /// </summary>
        public ZerodhaBrokerageFactory() : base(typeof(ZerodhaBrokerage))
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
            { "zerodha-api-key", Config.Get("zerodha-api-key")},
            { "zerodha-api-secret", Config.Get("zerodha-api-secret")},
            { "zerodha-access-token", Config.Get("zerodha-access-token")}
        };

        /// <summary>
        /// The brokerage model
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new ZerodhaBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var required = new[] { "zerodha-api-key", "zerodha-api-secret", "zerodha-access-token" };

            foreach (var item in required)
            {
                if (string.IsNullOrEmpty(job.BrokerageData[item]))
                    throw new Exception($"ZerodhaBrokerageFactory.CreateBrokerage: Missing {item} in config.json");
            }

            var brokerage = new ZerodhaBrokerage(
                job.BrokerageData["zerodha-api-key"],
                job.BrokerageData["zerodha-api-secret"],
                job.BrokerageData["zerodha-access-token"],
                algorithm);
            //Add the brokerage to the composer to ensure its accessible to the live data feed.
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);
            Composer.Instance.AddPart<IHistoryProvider>(brokerage);
            return brokerage;
        }
    }
}
