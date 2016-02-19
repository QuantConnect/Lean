using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Factory method to create Bitfinex Websockets brokerage
    /// </summary>
    public class BitfinexBrokerageFactory : BrokerageFactory
    {

        /// <summary>
        /// Factory constructor
        /// </summary>
        public BitfinexBrokerageFactory()
            : base(typeof(BitfinexBrokerage))
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
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "bitfinex-api-secret", Config.Get("bitfinex-api-secret") },
                    { "bitfinex-api-key", Config.Get("bitfinex-api-key") }
                };
            }
        }

        /// <summary>
        /// The brokerage model
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new BitfinexBrokerageModel(); }
        }

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override Interfaces.IBrokerage CreateBrokerage(Packets.LiveNodePacket job, Interfaces.IAlgorithm algorithm)
        {
            var brokerage = new BitfinexWebsocketsBrokerage();
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
