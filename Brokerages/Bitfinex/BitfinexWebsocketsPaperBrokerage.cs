using QuantConnect.Brokerages.Paper;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Bitfinex
{

    public class BitfinexWebsocketsPaperBrokerage : PaperBrokerage, IDataQueueHandler
    {

        BitfinexWebsocketsBrokerage brokerage;

        public override List<Cash> GetCashBalance()
        {
            return brokerage.GetCashBalance();
        }

        public BitfinexWebsocketsPaperBrokerage(IAlgorithm algorithm, LiveNodePacket job) : base(algorithm, job)
        {
            brokerage = (BitfinexWebsocketsBrokerage)new BitfinexBrokerageFactory().CreateBrokerage(job, algorithm);
        }

        public IEnumerable<BaseData> GetNextTicks()
        {
            return brokerage.GetNextTicks();
        }

        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            brokerage.Subscribe(job, symbols);
        }

        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            brokerage.Unsubscribe(job, symbols);
        }
    }
}
