using System;
using NUnit.Framework;
using QuantConnect.Brokerages.Zerodha;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Zerodha
{
    [TestFixture, Ignore("This test requires a configured and active Zerodha account")]
    public class ZerodhaBrokerageHistoryProviderTests
    {
            private static TestCaseData[] TestParameters
            {
                get
                {
                    return new[]
                    {
                    // valid parameters
                    new TestCaseData(Symbols.SBIN, Resolution.Tick, Time.OneMinute, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Daily, TimeSpan.FromDays(15), false),

                    // invalid period, throws "System.ArgumentException : Invalid date range specified"
                    new TestCaseData(Symbols.SBIN, Resolution.Daily, TimeSpan.FromDays(-15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Forex"
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), true)
                };
                }
            }

            [Test, TestCaseSource(nameof(TestParameters))]
            public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
            {
                TestDelegate test = () =>
                {
                    var accessToken = Config.Get("zerodha-access-token");
                    var apiKey = Config.Get("zerodha-api-key");
                    var brokerage = new ZerodhaBrokerage(apiKey, accessToken,null,null);

                    var now = DateTime.UtcNow;

                    var requests = new[]
                    {
                    new HistoryRequest(now.Add(-period),
                        now,
                        typeof(QuoteBar),
                        symbol,
                        resolution,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.Kolkata),
                        TimeZones.Kolkata,
                        Resolution.Minute,
                        false,
                        false,
                        DataNormalizationMode.Adjusted,
                        TickType.Quote)
                };

                    var history = brokerage.GetHistory(requests, TimeZones.Utc);

                    foreach (var slice in history)
                    {
                        if (resolution == Resolution.Tick)
                        {
                            foreach (var tick in slice.Ticks[symbol])
                            {
                                Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol, tick.BidPrice, tick.AskPrice);
                            }
                        }
                        else
                        {
                            var bar = slice.Bars[symbol];

                            Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}, V={6}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                        }
                    }

                    Log.Trace("Data points retrieved: " + brokerage.DataPointCount);
                };

                if (throwsException)
                {
                    Assert.Throws<ArgumentException>(test);
                }
                else
                {
                    Assert.DoesNotThrow(test);
                }
            }
            }
}
