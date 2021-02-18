/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public partial class PandasConverterTests
    {
        private QCAlgorithm _algorithm;
        private ZipDataCacheProvider _cacheProvider;

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Tick)]
        public void EquityTAQSingleSymbol(Resolution resolution)
        {
            AssertEquity(new[] { Symbols.IBM }, resolution);
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Tick)]
        public void EquityTAQMultipleSymbols(Resolution resolution)
        {
            AssertEquity(new[]
            {
                Symbols.IBM,
                Symbols.SPY,
                Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                Symbol.Create("AIG", SecurityType.Equity, Market.USA)
            }, resolution);
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Tick)]
        public void ForexSingleSymbol(Resolution resolution)
        {
            AssertForex(new[] { Symbols.EURUSD }, resolution, new DateTime(2014, 5, 1), new DateTime(2014, 5, 5));
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Tick)]
        public void ForexMultipleSymbols(Resolution resolution)
        {
            AssertForex(new[]
            {
                Symbols.EURUSD,
                Symbol.Create("NZDUSD", SecurityType.Forex, Market.Oanda)
            }, resolution, new DateTime(2014, 5, 1), new DateTime(2014, 5, 5));
        }

        [TestCase(Resolution.Minute)]
        public void FuturesSingleSymbol(Resolution resolution)
        {
            AssertFuture(new[]
            {
                Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2013, 12, 20))
            }, resolution);
        }

        [TestCase(Resolution.Minute)]
        public void FuturesMultipleSymbols(Resolution resolution)
        {
            AssertFuture(new[]
            {
                Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2013, 12, 20)),
                Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2014, 3, 21)),
                Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2014, 6, 20)),
            }, resolution);
        }

        [TestCase(Resolution.Minute)]
        public void OptionSingleSymbol(Resolution resolution)
        {
            AssertOption(new[]
            {
                Symbol.CreateOption(
                    Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    650m,
                    new DateTime(2014, 6, 13))
            }, resolution, new DateTime(2014, 6, 6), new DateTime(2014, 6, 7));
        }

        [TestCase(Resolution.Minute)]
        public void OptionMultipleSymbols(Resolution resolution)
        {
            AssertOption(new[]
            {
                Symbol.CreateOption(
                    Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    650m,
                    new DateTime(2014, 6, 6)),
                Symbol.CreateOption(
                    Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Put,
                    650m,
                    new DateTime(2014, 6, 6)),
                Symbol.CreateOption(
                    Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    660m,
                    new DateTime(2014, 6, 13)),
                Symbol.CreateOption(
                    Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Put,
                    660m,
                    new DateTime(2014, 6, 13))
            }, resolution, new DateTime(2014, 6, 6), new DateTime(2014, 6, 7));
        }

        [Test]
        public void OpenInterestOnlyOption()
        {
            // This test ensures that the DataFrame created with only
            // open interest data has more than the two default columns: "symbol", "time".
            AssertOption(new []
            {
                Symbol.CreateOption(
                    Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    750,
                    new DateTime(2014, 10, 18))
            }, Resolution.Minute, new DateTime(2014, 6, 9), new DateTime(2014, 6, 10), openInterestOnly: true);
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Minute)]
        public void CryptoSingleSymbol(Resolution resolution)
        {
            AssertCrypto(new[] { Symbols.BTCUSD }, resolution, new DateTime(2018, 4, 4), new DateTime(2018, 4, 6));
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Minute)]
        public void CryptoMultipleSymbols(Resolution resolution)
        {
            var symbols = resolution == Resolution.Daily ? new[] { Symbols.BTCUSD } : new[]
            {
                Symbols.BTCUSD,
                Symbols.BTCEUR,
                Symbols.ETHUSD
            };

            AssertCrypto(symbols, resolution, new DateTime(2018, 4, 4), new DateTime(2018, 4, 6));
        }

        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Tick)]
        public void CfdSingleSymbol(Resolution resolution)
        {
            AssertForex(new[]
            {
                Symbol.Create("XAUUSD", SecurityType.Cfd, Market.Oanda)
            }, resolution, new DateTime(2014, 5, 1), new DateTime(2014, 5, 4));
        }

        private void AssertEquity(IEnumerable<Symbol> symbols, Resolution resolution, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (Py.GIL())
            {
                // History request for Tick data with multiple Symbols is the bottleneck when
                // it comes to the EquityTAQMultipleSymbols test. Slice.Keys is also extremely
                // slow, but must be iterated in order to grab all Symbols from the Slice, including
                // custom data Symbols.
                var history = History(
                    symbols,
                    startDate ?? new DateTime(2013, 10, 7),
                    endDate ?? (resolution != Resolution.Tick ? new DateTime(2013, 10, 11) : new DateTime(2013, 10, 8)),
                    resolution
                ).ToList();

                var tbColumns = new[] { "open", "high", "low", "close", "volume" };
                var qbColumns = new[] {
                    "bidopen", "bidhigh", "bidlow", "bidclose", "bidsize",
                    "askopen", "askhigh", "asklow", "askclose", "asksize"
                };
                // The "suspicious" column is excluded because all of its values are currently false.
                // It would be sandwiched between the "exchange" and "lastprice" columns.
                var tickColumns = new[] { "exchange", "lastprice", "quantity", "bidprice", "bidsize", "askprice", "asksize" };

                Assert.Greater(history.Count, 0);

                dynamic df = _algorithm.PandasConverter.GetDataFrame(history);
                var tbs = history.SelectMany(x => x.Bars.Values.Select(y => (BaseData)y)).ToList();
                var qbs = history.SelectMany(x => x.QuoteBars.Values.Select(y => (BaseData)y)).ToList();
                var taqDataPoints = tbs.Concat(qbs).GroupBy(x => x.EndTime).Sum(kvp => kvp.GroupBy(x => x.Symbol).Count());

                var tickLength = history.AsParallel().Select(x => x.Ticks.Values.Sum(y => y.Count)).Sum();
                var dataPointsCount = taqDataPoints + tickLength;

                Console.WriteLine($"dpts: {dataPointsCount}");
                Assert.AreEqual(dataPointsCount, df.__len__().AsManagedObject(typeof(int)));

                var pandasColumns = (string[])df.columns.AsManagedObject(typeof(string[]));

                if (resolution == Resolution.Daily || resolution == Resolution.Hour)
                {
                    Assert.IsTrue(tbColumns.SequenceEqual(pandasColumns));
                }
                else if (resolution == Resolution.Minute || resolution == Resolution.Second)
                {
                    Assert.IsTrue(tbColumns.Concat(qbColumns).SequenceEqual(pandasColumns));
                }
                else
                {
                    Assert.IsTrue(tickColumns.SequenceEqual(pandasColumns));
                }

                var pandasIndexes = (string[])df.index.names.AsManagedObject(typeof(string[]));
                Assert.IsTrue(new[] { "symbol", "time" }.SequenceEqual(pandasIndexes));
                Assert.DoesNotThrow(() =>
                {
                    var locals = new PyDict();
                    locals.SetItem("df", df);

                    foreach (var symbol in symbols)
                    {
                        PythonEngine.Eval($"df.loc[\"{symbol.ID}\"]", null, locals.Handle);
                    }
                });
            }
        }

        private void AssertForex(IEnumerable<Symbol> symbols, Resolution resolution, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (Py.GIL())
            {
                var history = History(
                    symbols,
                    startDate ?? new DateTime(2013, 10, 7),
                    endDate ?? (resolution != Resolution.Tick ? new DateTime(2013, 10, 11) : new DateTime(2013, 10, 8)),
                    resolution
                ).ToList();

                var qbColumns = new[] {
                    // When we have no Trades, we set "OHLC" to the QuoteBar's OHLC properties.
                    // We expect the "volume" column to be dropped, as it will be full of NaN values.
                    // Quotes only data will not have BidSize/AskSize
                    "open", "high", "low", "close",
                    "bidopen", "bidhigh", "bidlow", "bidclose",
                    "askopen", "askhigh", "asklow", "askclose"
                };

                // The "suspicious" and "exchange" columns are excluded because all of its values are filtered out.
                var tickColumns = new[]
                {
                    "lastprice",
                    "bidprice",
                    "askprice"
                };

                Assert.Greater(history.Count, 0);

                dynamic df = _algorithm.PandasConverter.GetDataFrame(history);
                var qbs = history.SelectMany(x => x.QuoteBars.Values.Select(y => (BaseData)y)).ToList();
                var taqDataPoints = qbs.GroupBy(x => x.EndTime).Sum(kvp => kvp.GroupBy(x => x.Symbol).Count());

                var tickLength = history.AsParallel().Select(x => x.Ticks.Values.Sum(y => y.Count)).Sum();
                var dataPointsCount = taqDataPoints + tickLength;

                Console.WriteLine($"dpts: {dataPointsCount}");

                var pandasColumns = (string[])df.columns.AsManagedObject(typeof(string[]));

                Assert.AreEqual(dataPointsCount, df.__len__().AsManagedObject(typeof(int)));

                if (resolution != Resolution.Tick)
                {
                    Assert.IsTrue(qbColumns.SequenceEqual(pandasColumns));
                }
                else
                {
                    Assert.IsTrue(tickColumns.SequenceEqual(pandasColumns));
                }

                var pandasIndexes = (string[])df.index.names.AsManagedObject(typeof(string[]));
                Assert.IsTrue(new[] { "symbol", "time" }.SequenceEqual(pandasIndexes));

                Assert.DoesNotThrow(() =>
                {
                    var locals = new PyDict();
                    locals.SetItem("df", df);

                    foreach (var symbol in symbols)
                    {
                        PythonEngine.Eval($"df.loc[\"{symbol.ID}\"]", null, locals.Handle);
                    }
                });
            }
        }

        private void AssertFuture(IEnumerable<Symbol> symbols, Resolution resolution, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (Py.GIL())
            {
                var history = History(
                    symbols,
                    startDate ?? new DateTime(2013, 10, 7),
                    endDate ?? (resolution != Resolution.Tick ? new DateTime(2013, 10, 11) : new DateTime(2013, 10, 8)),
                    resolution
                ).ToList();

                var futureColumns = new[] {
                    "open", "high", "low", "close", "volume",
                    "bidopen", "bidhigh", "bidlow", "bidclose", "bidsize",
                    "askopen", "askhigh", "asklow", "askclose", "asksize",
                    "openinterest"
                };

                // The "suspicious" and "exchange" columns are excluded because all of its values are filtered out.
                var tickColumns = new[]
                {
                    "lastprice", "quantity",
                    "bidprice", "bidsize",
                    "askprice", "asksize",
                    "openinterest"
                };

                Assert.Greater(history.Count, 0);

                dynamic df = _algorithm.PandasConverter.GetDataFrame(history);
                var tbs = history.SelectMany(x => x.Bars.Values.Select(y => (BaseData)y)).ToList();
                var qbs = history.SelectMany(x => x.QuoteBars.Values.Select(y => (BaseData)y)).ToList();
                var oi = history.SelectMany(x => x.Ticks.Values.Where(t => t is OpenInterest).SelectMany(t => t)).ToList();
                var tbQbOiDataPoints = tbs.Concat(qbs).Concat(oi).GroupBy(x => x.EndTime).Sum(kvp => kvp.GroupBy(x => x.Symbol).Count());

                var tickLength = history.AsParallel().Sum(x => x.Ticks.Values.SelectMany(t => t).Count(t => !(t is OpenInterest)));
                var dataPointsCount = tbQbOiDataPoints + tickLength;

                Console.WriteLine($"dpts: {dataPointsCount}");
                Assert.AreEqual(dataPointsCount, df.__len__().AsManagedObject(typeof(int)));

                var pandasColumns = (string[])df.columns.AsManagedObject(typeof(string[]));
                if (resolution != Resolution.Tick)
                {
                    Assert.IsTrue(futureColumns.SequenceEqual(pandasColumns));
                }
                else
                {
                    Assert.IsTrue(tickColumns.SequenceEqual(pandasColumns));
                }

                var pandasIndexes = (string[])df.index.names.AsManagedObject(typeof(string[]));
                Assert.IsTrue(new[] { "expiry", "symbol", "time" }.SequenceEqual(pandasIndexes));
            }
        }

        private void AssertOption(IEnumerable<Symbol> symbols, Resolution resolution, DateTime? startDate = null, DateTime? endDate = null, bool openInterestOnly = false)
        {
            using (Py.GIL())
            {
                var history = History(
                    symbols,
                    startDate ?? new DateTime(2013, 10, 7),
                    endDate ?? (resolution != Resolution.Tick ? new DateTime(2013, 10, 11) : new DateTime(2013, 10, 8)),
                    resolution
                ).ToList();

                var optionColumns = new[] {
                    "open", "high", "low", "close", "volume",
                    "bidopen", "bidhigh", "bidlow", "bidclose", "bidsize",
                    "askopen", "askhigh", "asklow", "askclose", "asksize",
                    "openinterest"
                };

                Assert.Greater(history.Count, 0);

                dynamic df = _algorithm.PandasConverter.GetDataFrame(history);
                var tbs = history.SelectMany(x => x.Bars.Values.Select(y => (BaseData)y)).ToList();
                var qbs = history.SelectMany(x => x.QuoteBars.Values.Select(y => (BaseData)y)).ToList();
                var oi = history.SelectMany(x => x.Ticks.Values.Select(ticks => ticks.Where(t => t is OpenInterest)).SelectMany(t => t)).ToList();
                var dataPointsCount = tbs.Concat(qbs).Concat(oi).GroupBy(x => x.EndTime).Sum(kvp => kvp.GroupBy(x => x.Symbol).Count());

                Assert.AreEqual(dataPointsCount, df.__len__().AsManagedObject(typeof(int)));

                var pandasColumns = (string[])df.columns.AsManagedObject(typeof(string[]));
                if (openInterestOnly)
                {
                    Assert.IsTrue(new[] { "openinterest" }.SequenceEqual(pandasColumns));
                }
                else
                {
                    Assert.IsTrue(optionColumns.SequenceEqual(pandasColumns));
                }


                var pandasIndexes = (string[])df.index.names.AsManagedObject(typeof(string[]));
                Assert.IsTrue(new[] { "expiry", "strike", "type", "symbol", "time" }.SequenceEqual(pandasIndexes));

                Assert.DoesNotThrow(() =>
                {
                    var locals = new PyDict();
                    locals.SetItem("datetime", Py.Import("datetime"));
                    locals.SetItem("df", df);
                    foreach (var symbol in symbols)
                    {
                        PythonEngine.Eval($"df.loc[datetime.datetime({symbol.ID.Date.Year}, {symbol.ID.Date.Month}, {symbol.ID.Date.Day})].loc[{symbol.ID.StrikePrice.ToStringInvariant()}].loc['{symbol.ID.OptionRight.ToString()}'].loc['{symbol.ID.ToString()}']", null, locals.Handle);
                    }
                });
            }
        }

        private void AssertCrypto(IEnumerable<Symbol> symbols, Resolution resolution, DateTime startDate, DateTime endDate)
        {
            using (Py.GIL())
            {
                // History request for Tick data with multiple Symbols is the bottleneck when
                // it comes to the EquityTAQMultipleSymbols test. Slice.Keys is also extremely
                // slow, but must be iterated in order to grab all Symbols from the Slice, including
                // custom data Symbols.
                var history = History(
                    symbols,
                    startDate,
                    endDate,
                    resolution
                ).ToList();

                var cryptoColumns = new[] {
                    "open", "high", "low", "close", "volume",
                    "bidopen", "bidhigh", "bidlow", "bidclose", "bidsize",
                    "askopen", "askhigh", "asklow", "askclose", "asksize"
                };

                // The "suspicious" column is excluded because all of its values are currently false.
                // It would be sandwiched between the "exchange" and "lastprice" columns.
                var tickColumns = new[] { "exchange", "lastprice", "quantity", "bidprice", "bidsize", "askprice", "asksize" };

                Assert.Greater(history.Count, 0);

                dynamic df = _algorithm.PandasConverter.GetDataFrame(history);
                var tbs = history.SelectMany(x => x.Bars.Values.Select(y => (BaseData)y)).ToList();
                var qbs = history.SelectMany(x => x.QuoteBars.Values.Select(y => (BaseData)y)).ToList();
                var taqDataPoints = tbs.Concat(qbs).GroupBy(x => x.EndTime).Sum(kvp => kvp.GroupBy(x => x.Symbol).Count());

                var tickLength = history.AsParallel().Select(x => x.Ticks.Values.Sum(y => y.Count)).Sum();
                var dataPointsCount = taqDataPoints + tickLength;

                Console.WriteLine($"dpts: {dataPointsCount}");
                Assert.AreEqual(dataPointsCount, df.__len__().AsManagedObject(typeof(int)));

                var pandasColumns = (string[])df.columns.AsManagedObject(typeof(string[]));
                if (resolution != Resolution.Tick)
                {
                    Assert.IsTrue(cryptoColumns.SequenceEqual(pandasColumns));
                }
                else
                {
                    Assert.IsTrue(tickColumns.SequenceEqual(pandasColumns));
                }

                var pandasIndexes = (string[])df.index.names.AsManagedObject(typeof(string[]));
                Assert.IsTrue(new[] { "symbol", "time" }.SequenceEqual(pandasIndexes));

                Assert.DoesNotThrow(() =>
                {
                    var locals = new PyDict();
                    locals.SetItem("df", df);

                    foreach (var symbol in symbols)
                    {
                        PythonEngine.Eval($"df.loc[\"{symbol.ID}\"]", null, locals.Handle);
                    }
                });
            }
        }

        private IEnumerable<Slice> History(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution resolution)
        {
            if (_algorithm != null)
            {
                return _algorithm.History(symbols, start, end, resolution);
            }

            _algorithm = new QCAlgorithm();
            _algorithm.SetPandasConverter();
            var dataFeed = new NullDataFeed();

            _algorithm.SubscriptionManager = new SubscriptionManager();
            _algorithm.SubscriptionManager.SetDataManager(new DataManager(
                dataFeed,
                new UniverseSelection(
                    _algorithm,
                    new SecurityService(
                        new CashBook(),
                        MarketHoursDatabase.FromDataFolder(),
                        SymbolPropertiesDatabase.FromDataFolder(),
                        _algorithm,
                        null,
                        null
                    ),
                    new DataPermissionManager(),
                    new DefaultDataProvider()
                ),
                _algorithm,
                new TimeKeeper(DateTime.UtcNow),
                MarketHoursDatabase.FromDataFolder(),
                false,
                null,
                new DataPermissionManager()
            ));

            _cacheProvider = new ZipDataCacheProvider(new DefaultDataProvider());
            _algorithm.HistoryProvider = new SubscriptionDataReaderHistoryProvider();
            _algorithm.HistoryProvider.Initialize(
                new HistoryProviderInitializeParameters(
                    null,
                    null,
                    null,
                    _cacheProvider,
                    new LocalDiskMapFileProvider(),
                    new LocalDiskFactorFileProvider(),
                    (_) => {},
                    false,
                    new DataPermissionManager()));

            _algorithm.SetStartDate(DateTime.UtcNow.AddDays(-1));

            return _algorithm.History(symbols, start, end, resolution);
        }

        [TearDown]
        public void Dispose()
        {
            _cacheProvider?.Dispose();
        }
    }
}
