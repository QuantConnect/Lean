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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Securities;
using QuantConnect.ToolBox;
using QuantConnect.Util;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class LeanDataReaderTests
    {
        string _dataDirectory = "../../../Data/";
        DateTime _fromDate = new DateTime(2013, 10, 7);
        DateTime _toDate = new DateTime(2013, 10, 11);

        [Test, Parallelizable(ParallelScope.Self)]
        public void LoadsEquity_Daily_SingleEntryZip()
        {
            var dataPath = LeanData.GenerateZipFilePath(Globals.DataFolder, Symbols.AAPL, DateTime.UtcNow, Resolution.Daily, TickType.Trade);
            var leanDataReader = new LeanDataReader(dataPath);
            var data = leanDataReader.Parse().ToList();

            Assert.AreEqual(5849, data.Count);
            Assert.IsTrue(data.All(baseData => baseData.Symbol == Symbols.AAPL && baseData is TradeBar));
        }

        #region futures

        [Test, Parallelizable(ParallelScope.Self)]
        public void ReadsEntireZipFileEntries_OpenInterest()
        {
            var baseFuture = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, SecurityIdentifier.DefaultDate);
            var filePath = LeanData.GenerateZipFilePath(Globals.DataFolder, baseFuture, new DateTime(2013, 10, 06), Resolution.Minute, TickType.OpenInterest);
            var leanDataReader = new LeanDataReader(filePath);

            var data = leanDataReader.Parse()
                .ToList()
                .GroupBy(baseData => baseData.Symbol)
                .Select(grp => grp.ToList())
                .OrderBy(list => list[0].Symbol)
                .ToList();

            Assert.AreEqual(5, data.Count);
            Assert.IsTrue(data.All(kvp => kvp.Count == 1));

            foreach (var dataForSymbol in data)
            {
                Assert.IsTrue(dataForSymbol[0] is OpenInterest);
                Assert.IsFalse(dataForSymbol[0].Symbol.IsCanonical());
                Assert.AreEqual(Futures.Indices.SP500EMini, dataForSymbol[0].Symbol.ID.Symbol);
                Assert.AreNotEqual(0, dataForSymbol[0]);
            }
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void ReadsEntireZipFileEntries_Trade()
        {
            var baseFuture = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, SecurityIdentifier.DefaultDate);
            var filePath = LeanData.GenerateZipFilePath(Globals.DataFolder, baseFuture, new DateTime(2013, 10, 06), Resolution.Minute, TickType.Trade);
            var leanDataReader = new LeanDataReader(filePath);

            var data = leanDataReader.Parse()
                .ToList()
                .GroupBy(baseData => baseData.Symbol)
                .Select(grp => grp.ToList())
                .OrderBy(list => list[0].Symbol)
                .ToList();

            Assert.AreEqual(2, data.Count);

            foreach (var dataForSymbol in data)
            {
                Assert.IsTrue(dataForSymbol[0] is TradeBar);
                Assert.IsFalse(dataForSymbol[0].Symbol.IsCanonical());
                Assert.AreEqual(Futures.Indices.SP500EMini, dataForSymbol[0].Symbol.ID.Symbol);
            }

            Assert.AreEqual(118, data[0].Count);
            Assert.AreEqual(10, data[1].Count);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void ReadsEntireZipFileEntries_Quote()
        {
            var baseFuture = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, SecurityIdentifier.DefaultDate);
            var filePath = LeanData.GenerateZipFilePath(Globals.DataFolder, baseFuture, new DateTime(2013, 10, 06), Resolution.Minute, TickType.Quote);
            var leanDataReader = new LeanDataReader(filePath);

            var data = leanDataReader.Parse()
                .ToList()
                .GroupBy(baseData => baseData.Symbol)
                .Select(grp => grp.ToList())
                .OrderBy(list => list[0].Symbol)
                .ToList();

            Assert.AreEqual(5, data.Count);

            foreach (var dataForSymbol in data)
            {
                Assert.IsTrue(dataForSymbol[0] is QuoteBar);
                Assert.IsFalse(dataForSymbol[0].Symbol.IsCanonical());
                Assert.AreEqual(Futures.Indices.SP500EMini, dataForSymbol[0].Symbol.ID.Symbol);
            }

            Assert.AreEqual(10, data[0].Count);
            Assert.AreEqual(13, data[1].Count);
            Assert.AreEqual(52, data[2].Count);
            Assert.AreEqual(155, data[3].Count);
            Assert.AreEqual(100, data[4].Count);
        }

        [Test]
        public void ReadFutureChainData()
        {
            var canonicalFutures = new Dictionary<Symbol, string>()
            {
                { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME),
                    "ES20Z13|ES21H14|ES20M14|ES19U14|ES19Z14" },
                {Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX),
                    "GC29V13|GC26X13|GC27Z13|GC26G14|GC28J14|GC26M14|GC27Q14|GC29V14|GC29Z14|GC25G15|GC28J15|GC26M15|GC27Q15|GC29Z15|GC28M16|GC28Z16|GC28M17|GC27Z17|GC27M18|GC27Z18|GC26M19"},
            };

            var tickTypes = new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest };

            var resolutions = new[] { Resolution.Minute };


            foreach (var canonical in canonicalFutures)
            {
                foreach (var res in resolutions)
                {
                    foreach (var tickType in tickTypes)
                    {
                        var futures = LoadFutureChain(canonical.Key, _fromDate, tickType, res);

                        string chain = string.Join("|", futures.Select(f => f.Value));

                        if (tickType == TickType.Quote) //only quotes have the full chain!
                            Assert.AreEqual(canonical.Value, chain);

                        foreach (var future in futures)
                        {
                            string csv = LoadFutureData(future, tickType, res);
                            Assert.IsTrue(!string.IsNullOrEmpty(csv));
                        }
                    }
                }
            }
        }

        private List<Symbol> LoadFutureChain(Symbol baseFuture, DateTime date, TickType tickType, Resolution res)
        {
            var filePath = LeanData.GenerateZipFilePath(_dataDirectory, baseFuture, date, res, tickType);

            //load future chain first
            var config = new SubscriptionDataConfig(typeof(ZipEntryName), baseFuture, res,
                                                    TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, tickType);
            var factory = new ZipEntryNameSubscriptionDataSourceReader(TestGlobals.DataCacheProvider, config, date, false);

            var result = factory.Read(new SubscriptionDataSource(filePath, SubscriptionTransportMedium.LocalFile, FileFormat.ZipEntryName))
                          .Select(s => s.Symbol).ToList();
            return result;
        }

        private string LoadFutureData(Symbol future, TickType tickType, Resolution res)
        {
            var dataType = LeanData.GetDataType(res, tickType);
            var config = new SubscriptionDataConfig(dataType, future, res,
                                                    TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, tickType);

            var date = _fromDate;

            var sb = new StringBuilder();

            while (date <= _toDate)
            {
                var leanDataReader = new LeanDataReader(config, future, res, date, _dataDirectory);

                foreach (var bar in leanDataReader.Parse())
                {
                    //write base data type back to string
                    sb.AppendLine(LeanData.GenerateLine(bar, SecurityType.Future, res));
                }
                date = date.AddDays(1);
            }
            var csv = sb.ToString();
            return csv;
        }

        [Test]
        public void GenerateDailyAndHourlyFutureDataFromMinutes()
        {

            var tickTypes = new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest };

            var futures = new[] { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME),
                Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX)};
            var resolutions = new[] { Resolution.Hour, Resolution.Daily };

            foreach (var future in futures)
                foreach (var res in resolutions)
                    foreach (var tickType in tickTypes)
                        ConvertMinuteFuturesData(future, tickType, res);
        }

        private void ConvertMinuteFuturesData(Symbol canonical, TickType tickType, Resolution outputResolution, Resolution inputResolution = Resolution.Minute)
        {

            var timeSpans = new Dictionary<Resolution, TimeSpan>()
            {
                { Resolution.Daily, TimeSpan.FromHours(24)},
                { Resolution.Hour, TimeSpan.FromHours(1)},
            };

            var timeSpan = timeSpans[outputResolution];

            var tickTypeConsolidatorMap = new Dictionary<TickType, Func<IDataConsolidator>>()
            {
                {TickType.Quote, () => new QuoteBarConsolidator(timeSpan)},
                {TickType.OpenInterest, ()=> new OpenInterestConsolidator(timeSpan)},
                {TickType.Trade, ()=> new TradeBarConsolidator(timeSpan) }

            };

            var consolidators = new Dictionary<string, IDataConsolidator>();
            var configs = new Dictionary<string, SubscriptionDataConfig>();
            var outputFiles = new Dictionary<string, StringBuilder>();
            var futures = new Dictionary<string, Symbol>();

            var date = _fromDate;
            while (date <= _toDate)
            {
                var futureChain = LoadFutureChain(canonical, date, tickType, inputResolution);

                foreach (var future in futureChain)
                {
                    if (!futures.ContainsKey(future.Value))
                    {
                        futures[future.Value] = future;
                        var config = new SubscriptionDataConfig(LeanData.GetDataType(outputResolution, tickType),
                                                                future, inputResolution, TimeZones.NewYork, TimeZones.NewYork,
                                                                false, false, false, false, tickType);
                        configs[future.Value] = config;

                        consolidators[future.Value] = tickTypeConsolidatorMap[tickType].Invoke();

                        var sb = new StringBuilder();
                        outputFiles[future.Value] = sb;

                        consolidators[future.Value].DataConsolidated += (sender, bar) =>
                        {
                            sb.Append(LeanData.GenerateLine(bar, SecurityType.Future, outputResolution) + Environment.NewLine);
                        };
                    }

                    var leanDataReader = new LeanDataReader(configs[future.Value], future, inputResolution, date, _dataDirectory);

                    var consolidator = consolidators[future.Value];

                    foreach (var bar in leanDataReader.Parse())
                    {
                        consolidator.Update(bar);
                    }
                }
                date = date.AddDays(1);
            }

            //write all results
            foreach (var consolidator in consolidators.Values)
                consolidator.Scan(date);

            var zip = LeanData.GenerateRelativeZipFilePath(canonical, _fromDate, outputResolution, tickType);
            var zipPath = Path.Combine(_dataDirectory, zip);
            var fi = new FileInfo(zipPath);

            if (!fi.Directory.Exists)
                fi.Directory.Create();

            foreach (var future in futures.Values)
            {
                var zipEntry = LeanData.GenerateZipEntryName(future, _fromDate, outputResolution, tickType);
                var sb = outputFiles[future.Value];

                //Uncomment to write zip files
                //QuantConnect.Compression.ZipCreateAppendData(zipPath, zipEntry, sb.ToString());

                Assert.IsTrue(sb.Length > 0);
            }
        }

        #endregion


        [Test, TestCaseSource(nameof(OptionAndFuturesCases))]
        public void ReadLeanFutureAndOptionDataFromFilePath(string composedFilePath, Symbol symbol, int rowsInfile, double sumValue)
        {
            // Act
            var ldr = new LeanDataReader(composedFilePath);
            var data = ldr.Parse().ToList();
            // Assert
            Assert.True(symbol.Equals(data.First().Symbol));
            Assert.AreEqual(rowsInfile, data.Count);
            Assert.AreEqual(sumValue, data.Sum(c => c.Value));
        }


        public static object[] OptionAndFuturesCases =
        {
            new object[]
            {
                "../../../Data/future/cme/minute/es/20131008_quote.zip#20131008_es_minute_quote_201312_20131220.csv",
                LeanData
                    .ReadSymbolFromZipEntry(Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME),
                                            Resolution.Minute, "20131008_es_minute_quote_201312_20131220.csv"),
                1411,
                2346061.875
            },

            new object[]
            {
                "../../../Data/future/comex/minute/gc/20131010_trade.zip#20131010_gc_minute_trade_201312_20131227.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX),
                                                Resolution.Minute, "20131010_gc_minute_trade_201312_20131227.csv"),
                1379,
                1791800.9
            },

            new object[]
            {
                "../../../Data/future/comex/tick/gc/20131009_quote.zip#20131009_gc_tick_quote_201406_20140626.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX),
                                                Resolution.Tick, "20131009_gc_tick_quote_201406_20140626.csv"),
                197839,
                259245064.8
            },

            new object[]
            {
                "../../../Data/future/comex/tick/gc/20131009_trade.zip#20131009_gc_tick_trade_201312_20131227.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX),
                                                Resolution.Tick, "20131009_gc_tick_trade_201312_20131227.csv"),
                64712,
                84596673.8
            },

            new object[]
            {
                "../../../Data/future/cme/minute/es/20131010_openinterest.zip#20131010_es_minute_openinterest_201312_20131220.csv",
                LeanData
                    .ReadSymbolFromZipEntry(Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME),
                                            Resolution.Minute, "20131010_es_minute_openinterest_201312.csv"),
                3,
                8119169
            },

            new object[]
            {
                "../../../Data/future/comex/tick/gc/20131009_openinterest.zip#20131009_gc_tick_openinterest_201310_20131029.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX),
                                                Resolution.Tick, "20131009_gc_tick_openinterest_201310_20131029.csv"),
                4,
                1312
            },

            new object[]
            {
                "../../../Data/option/usa/minute/aapl/20140606_quote_american.zip#20140606_aapl_minute_quote_american_put_7500000_20141018.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("AAPL", SecurityType.Option, Market.USA),
                                                Resolution.Minute,
                                                "20140606_aapl_minute_quote_american_put_7500000_20141018.csv"),
                391,
                44210.7
            },

            new object[]
            {
                "../../../Data/option/usa/minute/aapl/20140606_trade_american.zip#20140606_aapl_minute_trade_american_call_6475000_20140606.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("AAPL", SecurityType.Option, Market.USA),
                                                Resolution.Minute,
                                                "20140606_aapl_minute_trade_american_call_6475000_20140606.csv"),
                374,
                745.35
            },

            new object[]
            {
                "../../../Data/option/usa/minute/goog/20151224_openinterest_american.zip#20151224_goog_minute_openinterest_american_call_3000000_20160115.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("GOOG", SecurityType.Option, Market.USA),
                                                Resolution.Minute,
                                                "20151224_goog_minute_openinterest_american_call_3000000_20160115.csv"),
                1,
                38
            },

            new object[]
            {
                "../../../Data/option/usa/daily/aapl_2014_openinterest_american.zip#aapl_openinterest_american_call_1950000_20150117.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("AAPL", SecurityType.Option, Market.USA),
                    Resolution.Daily,
                    "aapl_openinterest_american_call_1950000_20150117.csv"),
                2,
                824
            },

            new object[]
            {
            "../../../Data/option/usa/daily/aapl_2014_trade_american.zip#aapl_trade_american_call_5400000_20141018.csv",
            LeanData.ReadSymbolFromZipEntry(Symbol.Create("AAPL", SecurityType.Option, Market.USA),
                Resolution.Daily,
                "aapl_trade_american_call_5400000_20141018.csv"),
            1,
            109.9
            },

            new object[]
            {
                "../../../Data/option/usa/daily/aapl_2014_quote_american.zip#aapl_quote_american_call_307100_20150117.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("AAPL", SecurityType.Option, Market.USA),
                    Resolution.Daily,
                    "aapl_quote_american_call_307100_20150117.csv"),
                1,
                63.3
            }
        };


        [Test, TestCaseSource(nameof(SpotMarketCases))]
        public void ReadLeanSpotMarketsSecuritiesDataFromFilePath(string securityType, string market, string resolution, string ticker, string fileName, int rowsInfile, double sumValue)
        {
            // Arrange
            var filepath = GenerateFilepathForTesting(_dataDirectory, securityType, market, resolution, ticker, fileName);

            SecurityType securityTypeEnum;
            Enum.TryParse(securityType, true, out securityTypeEnum);
            var symbol = Symbol.Create(ticker, securityTypeEnum, market);

            // Act
            var ldr = new LeanDataReader(filepath);
            var data = ldr.Parse().ToList();
            // Assert
            Assert.True(symbol.Equals(data.First().Symbol));
            Assert.AreEqual(rowsInfile, data.Count);
            Assert.AreEqual(sumValue, data.Sum(c => c.Value));
        }

        public static object[] SpotMarketCases =
        {
            //TODO: generate Low resolution sample data for equities
            new object[] {"equity", "usa", "daily", "aig", "aig.zip", 5849, 340770.5801},
            new object[] {"equity", "usa", "minute", "aapl", "20140605_trade.zip", 686, 443184.58},
            new object[] {"equity", "usa", "minute", "ibm", "20131010_quote.zip", 584, 107061.125},
            new object[] {"equity", "usa", "second", "ibm", "20131010_trade.zip", 5060, 929385.34},
            new object[] {"equity", "usa", "tick", "bac", "20131011_trade.zip", 112177, 1591680.73},
            new object[] {"forex", "oanda", "minute", "eurusd", "20140502_quote.zip", 1222, 1693.578875},
            new object[] {"forex", "oanda", "second", "nzdusd", "20140514_quote.zip", 18061, 15638.724575},
            new object[] {"forex", "oanda", "tick", "eurusd", "20140507_quote.zip", 41367, 57598.54664},
            new object[] {"cfd", "oanda", "hour", "xauusd", "xauusd.zip", 76499, 90453133.772 },
            new object[] {"crypto", "coinbase", "second", "btcusd", "20161008_trade.zip", 3453, 2137057.57},
            new object[] {"crypto", "coinbase", "minute", "ethusd", "20170903_trade.zip", 1440, 510470.66},
            new object[] {"crypto", "coinbase", "daily", "btcusd", "btcusd_trade.zip", 1318, 3725052.03},
        };

        public static string GenerateFilepathForTesting(string dataDirectory, string securityType, string market, string resolution, string ticker,
                                                 string fileName)
        {
            string filepath;
            if (resolution == "daily" || resolution == "hour")
            {
                filepath = Path.Combine(dataDirectory, securityType, market, resolution, fileName);
            }
            else
            {
                filepath = Path.Combine(dataDirectory, securityType, market, resolution, ticker, fileName);
            }
            return filepath;
        }

    }
}
