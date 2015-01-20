using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Test live trading data feed with fast forward capability
    /// </summary>
    public class TestLiveTradingDataFeed : PaperTradingDataFeed
    {
        private readonly int _fastForward;
        private DateTime _current = DateTime.Now;

        /// <summary>
        /// Creates a test live trading data feed with the specified fast forward factor
        /// </summary>
        public TestLiveTradingDataFeed(IAlgorithm algorithm, LiveNodePacket job, int fastForward = 100) 
            : base(algorithm, job)
        {
            _fastForward = fastForward;
        }

        public override IEnumerable<Tick> GetNextTicks()
        {
            var ticks = Engine.Queue.NextTicks().ToList();
            var multiplied = Multiply(ticks, _fastForward);
            foreach (var tick in multiplied)
            {
                _current += TimeSpan.FromMilliseconds(100);
                tick.Time = _current;
                yield return tick;
            }
        }

        private IEnumerable<T> Multiply<T>(List<T> enumerable, int count)
        {
            IEnumerable<T> result = enumerable;
            for (int i = 1; i < count; i++)
            {
                result = result.Concat(enumerable);
            }
            return result;
        }
    }
}