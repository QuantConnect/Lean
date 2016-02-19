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
    /// Alternate brokerage factory provided for paper testing using REST only
    /// </summary>
    //todo: further testing
    public class BitfinexRestBrokerageFactory : BrokerageFactory
    {


        /// <summary>
        /// Create factory instance
        /// </summary>
        public BitfinexRestBrokerageFactory()
            : base(typeof(BitfinexBrokerage))
        {
        }

        /// <summary>
        /// Empty dispose method
        /// </summary>
        public override void Dispose()
        {

        }

        /// <summary>
        /// Data for brokerage
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
        /// Brokerage Model
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new BitfinexBrokerageModel(); }
        }

        /// <summary>
        /// Create brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override Interfaces.IBrokerage CreateBrokerage(Packets.LiveNodePacket job, Interfaces.IAlgorithm algorithm)
        {
            var brokerage = new BitfinexBrokerage();

            return brokerage;
        }
    }
}
