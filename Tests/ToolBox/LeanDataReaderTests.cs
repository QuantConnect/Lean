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
using QuantConnect;
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
    [TestFixture]
    public class LeanDataReaderTests
    {
        string _dataDirectory = "../../../Data/";
        DateTime _fromDate = new DateTime(2013, 10, 7);
        DateTime _toDate = new DateTime(2013, 10, 11);

        #region futures

        [Test]
        public void ReadFutureChainData()
        {
            var canonicalFutures = new Dictionary<Symbol, string>()
            {
                { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                    "ESZ13|ESH14|ESM14|ESU14|ESZ14" },
                {Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                    "GCV13|GCX13|GCZ13|GCG14|GCJ14|GCM14|GCQ14|GCV14|GCZ14|GCG15|GCJ15|GCM15|GCQ15|GCZ15|GCM16|GCZ16|GCM17|GCZ17|GCM18|GCZ18|GCM19"},
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

            var factory = new ZipEntryNameSubscriptionDataSourceReader(config, date, false);

            return factory.Read(new SubscriptionDataSource(filePath, SubscriptionTransportMedium.LocalFile, FileFormat.ZipEntryName))
                          .Select(s => s.Symbol).ToList();
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

                var data = leanDataReader.Parse().ToList();
                foreach (var bar in data)
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

            var futures = new[] { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA)};
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

                    var data = leanDataReader.Parse().ToList();
                    var consolidator = consolidators[future.Value];

                    foreach (var bar in data)
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


        [Test, TestCaseSource(nameof(OptionAndFuturesCases)), Category("TravisExclude")]
        public void ReadLeanFutureAndOptionDataFromFilePath(string composedFilePath, Symbol symbol,  int rowsInfile, double sumValue)
        {
            // Act
            var ldr = new LeanDataReader(composedFilePath);
            var data = ldr.Parse().ToArray();
            // Assert
            Assert.True(symbol.Equals(data.First().Symbol));
            Assert.AreEqual(rowsInfile, data.Length);
            Assert.AreEqual(sumValue, data.Sum(c => c.Value));
        }


        public static object[] OptionAndFuturesCases =
        {
            new object[]
            {
                "../../../Data/future/usa/minute/es/20131008_quote.zip#20131008_es_minute_quote_201312.csv",
                LeanData
                    .ReadSymbolFromZipEntry(Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                                            Resolution.Minute, "20131008_es_minute_quote_201312.csv"),
                1411,
                2346061.875
            },

            new object[]
            {
                "../../../Data/future/usa/minute/gc/20131010_trade.zip#20131010_gc_minute_trade_201312.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                                                Resolution.Minute, "20131010_gc_minute_trade_201312.csv"),
                1379,
                1791800.9
            },

            new object[]
            {
                "../../../Data/future/usa/tick/gc/20131009_quote.zip#20131009_gc_tick_quote_201406.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                                                Resolution.Tick, "20131009_gc_tick_quote_201406.csv"),
                197839,
                259245064.8
            },

            new object[]
            {
                "../../../Data/future/usa/tick/gc/20131009_trade.zip#20131009_gc_tick_trade_201312.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                                                Resolution.Tick, "20131009_gc_tick_trade_201312.csv"),
                64712,
                84596673.8
            },

            new object[]
            {
                "../../../Data/future/usa/minute/es/20131010_openinterest.zip#20131010_es_minute_openinterest_201312.csv",
                LeanData
                    .ReadSymbolFromZipEntry(Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                                            Resolution.Minute, "20131010_es_minute_openinterest_201312.csv"),
                3,
                8119169
            },

            new object[]
            {
                "../../../Data/future/usa/tick/gc/20131009_openinterest.zip#20131009_gc_tick_openinterest_201310.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                                                Resolution.Tick, "20131009_gc_tick_openinterest_201310.csv"),
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
                44212.225
            },

            new object[]
            {
                "../../../Data/option/usa/minute/aapl/20140606_trade_american.zip#20140606_aapl_minute_trade_american_call_6475000_20140606.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("AAPL", SecurityType.Option, Market.USA),
                                                Resolution.Minute,
                                                "20140606_aapl_minute_trade_american_call_6475000_20140606.csv"),
                377,
                750.1
            },

            new object[]
            {
                "../../../Data/option/usa/minute/goog/20151224_openinterest_american.zip#20151224_goog_minute_openinterest_american_call_3000000_20160115.csv",
                LeanData.ReadSymbolFromZipEntry(Symbol.Create("GOOG", SecurityType.Option, Market.USA),
                                                Resolution.Minute,
                                                "20151224_goog_minute_openinterest_american_call_3000000_20160115.csv"),
                1,
                38
            }
        };


        [Test, TestCaseSource(nameof(SpotMarketCases)), Category("TravisExclude")]
        public void ReadLeanSpotMarketsSecuritiesDataFromFilePath(string securityType, string market, string resolution, string ticker, string fileName, int rowsInfile, double sumValue)
        {
            // Arrange
            var filepath = GenerateFilepathForTesting(_dataDirectory, securityType, market, resolution, ticker, fileName);

            SecurityType securityTypeEnum;
            Enum.TryParse(securityType, true, out securityTypeEnum);
            var symbol = Symbol.Create(ticker, securityTypeEnum, market);

            // Act
            var ldr = new LeanDataReader(filepath);
            var data = ldr.Parse().ToArray();
            // Assert
            Assert.True(symbol.Equals(data.First().Symbol));
            Assert.AreEqual(data.Length, rowsInfile);
            Assert.AreEqual(data.Sum(c => c.Value), sumValue);
        }

        public static object[] SpotMarketCases =
        {
            new object[] {"equity", "usa", "daily", "aig", "aig.zip", 4433, 267747.235},
            new object[] {"equity", "usa", "minute", "aapl", "20140605_trade.zip", 658, 425068.8450},
            new object[] {"equity", "usa", "second", "ibm", "20131010_trade.zip", 4409, 809851.9580},
            new object[] {"equity", "usa", "tick", "bac", "20131011_trade.zip", 112230, 1592319.5871},
            new object[] {"forex", "fxcm", "minute", "eurusd", "20140502_quote.zip", 958, 1327.638085},
            new object[] {"forex", "fxcm", "second", "nzdusd", "20140514_quote.zip", 25895, 22432.757185},
            new object[] {"forex", "fxcm", "tick", "eurusd", "20140507_quote.zip", 89826, 125073.092245},
            new object[] {"cfd", "oanda", "hour", "xauusd", "xauusd.zip", 69081, 80935843.1265},
            new object[] {"crypto", "gdax", "second", "btcusd", "20161008_trade.zip", 3453, 2137057.57},
            new object[] {"crypto", "gdax", "second", "btcusd", "20161009_quote.zip", 1438, 889045.065},
            new object[] {"crypto", "gdax", "minute", "ethusd", "20170903_trade.zip", 1440, 510470.66},
            new object[] {"crypto", "gdax", "minute", "btcusd", "20161007_quote.zip", 1438, 884448.535},
            new object[] {"crypto", "gdax", "daily", "btcusd", "btcusd_trade.zip", 1025, 1020535.83},
            new object[] {"crypto", "gdax", "daily", "btcusd", "btcusd_quote.zip", 788, 954122.585}
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
