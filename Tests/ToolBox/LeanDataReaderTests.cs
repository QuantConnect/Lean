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

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class LeanDataReaderTests
    {
        #region futures

        string _dataDirectory = "../../../Data/";
        DateTime _fromDate = new DateTime(2013, 10, 7);
        DateTime _toDate = new DateTime(2013, 10, 11);


        [Test]
        public void ReadFutureChainData()
        {
            var canonicalFutures = new Dictionary<Symbol,string>()
            {
                { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                "ESZ13|ESH14|ESM14|ESU14|ESZ14" },
                {Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                "GCV13|GCX13|GCZ13|GCG14|GCJ14|GCM14|GCQ14|GCV14|GCZ14|GCG15|GCJ15|GCM15|GCQ15|GCZ15|GCM16|GCZ16|GCM17|GCZ17|GCM18|GCZ18|GCM19"},
            };

            var tickTypes = new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest };

            var resolutions = new[] {Resolution.Hour, Resolution.Daily };


            foreach (var canonical in canonicalFutures)
            {
                foreach (var res in resolutions)
                {
                    foreach (var tickType in tickTypes)
                    {
                        var futures = LoadFutureChain(canonical.Key,_fromDate, tickType, res);

                        string chain = string.Join("|", futures.Select(f => f.Value));

                        if (tickType==TickType.Quote) //only quotes have the full chain!
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

            var tickTypes = new[] {TickType.Trade, TickType.Quote, TickType.OpenInterest };

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
                                future, inputResolution,TimeZones.NewYork, TimeZones.NewYork,
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
    }
}
