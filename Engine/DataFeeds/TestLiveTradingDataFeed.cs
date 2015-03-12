using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        private DateTime _current;
        private readonly DateTime _start;
        private readonly TimeSpan _tickResolution;
        private readonly int _fastForward = 5;
        private readonly TimeSpan _period = TimeSpan.FromHours(0.5);
        private readonly int _delay = 1;

        /// <summary>
        /// Defines the number of ticks produced per
        /// </summary>
        public int FastForward
        {
            get { return _fastForward; }
        }

        /// <summary>
        /// Creates a test live trading data feed with the specified fast forward factor
        /// </summary>
        /// <param name="algorithm">The algorithm under analysis</param>
        /// <param name="job">The job for the algorithm</param>
        public TestLiveTradingDataFeed(IAlgorithm algorithm, LiveNodePacket job)
            : base(algorithm, job)
        {
            _start = DateTime.Now;
            _current = DateTime.Now;
            _tickResolution = TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Implementation of Get Next Ticks for test framework.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Tick> GetNextTicks()
        {
            var ticks = new List<Tick>();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < _fastForward; i++)
            {
                _current += _tickResolution;
                var price = ComputeNextSineValue(_start, _current, _period);
                ticks.Add(new Tick
                {
                    Symbol = "EURUSD",
                    Time = _current,
                    Quantity = 10,
                    Value = price,
                    BidPrice = price * 0.99m,
                    AskPrice = price * 1.01m,
                    SaleCondition = "",
                    DataType = MarketDataType.Tick,
                    Exchange = "ABC",
                    Suspicious = false
                });
            }
            while (sw.ElapsedMilliseconds < _delay) Thread.Sleep(1);
            GC.Collect(3, GCCollectionMode.Forced, true);
            return ticks;
        }

        /// <summary>
        /// Calculate the next fake value for our fake data:
        /// </summary>
        /// <param name="start">Start of the fake data period</param>
        /// <param name="current">Current time for the fake data period</param>
        /// <param name="period">Period we want the sine to run over</param>
        /// <returns></returns>
        private decimal ComputeNextSineValue(DateTime start, DateTime current, TimeSpan period)
        {
            var percentage = ((current - start).TotalHours / period.TotalHours);

            return ((decimal)Math.Sin(percentage) * 100) + 1000;
        }
    }
}