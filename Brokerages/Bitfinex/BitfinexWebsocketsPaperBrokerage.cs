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

    /// <summary>
    /// Bitfinex paper brokerage
    /// </summary>
    public class BitfinexWebsocketsPaperBrokerage : PaperBrokerage, IDataQueueHandler
    {

        BitfinexWebsocketsBrokerage brokerage;

        /// <summary>
        /// Retreive cash balance
        /// </summary>
        /// <returns></returns>
        public override List<Cash> GetCashBalance()
        {
            return brokerage.GetCashBalance();
        }

        /// <summary>
        /// Creates instance of brokerage
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="job"></param>
        public BitfinexWebsocketsPaperBrokerage(IAlgorithm algorithm, LiveNodePacket job) : base(algorithm, job)
        {
            brokerage = (BitfinexWebsocketsBrokerage)new BitfinexBrokerageFactory().CreateBrokerage(job, algorithm);
        }

        /// <summary>
        /// Get ticks from brokerage
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            return brokerage.GetNextTicks();
        }

        /// <summary>
        /// Begin ticker messages
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            brokerage.Subscribe(job, symbols);
        }

        /// <summary>
        /// End ticker messages
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            brokerage.Unsubscribe(job, symbols);
        }
    }
}
