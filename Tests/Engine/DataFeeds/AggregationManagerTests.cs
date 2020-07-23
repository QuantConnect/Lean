using NUnit.Framework;
using Oanda.RestV20.Model;
using QuantConnect.Brokerages.Bitfinex.Messages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DateTime = System.DateTime;
using Tick = QuantConnect.Data.Market.Tick;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class AggregationManagerTests
    {
        [Test]
        public void PassesTicksStraightThrough()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Minute);

            var count = 0;
            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(1), Symbols.SPY, 30, 30));
            aggregator.Update(new Tick(reference.AddSeconds(2), Symbols.SPY, 20, 20));

            Assert.AreEqual(count, 2);

            aggregator.Update(new Tick(reference.AddSeconds(3), Symbols.AAPL, 200, 200));
            Assert.AreEqual(count, 2);

            aggregator.Remove(config);
            aggregator.Update(new Tick(reference.AddSeconds(4), Symbols.SPY, 20, 20));
            Assert.AreEqual(count, 2);
        }

        [Test]
        public void BadTicksIgnored()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Minute);

            var count = 0;
            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(1), Symbols.AAPL, 200, 200));
            Assert.AreEqual(count, 0);
        }

        [Test]
        public void UnknownSubscriptionIgnored()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Minute);

            var count = 0;

            aggregator.Update(new Tick(reference.AddSeconds(1), Symbols.SPY, 30, 30));
            aggregator.Update(new Tick(reference.AddSeconds(2), Symbols.SPY, 20, 20));
            Assert.AreEqual(count, 0);

            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(3), Symbols.SPY, 200, 200));
            Assert.AreEqual(count, 1);

            aggregator.Remove(config);
            aggregator.Update(new Tick(reference.AddSeconds(4), Symbols.SPY, 20, 20));
            Assert.AreEqual(count, 1);
        }

        [Test]
        public void CanHandleMultipleSubscriptions()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;

            var symbols = new[] { Symbols.SPY, Symbols.AAPL, Symbols.USDJPY, Symbols.EURUSD };
            var total = 0;

            foreach (var symbol in symbols)
            {
                var enumerator = aggregator.Add(GetSubscriptionDataConfig<Tick>(symbol, Resolution.Minute), (s, e) => { });
                var cancelationToken = new CancellationTokenSource();
                total = 0;
                Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            while (enumerator.MoveNext() && !cancelationToken.IsCancellationRequested)
                            {
                                if (enumerator.Current != null)
                                {
                                    Interlocked.Increment(ref total);
                                }
                            }
                        }
                        finally
                        {
                            if (enumerator != null)
                            {
                                enumerator.Dispose();
                            }
                        }
                    },
                    cancelationToken.Token);

                for (int i = 0; i < 100; i++)
                {
                    aggregator.Update(new Tick(reference.AddSeconds(i), symbol, 20 + i, 20 + i));
                    Thread.Sleep(1);
                }
                Thread.Sleep(1000);
                cancelationToken.Cancel();
                Thread.Sleep(5000);
                Assert.AreEqual(100, total);
            }
        }

        [TestCase(typeof(TradeBar), TickType.Trade)]
        [TestCase(typeof(QuoteBar), TickType.Quote)]
        public void CanHandleBars(Type type, TickType tickType)
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var total = 0;
            var enumerator = aggregator.Add(GetSubscriptionDataConfig(type, Symbols.EURUSD, Resolution.Tick), (s, e) => { });
            var cancelationToken = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        while (enumerator.MoveNext() && !cancelationToken.IsCancellationRequested)
                        {
                            if (enumerator.Current != null)
                            {
                                Assert.IsTrue(enumerator.Current.GetType() == type);
                                Interlocked.Increment(ref total);
                            }
                        }
                    }
                    finally
                    {
                        if (enumerator != null)
                        {
                            enumerator.Dispose();
                        }
                    }
                },
                cancelationToken.Token);

            for (int i = 0; i < 100; i++)
            {
                aggregator.Update(new Tick(reference.AddSeconds(i), Symbols.EURUSD, 20 + i, 20 + i) { TickType = tickType });
                Thread.Sleep(1);
            }
            cancelationToken.Cancel();
            Assert.AreEqual(100, total);
        }

        private IDataAggregator GetDataAggregator()
        {
            return new TestAggregationManager(new ManualTimeProvider(DateTime.Today));
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return GetSubscriptionDataConfig(typeof(T), symbol, resolution);
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig(Type T, Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                T,
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        private class TestAggregationManager : AggregationManager
        {
            public TestAggregationManager(ITimeProvider timeProvider)
            {
                TimeProvider = timeProvider;
            }
        }
    }
}
