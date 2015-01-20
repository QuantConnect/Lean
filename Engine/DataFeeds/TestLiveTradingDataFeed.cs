using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
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
        private readonly DateTime _start;
        private readonly TimeSpan _tickResolution;
        private readonly int _fastForward = Config.GetInt("test-live-data-feed-fast-forward", 10);
        private readonly TimeSpan _period = TimeSpan.FromHours(Config.GetDouble("test-live-data-feed-period-hours", 24));

        /// <summary>
        /// Defines the number of ticks produced per
        /// </summary>
        public int FastForward
        {
            get { return _fastForward;}
        }

        private DateTime _current = DateTime.Now;

        /// <summary>
        /// Creates a test live trading data feed with the specified fast forward factor
        /// </summary>
        /// <param name="algorithm">The algorithm under analysis</param>
        /// <param name="job">The job for the algorithm</param>
        /// <param name="tickResolution">The resolution of the tick data</param>
        public TestLiveTradingDataFeed(IAlgorithm algorithm, LiveNodePacket job, TimeSpan tickResolution = default(TimeSpan)) 
            : base(algorithm, job)
        {
            _start = _current;
            _tickResolution = tickResolution != default(TimeSpan) ? tickResolution : TimeSpan.FromMilliseconds(100);
        }

        public override IEnumerable<Tick> GetNextTicks()
        {
            for (int i = 0; i < _fastForward; i++)
            {
                yield return new Tick
                {
                    Time = (_current += _tickResolution),
                    Quantity = 10,
                    Value = ComputeNextSineValue(_start, _current, _period)
                };
            }
        }

        private static decimal ComputeNextSineValue(DateTime start, DateTime current, TimeSpan period)
        {
            double percentage = (current.Ticks - start.Ticks)/(double) period.Ticks;
            return (decimal)Math.Sin(2*Math.PI*percentage);
        }
    }
}