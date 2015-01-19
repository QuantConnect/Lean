using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Live trading data feed used for paper trading.
    /// </summary>
    public class PaperTradingDataFeed : LiveTradingDataFeed
    {
        // this is unused right now, but will be used later
        private readonly LiveNodePacket _job;

        /// <summary>
        /// Creates a new PaperTradingDataFeed for the algorithm/job
        /// </summary>
        /// <param name="algorithm">The algorithm to receive the data, used for a complete listing of active securities</param>
        /// <param name="job">The job being run</param>
        public PaperTradingDataFeed(IAlgorithm algorithm, LiveNodePacket job)
            : base(algorithm)
        {
            _job = job;
        }

        /// <summary>
        /// Gets the next ticks from the live trading feed
        /// </summary>
        /// <returns>The next ticks to be processed</returns>
        public override IEnumerable<Tick> GetNextTicks()
        {
            return Engine.Queue.NextTicks();
        }
    }
}
