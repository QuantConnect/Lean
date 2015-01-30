using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;

namespace QuantConnect.Queues.Test
{
    /// <summary>
    /// Provides a fake queue handler to provider live data to run tests locally
    /// </summary>
    public class FakeLiveQueueHandler : Queue
    {
        private readonly Random _random = new Random();
        private readonly int _tickCount = Config.GetInt("fake-live-queue-handler-tick-count", 1000);
        private readonly int _createDataGapsEvery = Config.GetInt("fake-live-queue-handler-create-data-gaps-every-n-subscriptions", 2);
        private readonly TimeSpan _maxDataGap = TimeSpan.FromMinutes(Config.GetDouble("fake-live-queue-handler-max-gap-minutes", 1));
        private static readonly int TimesPerSecond = Config.GetInt("fake-live-queue-handler-times-per-second", 1);
        private static readonly DateTime _start = DateTime.Now;
        private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();
        private readonly Dictionary<string, DateTime> _lastDataTimes = new Dictionary<string, DateTime>(); 
        public override IEnumerable<Tick> GetNextTicks()
        {
            Thread.Sleep((int) (1000/(double) TimesPerSecond));

            // spread them out
            for (int i = 0; i < _tickCount; i++)
            {
                foreach (var subscription in _subscriptions)
                {
                    if (_random.NextDouble() < 0.0001)
                    //if (_lastDataTimes[subscription.Symbol].Add(subscription.DataInterval) <= DateTime.Now)
                    {
                        _lastDataTimes[subscription.Symbol] = DateTime.Now;
                        var sine = ComputeNextSineValue(_start, DateTime.Now, TimeSpan.FromMinutes(1));
                        yield return new Tick(DateTime.Now, subscription.Symbol, sine*1.025m, sine*.975m);
                    }
                }
            }
        }

        public override void Subscribe(IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var type in symbols)
            {
                foreach (var symbol in type.Value)
                {
                    TimeSpan gap = TimeSpan.Zero;
                    if (_subscriptions.Count%_createDataGapsEvery == 0)
                    {
                        double value = _random.NextDouble();
                        double milliseconds = _maxDataGap.TotalMilliseconds*value;
                        gap = TimeSpan.FromMilliseconds(milliseconds);
                    }
                    Console.WriteLine("SYMBOL: " + symbol + " GAP: " + gap.TotalSeconds.ToString("0.00"));
                    _subscriptions.Add(new Subscription(symbol, gap));
                    _lastDataTimes.Add(symbol, DateTime.MinValue);
                }
            }
        }

        public override void Unsubscribe(IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var type in symbols)
            {
                foreach (var symbol in type.Value)
                {
                    _subscriptions.Remove(new Subscription(symbol, TimeSpan.Zero));
                }
            }
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
            var percentage = ((current - start).TotalHours/period.TotalHours);

            return ((decimal) Math.Sin(percentage)*100) + 1000;
        }

        private class Subscription
        {
            public readonly TimeSpan DataInterval;
            public readonly string Symbol;

            public Subscription(string symbol, TimeSpan dataInterval)
            {
                Symbol = symbol.ToUpper();
                DataInterval = dataInterval;
            }

            public override int GetHashCode()
            {
                return Symbol.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                return ((Subscription) obj).Symbol.ToUpper() == Symbol;
            }
        }
    }
}
