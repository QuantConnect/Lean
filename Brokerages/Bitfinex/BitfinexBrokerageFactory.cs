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
    public class BitfinexBrokerageFactory : BrokerageFactory
    {

        public BitfinexBrokerageFactory()
            : base(typeof(BitfinexBrokerage))
        {
        }

        public override void Dispose()
        {

        }

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

        public override IBrokerageModel BrokerageModel
        {
            get { return new BitfinexBrokerageModel(); }
        }

        public override Interfaces.IBrokerage CreateBrokerage(Packets.LiveNodePacket job, Interfaces.IAlgorithm algorithm)
        {
            var brokerage = new BitfinexWebsocketsBrokerage();
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
