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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using Bitcoin = QuantConnect.Algorithm.CSharp.LiveTradingFeaturesAlgorithm.Bitcoin;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class LeanDataTests
    {
        [SetUp]
        public void SetUp()
        {
            SymbolCache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SymbolCache.Clear();
        }

        [Test, TestCaseSource(nameof(GetLeanDataTestParameters))]
        public void GenerateZipFileName(LeanDataTestParameters parameters)
        {
            var zip = LeanData.GenerateZipFileName(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipFileName, zip);
        }

        [Test, TestCaseSource(nameof(GetLeanDataTestParameters))]
        public void GenerateZipEntryName(LeanDataTestParameters parameters)
        {
            var entry = LeanData.GenerateZipEntryName(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipEntryName, entry);
        }

        [Test, TestCaseSource(nameof(GetLeanDataTestParameters))]
        public void GenerateRelativeZipFilePath(LeanDataTestParameters parameters)
        {
            var relativePath = LeanData.GenerateRelativeZipFilePath(parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedRelativeZipFilePath, relativePath);
        }

        [Test, TestCaseSource(nameof(GetLeanDataTestParameters))]
        public void GenerateZipFilePath(LeanDataTestParameters parameters)
        {
            var path = LeanData.GenerateZipFilePath(Globals.DataFolder, parameters.Symbol, parameters.Date, parameters.Resolution, parameters.TickType);
            Assert.AreEqual(parameters.ExpectedZipFilePath, path);
        }

        [Test, TestCaseSource(nameof(GetLeanDataLineTestParameters))]
        public void GenerateLine(LeanDataLineTestParameters parameters)
        {
            var line = LeanData.GenerateLine(parameters.Data, parameters.SecurityType, parameters.Resolution);
            Assert.AreEqual(parameters.ExpectedLine, line);

            if (parameters.Config.Type == typeof(QuoteBar))
            {
                Assert.AreEqual(line.Split(',').Length, 11);
            }

            if (parameters.Config.Type == typeof(TradeBar))
            {
                Assert.AreEqual(line.Split(',').Length, 6);
            }
        }

        [Test, TestCaseSource(nameof(GetLeanDataLineTestParameters))]
        public void ParsesGeneratedLines(LeanDataLineTestParameters parameters)
        {
            // ignore time zone issues here, we'll just say everything is UTC, so no conversions are performed
            var factory = (BaseData) Activator.CreateInstance(parameters.Data.GetType());
            var parsed = factory.Reader(parameters.Config, parameters.ExpectedLine, parameters.Data.Time.Date, false);

            Assert.IsInstanceOf(parameters.Config.Type, parsed);
            Assert.AreEqual(parameters.Data.Time, parsed.Time);
            Assert.AreEqual(parameters.Data.EndTime, parsed.EndTime);
            Assert.AreEqual(parameters.Data.Symbol, parsed.Symbol);
            Assert.AreEqual(parameters.Data.Value, parsed.Value);
            if (parsed is Tick)
            {
                var expected = (Tick) parameters.Data;
                var actual = (Tick) parsed;
                Assert.AreEqual(expected.Quantity, actual.Quantity);
                Assert.AreEqual(expected.BidPrice, actual.BidPrice);
                Assert.AreEqual(expected.AskPrice, actual.AskPrice);
                Assert.AreEqual(expected.BidSize, actual.BidSize);
                Assert.AreEqual(expected.AskSize, actual.AskSize);
                Assert.AreEqual(expected.Exchange, actual.Exchange);
                Assert.AreEqual(expected.SaleCondition, actual.SaleCondition);
                Assert.AreEqual(expected.Suspicious, actual.Suspicious);
            }
            else if (parsed is TradeBar)
            {
                var expected = (TradeBar) parameters.Data;
                var actual = (TradeBar) parsed;
                AssertBarsAreEqual(expected, actual);
                Assert.AreEqual(expected.Volume, actual.Volume);
            }
            else if (parsed is QuoteBar)
            {
                var expected = (QuoteBar) parameters.Data;
                var actual = (QuoteBar) parsed;
                AssertBarsAreEqual(expected.Bid, actual.Bid);
                AssertBarsAreEqual(expected.Ask, actual.Ask);
                Assert.AreEqual(expected.LastBidSize, actual.LastBidSize);
                Assert.AreEqual(expected.LastAskSize, actual.LastAskSize);
            }
        }

        [Test, TestCaseSource(nameof(GetLeanDataLineTestParameters))]
        public void GetSourceMatchesGenerateZipFilePath(LeanDataLineTestParameters parameters)
        {
            var source = parameters.Data.GetSource(parameters.Config, parameters.Data.Time.Date, false);
            var normalizedSourcePath = new FileInfo(source.Source).FullName;
            var zipFilePath = LeanData.GenerateZipFilePath(Globals.DataFolder, parameters.Data.Symbol, parameters.Data.Time.Date, parameters.Resolution, parameters.TickType);
            var normalizeZipFilePath = new FileInfo(zipFilePath).FullName;
            var indexOfHash = normalizedSourcePath.LastIndexOf("#", StringComparison.Ordinal);
            if (indexOfHash > 0)
            {
                normalizedSourcePath = normalizedSourcePath.Substring(0, indexOfHash);
            }
            Assert.AreEqual(normalizeZipFilePath, normalizedSourcePath);
        }

        [Test, TestCaseSource(nameof(GetLeanDataTestParameters))]
        public void GetSource(LeanDataTestParameters parameters)
        {
            var factory = (BaseData)Activator.CreateInstance(parameters.BaseDataType);
            var source = factory.GetSource(parameters.Config, parameters.Date, false);
            var expected = parameters.ExpectedZipFilePath;
            if (parameters.SecurityType == SecurityType.Option || parameters.SecurityType == SecurityType.Future)
            {
                expected += "#" + parameters.ExpectedZipEntryName;
            }
            Assert.AreEqual(expected, source.Source);
        }

        [Test]
        public void GetDataType_ReturnsCorrectType()
        {
            var tickType = typeof(Tick);
            var openInterestType = typeof(OpenInterest);
            var quoteBarType = typeof(QuoteBar);
            var tradeBarType = typeof(TradeBar);

            Assert.AreEqual(LeanData.GetDataType(Resolution.Tick, TickType.OpenInterest), tickType);
            Assert.AreNotEqual(LeanData.GetDataType(Resolution.Daily, TickType.OpenInterest), tickType);

            Assert.AreEqual(LeanData.GetDataType(Resolution.Second, TickType.OpenInterest), openInterestType);
            Assert.AreNotEqual(LeanData.GetDataType(Resolution.Tick, TickType.OpenInterest), openInterestType);

            Assert.AreEqual(LeanData.GetDataType(Resolution.Minute, TickType.Quote), quoteBarType);
            Assert.AreNotEqual(LeanData.GetDataType(Resolution.Second, TickType.Trade), quoteBarType);

            Assert.AreEqual(LeanData.GetDataType(Resolution.Hour, TickType.Trade), tradeBarType);
            Assert.AreNotEqual(LeanData.GetDataType(Resolution.Tick, TickType.OpenInterest), tradeBarType);
        }

        [Test]
        public void LeanData_CanDetermineTheCorrectCommonDataTypes()
        {
            Assert.IsTrue(LeanData.IsCommonLeanDataType(typeof(OpenInterest)));
            Assert.IsTrue(LeanData.IsCommonLeanDataType(typeof(TradeBar)));
            Assert.IsTrue(LeanData.IsCommonLeanDataType(typeof(QuoteBar)));
            Assert.IsFalse(LeanData.IsCommonLeanDataType(typeof(Bitcoin)));
        }

        [Test]
        public void LeanData_GetCommonTickTypeForCommonDataTypes_ReturnsCorrectDataForTickResolution()
        {
            Assert.AreEqual(LeanData.GetCommonTickTypeForCommonDataTypes(typeof(Tick), SecurityType.Cfd), TickType.Quote);
            Assert.AreEqual(LeanData.GetCommonTickTypeForCommonDataTypes(typeof(Tick), SecurityType.Forex), TickType.Quote);
        }

        [Test]
        public void IncorrectPaths_CannotBeParsed()
        {
            DateTime date;
            Symbol symbol;
            Resolution resolution;

            var invalidPath = "forex/fxcm/eurusd/20160101_quote.zip";
            Assert.IsFalse(LeanData.TryParsePath(invalidPath, out symbol, out date, out resolution));

            var nonExistantPath = "Data/f/fxcm/eurusd/20160101_quote.zip";
            Assert.IsFalse(LeanData.TryParsePath(nonExistantPath, out symbol, out date, out resolution));

            var notAPath = "ooooooooooooooooooooooooooooooooooooooooooooooooooooooo";
            Assert.IsFalse(LeanData.TryParsePath(notAPath, out symbol, out date, out resolution));

            var  emptyPath = "";
            Assert.IsFalse(LeanData.TryParsePath(emptyPath, out symbol, out date, out resolution));

            string nullPath = null;
            Assert.IsFalse(LeanData.TryParsePath(nullPath, out symbol, out date, out resolution));

            var optionsTradePath = "Data/option/u sa/minute/aapl/20140606_trade_american.zip";
            Assert.IsFalse(LeanData.TryParsePath(optionsTradePath, out symbol, out date, out resolution));
        }

        [Test]
        public void CorrectPaths_CanBeParsedCorrectly()
        {
            DateTime date;
            Symbol symbol;
            Resolution resolution;

            var customPath = "a/very/custom/path/forex/oanda/tick/eurusd/20170104_quote.zip";
            Assert.IsTrue(LeanData.TryParsePath(customPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Forex);
            Assert.AreEqual(symbol.ID.Market, Market.Oanda);
            Assert.AreEqual(resolution, Resolution.Tick);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "eurusd");
            Assert.AreEqual(date.Date, Parse.DateTime("2017-01-04").Date);

            var mixedPathSeperators = @"Data//forex/fxcm\/minute//eurusd\\20160101_quote.zip";
            Assert.IsTrue(LeanData.TryParsePath(mixedPathSeperators, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Forex);
            Assert.AreEqual(symbol.ID.Market, Market.FXCM);
            Assert.AreEqual(resolution, Resolution.Minute);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "eurusd");
            Assert.AreEqual(date.Date, Parse.DateTime("2016-01-01").Date);

            var longRelativePath = "../../../../../../../../../Data/forex/fxcm/hour/gbpusd.zip";
            Assert.IsTrue(LeanData.TryParsePath(longRelativePath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Forex);
            Assert.AreEqual(symbol.ID.Market, Market.FXCM);
            Assert.AreEqual(resolution, Resolution.Hour);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "gbpusd");
            Assert.AreEqual(date.Date, DateTime.MinValue);

            var shortRelativePath = "Data/forex/fxcm/minute/eurusd/20160102_quote.zip";
            Assert.IsTrue(LeanData.TryParsePath(shortRelativePath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Forex);
            Assert.AreEqual(symbol.ID.Market, Market.FXCM);
            Assert.AreEqual(resolution, Resolution.Minute);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "eurusd");
            Assert.AreEqual(date.Date, Parse.DateTime("2016-01-02").Date);

            var dailyEquitiesPath = "Data/equity/usa/daily/aapl.zip";
            Assert.IsTrue(LeanData.TryParsePath(dailyEquitiesPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Equity);
            Assert.AreEqual(symbol.ID.Market, Market.USA);
            Assert.AreEqual(resolution, Resolution.Daily);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "aapl");
            Assert.AreEqual(date.Date, DateTime.MinValue);

            var minuteEquitiesPath = "Data/equity/usa/minute/googl/20070103_trade.zip";
            Assert.IsTrue(LeanData.TryParsePath(minuteEquitiesPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Equity);
            Assert.AreEqual(symbol.ID.Market, Market.USA);
            Assert.AreEqual(resolution, Resolution.Minute);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "goog");
            Assert.AreEqual(date.Date, Parse.DateTime("2007-01-03").Date);

            var cfdPath = "Data/cfd/oanda/minute/bcousd/20160101_trade.zip";
            Assert.IsTrue(LeanData.TryParsePath(cfdPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Cfd);
            Assert.AreEqual(symbol.ID.Market, Market.Oanda);
            Assert.AreEqual(resolution, Resolution.Minute);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "bcousd");
            Assert.AreEqual(date.Date, Parse.DateTime("2016-01-01").Date);
        }

        [TestCase("Data\\alternative\\estimize\\consensus\\aapl.csv", "aapl", null)]
        [TestCase("Data\\alternative\\psychsignal\\aapl\\20161007.zip", "aapl", "2016-10-07")]
        [TestCase("Data\\alternative\\sec\\aapl\\20161007_8K.zip", "aapl", "2016-10-07")]
        [TestCase("Data\\alternative\\smartinsider\\intentions\\aapl.tsv", "aapl", null)]
        [TestCase("Data\\alternative\\trading-economics\\calendar\\fdtr\\20161007.zip", "fdtr", "2016-10-07")]
        [TestCase("Data\\alternative\\ustreasury\\yieldcurverates.zip", "yieldcurverates", null)]
        public void AlternativePaths_CanBeParsedCorrectly(string path, string expectedSymbol, string expectedDate)
        {
            DateTime date;
            Symbol symbol;
            Resolution resolution;

            Assert.IsTrue(LeanData.TryParsePath(path, out symbol, out date, out resolution));
            Assert.AreEqual(SecurityType.Base, symbol.SecurityType);
            Assert.AreEqual(Market.USA, symbol.ID.Market);
            Assert.AreEqual(Resolution.Daily, resolution);
            Assert.AreEqual(expectedSymbol, symbol.ID.Symbol.ToLowerInvariant());
            Assert.AreEqual(expectedDate == null ? default(DateTime) : Parse.DateTime(expectedDate).Date, date);
        }

        [Test]
        public void CryptoPaths_CanBeParsedCorrectly()
        {
            DateTime date;
            Symbol symbol;
            Resolution resolution;

            var cryptoPath = "Data\\crypto\\gdax\\daily\\btcusd_quote.zip";
            Assert.IsTrue(LeanData.TryParsePath(cryptoPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Crypto);
            Assert.AreEqual(symbol.ID.Market, Market.GDAX);
            Assert.AreEqual(resolution, Resolution.Daily);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "btcusd");

            cryptoPath = "Data\\crypto\\gdax\\hour\\btcusd_quote.zip";
            Assert.IsTrue(LeanData.TryParsePath(cryptoPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Crypto);
            Assert.AreEqual(symbol.ID.Market, Market.GDAX);
            Assert.AreEqual(resolution, Resolution.Hour);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "btcusd");

            cryptoPath = "Data\\crypto\\gdax\\minute\\btcusd\\20161007_quote.zip";
            Assert.IsTrue(LeanData.TryParsePath(cryptoPath, out symbol, out date, out resolution));
            Assert.AreEqual(symbol.SecurityType, SecurityType.Crypto);
            Assert.AreEqual(symbol.ID.Market, Market.GDAX);
            Assert.AreEqual(resolution, Resolution.Minute);
            Assert.AreEqual(symbol.ID.Symbol.ToLowerInvariant(), "btcusd");
            Assert.AreEqual(date.Date, Parse.DateTime("2016-10-07").Date);
        }

        [TestCase(SecurityType.Base, "alteRNative")]
        [TestCase(SecurityType.Equity, "Equity")]
        [TestCase(SecurityType.Cfd, "Cfd")]
        [TestCase(SecurityType.Commodity, "Commodity")]
        [TestCase(SecurityType.Crypto, "Crypto")]
        [TestCase(SecurityType.Forex, "Forex")]
        [TestCase(SecurityType.Future, "Future")]
        [TestCase(SecurityType.Option, "Option")]
        [TestCase(SecurityType.FutureOption, "FutureOption")]
        public void ParsesDataSecurityType(SecurityType type, string path)
        {
            Assert.AreEqual(type, LeanData.ParseDataSecurityType(path));
        }

        [Test]
        public void SecurityTypeAsDataPath()
        {
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("alternative"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("equity"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("base"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("option"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("cfd"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("crypto"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("future"));
            Assert.IsTrue(LeanData.SecurityTypeAsDataPath.Contains("forex"));
        }

        [Test]
        public void OptionZipFilePathWithUnderlyingEquity()
        {
            var underlying = Symbol.Create("SPY", SecurityType.Equity, QuantConnect.Market.USA);
            var optionSymbol = Symbol.CreateOption(
                underlying,
                Market.USA,
                OptionStyle.American,
                OptionRight.Put,
                4200m,
                new DateTime(2020, 12, 31));

            var optionZipFilePath = LeanData.GenerateZipFilePath(Globals.DataFolder, optionSymbol, new DateTime(2020, 9, 22), Resolution.Minute, TickType.Quote)
                .Replace(Path.DirectorySeparatorChar, '/');
            var optionEntryFilePath = LeanData.GenerateZipEntryName(optionSymbol, new DateTime(2020, 9, 22), Resolution.Minute, TickType.Quote);

            Assert.AreEqual("../../../Data/option/usa/minute/spy/20200922_quote_american.zip", optionZipFilePath);
            Assert.AreEqual("20200922_spy_minute_quote_american_put_42000000_20201231.csv", optionEntryFilePath);
        }

        [TestCase("ES", "ES")]
        [TestCase("DC", "DC")]
        [TestCase("GC", "OG")]
        [TestCase("ZT", "OZT")]
        public void OptionZipFilePathWithUnderlyingFuture(string futureOptionTicker, string expectedFutureOptionTicker)
        {
            var underlying = Symbol.CreateFuture(futureOptionTicker, Market.CME, new DateTime(2021, 3, 19));
            var optionSymbol = Symbol.CreateOption(
                underlying,
                Market.CME,
                OptionStyle.American,
                OptionRight.Put,
                4200m,
                new DateTime(2021, 3, 18));

            var optionZipFilePath = LeanData.GenerateZipFilePath(Globals.DataFolder, optionSymbol, new DateTime(2020, 9, 22), Resolution.Minute, TickType.Quote)
                .Replace(Path.DirectorySeparatorChar, '/');
            var optionEntryFilePath = LeanData.GenerateZipEntryName(optionSymbol, new DateTime(2020, 9, 22), Resolution.Minute, TickType.Quote);

            Assert.AreEqual($"../../../Data/futureoption/cme/minute/{expectedFutureOptionTicker.ToLowerInvariant()}/{underlying.ID.Date:yyyyMMdd}/20200922_quote_american.zip", optionZipFilePath);
            Assert.AreEqual($"20200922_{expectedFutureOptionTicker.ToLowerInvariant()}_minute_quote_american_put_42000000_{optionSymbol.ID.Date:yyyyMMdd}.csv", optionEntryFilePath);
        }

        [TestCase(OptionRight.Call, 1650, 2020, 3, 26)]
        [TestCase(OptionRight.Call, 1540, 2020, 3, 26)]
        [TestCase(OptionRight.Call, 1600, 2020, 2, 25)]
        [TestCase(OptionRight.Call, 1545, 2020, 2, 25)]
        public void FutureOptionSingleZipFileContainingMultipleFuturesOptionsContracts(OptionRight right, int strike, int year, int month, int day)
        {
            var underlying = Symbol.CreateFuture("GC", Market.COMEX, new DateTime(2020, 4, 28));
            var expiry = new DateTime(year, month, day);
            var optionSymbol = Symbol.CreateOption(
                underlying,
                Market.COMEX,
                OptionStyle.American,
                right,
                (decimal)strike,
                expiry);

            var optionZipFilePath = LeanData.GenerateZipFilePath(Globals.DataFolder, optionSymbol, new DateTime(2020, 1, 5), Resolution.Minute, TickType.Quote)
                .Replace(Path.DirectorySeparatorChar, '/');
            var optionEntryFilePath = LeanData.GenerateZipEntryName(optionSymbol, new DateTime(2020, 1, 5), Resolution.Minute, TickType.Quote);

            Assert.AreEqual("../../../Data/futureoption/comex/minute/og/20200428/20200105_quote_american.zip", optionZipFilePath);
            Assert.AreEqual($"20200105_og_minute_quote_american_{right.ToLower()}_{strike}0000_{expiry:yyyyMMdd}.csv", optionEntryFilePath);
        }

        private static void AssertBarsAreEqual(IBar expected, IBar actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }
            if (expected == null && actual != null)
            {
                Assert.Fail("Expected null bar");
            }
            Assert.AreEqual(expected.Open, actual.Open);
            Assert.AreEqual(expected.High, actual.High);
            Assert.AreEqual(expected.Low, actual.Low);
            Assert.AreEqual(expected.Close, actual.Close);
        }

        private static TestCaseData[] GetLeanDataTestParameters()
        {
            var date = new DateTime(2016, 02, 17);
            var dateFutures = new DateTime(2018, 12, 10);

            return new List<LeanDataTestParameters>
            {
                // equity
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Tick, TickType.Trade, "20160217_trade.zip", "20160217_spy_Trade_Tick.csv", "equity/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Second, TickType.Trade, "20160217_trade.zip", "20160217_spy_second_trade.csv", "equity/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Minute, TickType.Trade, "20160217_trade.zip", "20160217_spy_minute_trade.csv", "equity/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Hour, TickType.Trade, "spy.zip", "spy.csv", "equity/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY, date, Resolution.Daily, TickType.Trade, "spy.zip", "spy.csv", "equity/usa/daily"),

                // equity option trades
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Tick, TickType.Trade, "20160217_trade_american.zip", "20160217_spy_tick_trade_american_put_1920000_20160219.csv", "option/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Tick, TickType.Quote, "20160217_quote_american.zip", "20160217_spy_tick_quote_american_put_1920000_20160219.csv", "option/usa/tick/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Second, TickType.Trade, "20160217_trade_american.zip", "20160217_spy_second_trade_american_put_1920000_20160219.csv", "option/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Second, TickType.Quote, "20160217_quote_american.zip", "20160217_spy_second_quote_american_put_1920000_20160219.csv", "option/usa/second/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Minute, TickType.Trade, "20160217_trade_american.zip", "20160217_spy_minute_trade_american_put_1920000_20160219.csv", "option/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Minute, TickType.Quote, "20160217_quote_american.zip", "20160217_spy_minute_quote_american_put_1920000_20160219.csv", "option/usa/minute/spy"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Hour, TickType.Trade, "spy_trade_american.zip", "spy_trade_american_put_1920000_20160219.csv", "option/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Hour, TickType.Quote, "spy_quote_american.zip", "spy_quote_american_put_1920000_20160219.csv", "option/usa/hour"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Daily, TickType.Trade, "spy_trade_american.zip", "spy_trade_american_put_1920000_20160219.csv", "option/usa/daily"),
                new LeanDataTestParameters(Symbols.SPY_P_192_Feb19_2016, date, Resolution.Daily, TickType.Quote, "spy_quote_american.zip", "spy_quote_american_put_1920000_20160219.csv", "option/usa/daily"),

                // forex
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_tick_quote.csv", "forex/oanda/tick/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_second_quote.csv", "forex/oanda/second/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_eurusd_minute_quote.csv", "forex/oanda/minute/eurusd"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Hour, TickType.Quote, "eurusd.zip", "eurusd.csv", "forex/oanda/hour"),
                new LeanDataTestParameters(Symbols.EURUSD, date, Resolution.Daily, TickType.Quote, "eurusd.zip", "eurusd.csv", "forex/oanda/daily"),

                // cfd
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_tick_quote.csv", "cfd/fxcm/tick/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_second_quote.csv", "cfd/fxcm/second/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_de10ybeur_minute_quote.csv", "cfd/fxcm/minute/de10ybeur"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Hour, TickType.Quote, "de10ybeur.zip", "de10ybeur.csv", "cfd/fxcm/hour"),
                new LeanDataTestParameters(Symbols.DE10YBEUR, date, Resolution.Daily, TickType.Quote, "de10ybeur.zip", "de10ybeur.csv", "cfd/fxcm/daily"),

                // Crypto - trades
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Tick, TickType.Trade, "20160217_trade.zip", "20160217_btcusd_tick_trade.csv", "crypto/gdax/tick/btcusd"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Second, TickType.Trade, "20160217_trade.zip", "20160217_btcusd_second_trade.csv", "crypto/gdax/second/btcusd"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Minute, TickType.Trade, "20160217_trade.zip", "20160217_btcusd_minute_trade.csv", "crypto/gdax/minute/btcusd"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Hour, TickType.Trade, "btcusd_trade.zip", "btcusd.csv", "crypto/gdax/hour"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Daily, TickType.Trade, "btcusd_trade.zip", "btcusd.csv", "crypto/gdax/daily"),

                // Crypto - quotes
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Tick, TickType.Quote, "20160217_quote.zip", "20160217_btcusd_tick_quote.csv", "crypto/gdax/tick/btcusd"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Second, TickType.Quote, "20160217_quote.zip", "20160217_btcusd_second_quote.csv", "crypto/gdax/second/btcusd"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Minute, TickType.Quote, "20160217_quote.zip", "20160217_btcusd_minute_quote.csv", "crypto/gdax/minute/btcusd"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Hour, TickType.Quote, "btcusd_quote.zip", "btcusd.csv", "crypto/gdax/hour"),
                new LeanDataTestParameters(Symbols.BTCUSD, date, Resolution.Daily, TickType.Quote, "btcusd_quote.zip", "btcusd.csv", "crypto/gdax/daily"),

                // Futures (expiration month == contract month) - trades
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Tick, TickType.Trade, "20181210_trade.zip", "20181210_es_tick_trade_201812_20181221.csv", "future/cme/tick/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Second, TickType.Trade, "20181210_trade.zip", "20181210_es_second_trade_201812_20181221.csv", "future/cme/second/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Minute, TickType.Trade, "20181210_trade.zip", "20181210_es_minute_trade_201812_20181221.csv", "future/cme/minute/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Hour, TickType.Trade, "es_trade.zip", "es_trade_201812_20181221.csv", "future/cme/hour"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Daily, TickType.Trade, "es_trade.zip", "es_trade_201812_20181221.csv", "future/cme/daily"),

                // Futures (expiration month == contract month) - quotes
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Tick, TickType.Quote, "20181210_quote.zip", "20181210_es_tick_quote_201812_20181221.csv", "future/cme/tick/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Second, TickType.Quote, "20181210_quote.zip", "20181210_es_second_quote_201812_20181221.csv", "future/cme/second/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Minute, TickType.Quote, "20181210_quote.zip", "20181210_es_minute_quote_201812_20181221.csv", "future/cme/minute/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Hour, TickType.Quote, "es_quote.zip", "es_quote_201812_20181221.csv", "future/cme/hour"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Daily, TickType.Quote, "es_quote.zip", "es_quote_201812_20181221.csv", "future/cme/daily"),

                // Futures (expiration month == contract month) - OpenInterest
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Tick, TickType.OpenInterest, "20181210_openinterest.zip", "20181210_es_tick_openinterest_201812_20181221.csv", "future/cme/tick/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Second, TickType.OpenInterest, "20181210_openinterest.zip", "20181210_es_second_openinterest_201812_20181221.csv", "future/cme/second/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Minute, TickType.OpenInterest, "20181210_openinterest.zip", "20181210_es_minute_openinterest_201812_20181221.csv", "future/cme/minute/es"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Hour, TickType.OpenInterest, "es_openinterest.zip", "es_openinterest_201812_20181221.csv", "future/cme/hour"),
                new LeanDataTestParameters(Symbols.Future_ESZ18_Dec2018, dateFutures, Resolution.Daily, TickType.OpenInterest, "es_openinterest.zip", "es_openinterest_201812_20181221.csv", "future/cme/daily"),

                // Futures (expiration month < contract month) - trades
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Tick, TickType.Trade, "20181210_trade.zip", "20181210_cl_tick_trade_201901_20181219.csv", "future/nymex/tick/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Second, TickType.Trade, "20181210_trade.zip", "20181210_cl_second_trade_201901_20181219.csv", "future/nymex/second/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Minute, TickType.Trade, "20181210_trade.zip", "20181210_cl_minute_trade_201901_20181219.csv", "future/nymex/minute/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Hour, TickType.Trade, "cl_trade.zip", "cl_trade_201901_20181219.csv", "future/nymex/hour"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Daily, TickType.Trade, "cl_trade.zip", "cl_trade_201901_20181219.csv", "future/nymex/daily"),

                // Futures (expiration month < contract month) - quotes
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Tick, TickType.Quote, "20181210_quote.zip", "20181210_cl_tick_quote_201901_20181219.csv", "future/nymex/tick/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Second, TickType.Quote, "20181210_quote.zip", "20181210_cl_second_quote_201901_20181219.csv", "future/nymex/second/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Minute, TickType.Quote, "20181210_quote.zip", "20181210_cl_minute_quote_201901_20181219.csv", "future/nymex/minute/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Hour, TickType.Quote, "cl_quote.zip", "cl_quote_201901_20181219.csv", "future/nymex/hour"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Daily, TickType.Quote, "cl_quote.zip", "cl_quote_201901_20181219.csv", "future/nymex/daily"),

                // Futures (expiration month < contract month) - open interest
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Tick, TickType.OpenInterest, "20181210_openinterest.zip", "20181210_cl_tick_openinterest_201901_20181219.csv", "future/nymex/tick/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Second, TickType.OpenInterest, "20181210_openinterest.zip", "20181210_cl_second_openinterest_201901_20181219.csv", "future/nymex/second/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Minute, TickType.OpenInterest, "20181210_openinterest.zip", "20181210_cl_minute_openinterest_201901_20181219.csv", "future/nymex/minute/cl"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Hour, TickType.OpenInterest, "cl_openinterest.zip", "cl_openinterest_201901_20181219.csv", "future/nymex/hour"),
                new LeanDataTestParameters(Symbols.Future_CLF19_Jan2019, dateFutures, Resolution.Daily, TickType.OpenInterest, "cl_openinterest.zip", "cl_openinterest_201901_20181219.csv", "future/nymex/daily"),

            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        private static TestCaseData[] GetLeanDataLineTestParameters()
        {
            var time = new DateTime(2016, 02, 18, 9, 30, 0);
            return new List<LeanDataLineTestParameters>
            {
                //equity
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.SPY, Value = 1, Quantity = 2, TickType = TickType.Trade, Exchange = "EX", SaleCondition = "SC", Suspicious = true}, SecurityType.Equity, Resolution.Tick,
                    "34200000,10000,2,EX,SC,1"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.SPY, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(1)), SecurityType.Equity, Resolution.Minute,
                    "34200000,10000,20000,30000,40000,5"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.SPY, 1, 2, 3, 4, 5, TimeSpan.FromDays(1)), SecurityType.Equity, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5"),

                // options
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.SPY_P_192_Feb19_2016, null, 0, new Bar(6, 7, 8, 9), 10, TimeSpan.FromMinutes(1)) {Bid = null}, SecurityType.Option, Resolution.Minute,
                    "34200000,,,,,0,60000,70000,80000,90000,10"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.SPY_P_192_Feb19_2016, new Bar(1, 2, 3, 4), 5, null, 0, TimeSpan.FromDays(1)) {Ask = null}, SecurityType.Option, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5,,,,,0"),
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.SPY_P_192_Feb19_2016, new Bar(1, 2, 3, 4), 5, new Bar(6, 7, 8, 9), 10, TimeSpan.FromMinutes(1)), SecurityType.Option, Resolution.Minute,
                    "34200000,10000,20000,30000,40000,5,60000,70000,80000,90000,10"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.SPY_P_192_Feb19_2016, new Bar(1, 2, 3, 4), 5, new Bar(6, 7, 8, 9), 10, TimeSpan.FromDays(1)), SecurityType.Option, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5,60000,70000,80000,90000,10"),
                new LeanDataLineTestParameters(new Tick(time, Symbols.SPY_P_192_Feb19_2016, 0, 1, 3) {Value = 2m, TickType = TickType.Quote, BidSize = 2, AskSize = 4, Exchange = "EX", Suspicious = true}, SecurityType.Option, Resolution.Tick,
                    "34200000,10000,2,30000,4,EX,1"),
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.SPY_P_192_Feb19_2016, Value = 1, Quantity = 2,TickType = TickType.Trade, Exchange = "EX", SaleCondition = "SC", Suspicious = true}, SecurityType.Option, Resolution.Tick,
                    "34200000,10000,2,EX,SC,1"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.SPY_P_192_Feb19_2016, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(1)), SecurityType.Option, Resolution.Minute,
                    "34200000,10000,20000,30000,40000,5"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.SPY_P_192_Feb19_2016, 1, 2, 3, 4, 5, TimeSpan.FromDays(1)), SecurityType.Option, Resolution.Daily,
                    "20160218 00:00,10000,20000,30000,40000,5"),

                // forex
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.EURUSD, BidPrice = 1, Value =1.5m, AskPrice = 2, TickType = TickType.Quote}, SecurityType.Forex, Resolution.Tick,
                    "34200000,1,2"),
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.EURUSD, new Bar(1, 2, 3, 4), 0, new Bar(1, 2, 3, 4), 0, TimeSpan.FromMinutes(1)), SecurityType.Forex, Resolution.Minute, "34200000,1,2,3,4,0,1,2,3,4,0"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.EURUSD, new Bar(1, 2, 3, 4), 0, new Bar(1, 2, 3, 4), 0, TimeSpan.FromDays(1)), SecurityType.Forex, Resolution.Daily,
                    "20160218 00:00,1,2,3,4,0,1,2,3,4,0"),

                // cfd
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.DE10YBEUR, BidPrice = 1, Value = 1.5m, AskPrice = 2, TickType = TickType.Quote}, SecurityType.Cfd, Resolution.Tick,
                    "34200000,1,2"),
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.DE10YBEUR, new Bar(1, 2, 3, 4), 0, new Bar(1, 2, 3, 4), 0, TimeSpan.FromMinutes(1)), SecurityType.Cfd, Resolution.Minute,
                    "34200000,1,2,3,4,0,1,2,3,4,0"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.DE10YBEUR, new Bar(1, 2, 3, 4), 0, new Bar(1, 2, 3, 4), 0, TimeSpan.FromDays(1)), SecurityType.Cfd, Resolution.Daily,
                    "20160218 00:00,1,2,3,4,0,1,2,3,4,0"),

                // crypto - trades
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.BTCUSD, null, 0, new Bar(6, 7, 8, 9), 10, TimeSpan.FromMinutes(1)) {Bid = null}, SecurityType.Crypto, Resolution.Minute,
                    "34200000,,,,,0,6,7,8,9,10"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.BTCUSD, new Bar(1, 2, 3, 4), 5, null, 0, TimeSpan.FromDays(1)) {Ask = null}, SecurityType.Crypto, Resolution.Daily,
                    "20160218 00:00,1,2,3,4,5,,,,,0"),
                new LeanDataLineTestParameters(new QuoteBar(time, Symbols.BTCUSD, new Bar(1, 2, 3, 4), 5, new Bar(6, 7, 8, 9), 10, TimeSpan.FromMinutes(1)), SecurityType.Crypto, Resolution.Minute,
                    "34200000,1,2,3,4,5,6,7,8,9,10"),
                new LeanDataLineTestParameters(new QuoteBar(time.Date, Symbols.BTCUSD, new Bar(1, 2, 3, 4), 5, new Bar(6, 7, 8, 9), 10, TimeSpan.FromDays(1)), SecurityType.Crypto, Resolution.Daily,
                    "20160218 00:00,1,2,3,4,5,6,7,8,9,10"),
                new LeanDataLineTestParameters(new Tick(time, Symbols.BTCUSD, 0, 1, 3) {Value = 2m, TickType = TickType.Quote, BidSize = 2, AskSize = 4, Exchange = "gdax", Suspicious = false}, SecurityType.Crypto, Resolution.Tick,
                    "34200000,1,2,3,4"),
                new LeanDataLineTestParameters(new Tick {Time = time, Symbol = Symbols.BTCUSD, Value = 1, Quantity = 2,TickType = TickType.Trade, Exchange = "gdax", Suspicious = false}, SecurityType.Crypto, Resolution.Tick,
                    "34200000,1,2"),
                new LeanDataLineTestParameters(new TradeBar(time, Symbols.BTCUSD, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(1)), SecurityType.Crypto, Resolution.Minute,
                    "34200000,1,2,3,4,5"),
                new LeanDataLineTestParameters(new TradeBar(time.Date, Symbols.BTCUSD, 1, 2, 3, 4, 5, TimeSpan.FromDays(1)), SecurityType.Crypto, Resolution.Daily,
                    "20160218 00:00,1,2,3,4,5"),

            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        public class LeanDataTestParameters
        {
            public readonly string Name;
            public readonly Symbol Symbol;
            public readonly DateTime Date;
            public readonly Resolution Resolution;
            public readonly TickType TickType;
            public readonly Type BaseDataType;
            public readonly SubscriptionDataConfig Config;
            public readonly string ExpectedZipFileName;
            public readonly string ExpectedZipEntryName;
            public readonly string ExpectedRelativeZipFilePath;
            public readonly string ExpectedZipFilePath;
            public SecurityType SecurityType { get { return Symbol.ID.SecurityType; } }

            public LeanDataTestParameters(Symbol symbol, DateTime date, Resolution resolution, TickType tickType, string expectedZipFileName, string expectedZipEntryName, string expectedRelativeZipFileDirectory = "")
            {
                Symbol = symbol;
                Date = date;
                Resolution = resolution;
                TickType = tickType;
                ExpectedZipFileName = expectedZipFileName;
                ExpectedZipEntryName = expectedZipEntryName;
                ExpectedRelativeZipFilePath = Path.Combine(expectedRelativeZipFileDirectory, expectedZipFileName).Replace("/", Path.DirectorySeparatorChar.ToStringInvariant());
                ExpectedZipFilePath = Path.Combine(Globals.DataFolder, ExpectedRelativeZipFilePath);

                Name = SecurityType + "_" + resolution + "_" + symbol.Value + "_" + tickType;

                BaseDataType = resolution == Resolution.Tick ? typeof(Tick) : typeof(TradeBar);
                if (symbol.ID.SecurityType == SecurityType.Option && resolution != Resolution.Tick)
                {
                    BaseDataType = typeof(QuoteBar);
                }
                Config = new SubscriptionDataConfig(BaseDataType, symbol, resolution, TimeZones.NewYork, TimeZones.NewYork, true, false, false, false, tickType);
            }
        }

        public class LeanDataLineTestParameters
        {
            public readonly string Name;
            public readonly BaseData Data;
            public readonly SecurityType SecurityType;
            public readonly Resolution Resolution;
            public readonly string ExpectedLine;
            public readonly SubscriptionDataConfig Config;
            public readonly TickType TickType;

            public LeanDataLineTestParameters(BaseData data, SecurityType securityType, Resolution resolution, string expectedLine)
            {
                Data = data;
                SecurityType = securityType;
                Resolution = resolution;
                ExpectedLine = expectedLine;
                if (data is Tick)
                {
                    var tick = (Tick) data;
                    TickType = tick.TickType;
                }
                else if (data is TradeBar)
                {
                    TickType = TickType.Trade;
                }
                else if (data is QuoteBar)
                {
                    TickType = TickType.Quote;
                }
                else
                {
                    throw new NotImplementedException();
                }

                // override for forex/cfd
                if (data.Symbol.ID.SecurityType == SecurityType.Forex || data.Symbol.ID.SecurityType == SecurityType.Cfd)
                {
                    TickType = TickType.Quote;
                }

                Config = new SubscriptionDataConfig(Data.GetType(), Data.Symbol, Resolution, TimeZones.Utc, TimeZones.Utc, false, true, false, false, TickType);

                Name = SecurityType + "_" + data.GetType().Name;

                if (data.GetType() != typeof (Tick) || Resolution != Resolution.Tick)
                {
                    Name += "_" + Resolution;
                }

                if (data is Tick)
                {
                    Name += "_" + ((Tick) data).TickType;
                }
            }
        }
    }
}
