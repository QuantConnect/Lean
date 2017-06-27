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
    class LeanDataReaderTests
    {

        #region futures

        string _datadir = "../../../Data/";
        DateTime _fromDate = new DateTime(2013, 10, 7);
        DateTime _toDate = new DateTime(2013, 10, 11);


        [Test]
        public void ReadFutureChainData()
        {           
            var canonical_futures = new Dictionary<Symbol,string>()
            {
                { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                "ESZ13|ESH14|ESM14|ESU14|ESZ14" },
                {Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA),
                "GCV13|GCX13|GCZ13|GCG14|GCJ14|GCM14|GCQ14|GCV14|GCZ14|GCG15|GCJ15|GCM15|GCQ15|GCZ15|GCM16|GCZ16|GCM17|GCZ17|GCM18|GCZ18|GCM19"},
            };
                                                      
            var ticktypes = new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest };

            var resolutions = new[] {Resolution.Hour, Resolution.Daily };


            foreach (var canonical in canonical_futures)
            {
                foreach (var res in resolutions)
                {
                    foreach (var ticktype in ticktypes)
                    {
                        var futures = LoadFutureChain(canonical.Key,_fromDate, ticktype, res);

                        string chain = string.Join("|", futures.Select(f => f.Value));

                        if (ticktype==TickType.Quote) //only quotes have the full chain!
                            Assert.AreEqual(canonical.Value, chain);

                        foreach (var future in futures)
                        {
                            string csv = LoadFutureData(future, ticktype, res);
                            Assert.IsTrue(!string.IsNullOrEmpty(csv));
                        }
                    }
                }
            }
        }

        List<Symbol> LoadFutureChain(Symbol basefuture, DateTime date, TickType ticktype, Resolution res)
        {
            var filePath = LeanData.GenerateZipFilePath(_datadir, basefuture, date, res, ticktype);

            //load future chain first
            var config = new SubscriptionDataConfig(typeof(ZipEntryName), basefuture, res,
                TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, ticktype);

            var dataProvider = new DefaultDataProvider();
            var dataCacheProvider = new SingleEntryDataCacheProvider(dataProvider);
            var factory = new ZipEntryNameSubscriptionDataSourceReader(dataCacheProvider, config, date, false);

            return factory.Read(new SubscriptionDataSource(filePath, SubscriptionTransportMedium.LocalFile, FileFormat.ZipEntryName))
                   .Select(s => s.Symbol).ToList();
        }

        string LoadFutureData(Symbol future, TickType ticktype, Resolution res)
        {
            var datatype = LeanData.GetDataType(res, ticktype);
            var config = new SubscriptionDataConfig(datatype, future, res,
                TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, ticktype);

            var date = _fromDate;

            var sb = new StringBuilder();

            while (date <= _toDate)
            {
                var leanDataReader = new LeanDataReader(config, future, res, date, _datadir);

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
        public void GenerateDailyAndHourlyDataTest()
        {
            
            var ticktypes = new[] {TickType.Trade, TickType.Quote, TickType.OpenInterest };

            var futures = new[] { Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA),
                        Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.USA)};
            var resolutions = new[] { Resolution.Hour, Resolution.Daily };

            foreach (var future in futures)
                foreach (var res in resolutions)
                    foreach (var ticktype in ticktypes)
                        ConvertMinuteFuturesData(future, ticktype, res);
        }

        void ConvertMinuteFuturesData(Symbol canonical, TickType ticktype, Resolution res_output, Resolution res_input = Resolution.Minute)
        {

            var timespans = new Dictionary<Resolution, TimeSpan>()
            {
                { Resolution.Daily, TimeSpan.FromHours(24)},
                { Resolution.Hour, TimeSpan.FromHours(1)},
            };

            var timespan = timespans[res_output];

            var consolidators = new Dictionary<TickType, Func<IDataConsolidator>>()
            {
                {TickType.Quote, () => new QuoteBarConsolidator(timespan)},
                {TickType.OpenInterest, ()=> new OpenInterestConsolidator(timespan)},
                {TickType.Trade, ()=> new TradeBarConsolidator(timespan) }

            };

            var cons = new Dictionary<string, IDataConsolidator>();
            var configs = new Dictionary<string, SubscriptionDataConfig>();
            var outputfiles = new Dictionary<string, StringBuilder>();
            var allfutures = new Dictionary<string, Symbol>();

            var date = _fromDate;
            while (date <= _toDate)
            {
                var futures = LoadFutureChain(canonical, date, ticktype, res_input);
                foreach (var future in futures)
                {
                    if (!allfutures.ContainsKey(future.Value))
                    {
                        allfutures[future.Value] = future;
                        var config = new SubscriptionDataConfig(LeanData.GetDataType(res_output, ticktype),
                                future, res_input,TimeZones.NewYork, TimeZones.NewYork, 
                                false, false, false, false, ticktype);
                        configs[future.Value] = config;

                        cons[future.Value] = consolidators[ticktype].Invoke();
                        
                        var sb = new StringBuilder();
                        outputfiles[future.Value] = sb;

                        cons[future.Value].DataConsolidated += (sender, bar) =>
                        {
                            sb.Append(LeanData.GenerateLine(bar, SecurityType.Future, res_output) + Environment.NewLine);
                        };
                    }

                    var leanDataReader = new LeanDataReader(configs[future.Value], future, res_input, date, _datadir);

                    var data = leanDataReader.Parse().ToList();
                    var consolidator = cons[future.Value];

                    foreach (var bar in data)
                    {
                        consolidator.Update(bar);
                    }
                }
                date = date.AddDays(1);
            }

            //write all results
            foreach (var con in cons.Values)
                con.Scan(date);

            var zip = LeanData.GenerateRelativeZipFilePath(canonical, _fromDate, res_output, ticktype);
            var zippath = Path.Combine(_datadir, zip);
            var fi = new FileInfo(zippath);

            if (!fi.Directory.Exists)
                fi.Directory.Create();

            foreach (var future in allfutures.Values)
            {
                var zipentry = LeanData.GenerateZipEntryName(future, _fromDate, res_output, ticktype);
                var sb = outputfiles[future.Value];
                //Uncomment to write zip files              
                //QuantConnect.Compression.ZipCreateAppendData(zippath, zipentry, sb.ToString());

                Assert.IsTrue(sb.Length > 0);

            }
        }

        #endregion
    }
}
