﻿/*
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
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class FactorFileTests
    {
        [Test]
        public void ReadsFactorFileWithoutInfValues()
        {
            var factorFile = FactorFile.Read("AAPL", "usa");

            Assert.AreEqual(29, factorFile.SortedFactorFileData.Count);

            Assert.AreEqual(new DateTime(1998, 01, 01), factorFile.FactorFileMinimumDate.Value);
        }

        [Test]
        public void ReadsFactorFileWithExponentialNotation()
        {
            // Source NEWL factor file at 2019-12-09
            var lines = new[]
            {
                "19980102,0.8116779,1e+07",
                "20051108,0.8116779,1e+07",
                "20060217,0.8416761,1e+07",
                "20060516,0.8644420,1e+07",
                "20060814,0.8747766,1e+07",
                "20061115,0.8901232,1e+07",
                "20070314,0.9082148,1e+07",
                "20070522,0.9166239,1e+07",
                "20070814,0.9306799,1e+07",
                "20071120,0.9534326,1e+07",
                "20080520,0.9830510,1e+07",
                "20100802,1.0000000,1e+07",
                "20131016,1.0000000,1.11111e+06",
                "20131205,1.0000000,75188",
                "20140305,1.0000000,25000",
                "20140514,1.0000000,2500",
                "20140714,1.0000000,50",
                "20501231,1.0000000,1"
            };

            DateTime? factorFileMinimumDate;
            var factorFile = FactorFileRow.Parse(lines, out factorFileMinimumDate).ToList();

            Assert.AreEqual(5, factorFile.Count);

            Assert.IsNotNull(factorFileMinimumDate);
            Assert.AreEqual(new DateTime(2013, 12, 04), factorFileMinimumDate.Value);
        }

        [Test]
        public void ReadsFactorFileWithInfValues()
        {
            var lines = new[]
            {
                "19980102,1.0000000,inf",
                "20151211,1.0000000,inf",
                "20160330,1.0000000,2500",
                "20160915,1.0000000,80",
                "20501231,1.0000000,1"
            };

            DateTime? factorFileMinimumDate;
            var factorFile = FactorFileRow.Parse(lines, out factorFileMinimumDate).ToList();

            Assert.AreEqual(3, factorFile.Count);

            Assert.IsNotNull(factorFileMinimumDate);
            Assert.AreEqual(new DateTime(2016, 3, 29), factorFileMinimumDate.Value);
        }

        [Test]
        public void CorrectlyDeterminesTimePriceFactors()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            var file = GetTestFactorFile(symbol, reference);

            // time price factors should be the price factor * split factor

            Assert.AreEqual(1, file.GetPriceScaleFactor(reference));
            Assert.AreEqual(1, file.GetPriceScaleFactor(reference.AddDays(-6)));
            Assert.AreEqual(.9, file.GetPriceScaleFactor(reference.AddDays(-7)));
            Assert.AreEqual(.9, file.GetPriceScaleFactor(reference.AddDays(-13)));
            Assert.AreEqual(.8, file.GetPriceScaleFactor(reference.AddDays(-14)));
            Assert.AreEqual(.8, file.GetPriceScaleFactor(reference.AddDays(-20)));
            Assert.AreEqual(.8m * .5m, file.GetPriceScaleFactor(reference.AddDays(-21)));
            Assert.AreEqual(.8m * .5m, file.GetPriceScaleFactor(reference.AddDays(-22)));
            Assert.AreEqual(.8m * .5m, file.GetPriceScaleFactor(reference.AddDays(-89)));
            Assert.AreEqual(.8m * .25m, file.GetPriceScaleFactor(reference.AddDays(-91)));
        }

        [Test]
        public void HasDividendEventOnNextTradingDay()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            decimal priceFactorRatio;
            decimal referencePrice;
            var file = GetTestFactorFile(symbol, reference);

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference, out priceFactorRatio, out referencePrice));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-6), out priceFactorRatio, out referencePrice));
            Assert.IsTrue(file.HasDividendEventOnNextTradingDay(reference.AddDays(-7), out priceFactorRatio, out referencePrice));
            Assert.AreEqual(.9m/1m, priceFactorRatio);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-8), out priceFactorRatio, out referencePrice));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-13), out priceFactorRatio, out referencePrice));
            Assert.IsTrue(file.HasDividendEventOnNextTradingDay(reference.AddDays(-14), out priceFactorRatio, out referencePrice));
            Assert.AreEqual(.8m / .9m, priceFactorRatio);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-15), out priceFactorRatio, out referencePrice));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-364), out priceFactorRatio, out referencePrice));
            Assert.IsTrue(file.HasDividendEventOnNextTradingDay(reference.AddDays(-365), out priceFactorRatio, out referencePrice));
            Assert.AreEqual(.7m / .8m, priceFactorRatio);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-366), out priceFactorRatio, out referencePrice));

            Assert.IsNull(file.FactorFileMinimumDate);
        }

        [Test]
        public void HasSplitEventOnNextTradingDay()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            decimal splitFactor;
            decimal referencePrice;
            var file = GetTestFactorFile(symbol, reference);

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference, out splitFactor, out referencePrice));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-20), out splitFactor, out referencePrice));
            Assert.IsTrue(file.HasSplitEventOnNextTradingDay(reference.AddDays(-21), out splitFactor, out referencePrice));
            Assert.AreEqual(.5, splitFactor);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-22), out splitFactor, out referencePrice));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-89), out splitFactor, out referencePrice));
            Assert.IsTrue(file.HasSplitEventOnNextTradingDay(reference.AddDays(-90), out splitFactor, out referencePrice));
            Assert.AreEqual(.5, splitFactor);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-91), out splitFactor, out referencePrice));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-364), out splitFactor, out referencePrice));
            Assert.IsTrue(file.HasSplitEventOnNextTradingDay(reference.AddDays(-365), out splitFactor, out referencePrice));
            Assert.AreEqual(.5, splitFactor);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-366), out splitFactor, out referencePrice));

            Assert.IsNull(file.FactorFileMinimumDate);
        }

        [Test]
        public void GeneratesCorrectSplitsAndDividends()
        {
            var reference = new DateTime(2018, 01, 01);
            var file = GetTestFactorFile("SPY", reference);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(QuantConnect.Market.USA, Symbols.SPY, SecurityType.Equity);
            var splitsAndDividends = file.GetSplitsAndDividends(Symbols.SPY, exchangeHours);

            var dividend = (Dividend)splitsAndDividends.Single(d => d.Time == reference.AddDays(-6));
            var distribution = Dividend.ComputeDistribution(100m, .9m / 1m, 2);
            Assert.AreEqual(distribution, dividend.Distribution);

            dividend = (Dividend) splitsAndDividends.Single(d => d.Time == reference.AddDays(-13));
            distribution = Math.Round(Dividend.ComputeDistribution(100m, .8m / .9m, 2), 2);
            Assert.AreEqual(distribution, dividend.Distribution);

            var split = (Split) splitsAndDividends.Single(d => d.Time == reference.AddDays(-20));
            var splitFactor = .5m;
            Assert.AreEqual(splitFactor, split.SplitFactor);

            split = (Split) splitsAndDividends.Single(d => d.Time == reference.AddDays(-89));
            splitFactor = .5m;
            Assert.AreEqual(splitFactor, split.SplitFactor);

            dividend = splitsAndDividends.OfType<Dividend>().Single(d => d.Time == reference.AddDays(-363));
            distribution = Dividend.ComputeDistribution(100m, .7m / .8m, 2);
            Assert.AreEqual(distribution, dividend.Distribution);

            split = splitsAndDividends.OfType<Split>().Single(d => d.Time == reference.AddDays(-363));
            splitFactor = .5m;
            Assert.AreEqual(splitFactor, split.SplitFactor);
        }

        [Test]
        public void GetsSplitsAndDividends()
        {
            var factorFile = GetFactorFile_AAPL2018_05_11();
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(QuantConnect.Market.USA, Symbols.SPY, SecurityType.Equity);
            var splitsAndDividends = factorFile.GetSplitsAndDividends(Symbols.AAPL, exchangeHours).ToList();
            foreach (var sad in splitsAndDividends)
            {
                Log.Trace($"{sad.Time.Date:yyyy-MM-dd}: {sad}");
            }
            var splits = splitsAndDividends.OfType<Split>().ToList();
            var dividends = splitsAndDividends.OfType<Dividend>().ToList();

            var dividend = dividends.Single(d => d.Time == new DateTime(2018, 05, 11));
            Assert.AreEqual(0.73m, dividend.Distribution.RoundToSignificantDigits(6));

            var split = splits.Single(d => d.Time == new DateTime(2014, 06, 09));
            Assert.AreEqual((1/7m).RoundToSignificantDigits(6), split.SplitFactor);
        }

        [Test]
        public void AppliesDividend()
        {
            var factorFileBeforeDividend = GetFactorFile_AAPL2018_05_08();
            var factorFileAfterDividend = GetFactorFile_AAPL2018_05_11();
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(QuantConnect.Market.USA, Symbols.SPY, SecurityType.Equity);

            var dividend = new Dividend(Symbols.AAPL, new DateTime(2018, 05, 11), 0.73m, 190.03m);
            var actual = factorFileBeforeDividend.Apply(new List<BaseData> {dividend}, exchangeHours);

            Assert.AreEqual(factorFileAfterDividend.Count(), actual.Count());
            Assert.True(actual.First().Date == new DateTime(1998, 01, 02),
                $"Factor file first row changed from 1998-01-02 to {actual.First().Date:yyyy-MM-dd} after applying new event");


            foreach (var item in factorFileAfterDividend.Reverse().Zip(actual.Reverse(), (a,e) => new{actual=a, expected=e}))
            {
                Log.Trace($"expected: {item.expected} actual: {item.actual}  diff: {100* (1 - item.actual.PriceFactor/item.expected.PriceFactor):0.0000}%");
                Assert.AreEqual(item.expected.Date, item.actual.Date);
                Assert.AreEqual(item.expected.ReferencePrice, item.actual.ReferencePrice);
                Assert.AreEqual(item.expected.SplitFactor, item.actual.SplitFactor);

                var delta = (double)item.expected.PriceFactor * 1e-5;
                Assert.AreEqual((double)item.expected.PriceFactor, (double)item.actual.PriceFactor, delta);
            }
        }


        [Test]
        public void AppliesSplit()
        {
            var factorFileBeforeSplit = GetFactorFile_LODE20191127();
            var factorFileAfterSplit = GetFactorFile_LODE20191129();
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(QuantConnect.Market.USA, Symbols.SPY, SecurityType.Equity);

            var eventTime = new DateTime(2019, 11, 29);
            var split = new Split(Symbols.LODE, eventTime, 0.06m, 5, SplitType.SplitOccurred);
            var actual = factorFileBeforeSplit.Apply(new List<BaseData> { split }, exchangeHours);

            Assert.AreEqual(factorFileAfterSplit.Count(), actual.Count());
            Assert.True(actual.First().Date == new DateTime(1998, 01, 02),
                $"Factor file first row changed from 1998-01-02 to {actual.First().Date:yyyy-MM-dd} after applying new event");
            Assert.True(actual.First().SplitFactor == 25m, "Factor File split factor is not computed correctly");
            foreach (var item in actual.Reverse().Zip(factorFileAfterSplit.Reverse(), (a, e) => new { actual = a, expected = e }))
            {
                Log.Trace($"expected: {item.expected} actual: {item.actual}  diff: {100 * (1 - item.actual.PriceFactor / item.expected.PriceFactor):0.0000}%");
                Assert.AreEqual(item.expected.Date, item.actual.Date);
                Assert.AreEqual(item.expected.ReferencePrice, item.actual.ReferencePrice);
                Assert.AreEqual(item.expected.SplitFactor, item.actual.SplitFactor);

                var delta = (double)item.expected.PriceFactor * 1e-5;
                Assert.AreEqual((double)item.expected.PriceFactor, (double)item.actual.PriceFactor, delta);
            }
        }


        [Test]
        public void CanHandleRepeatedEventsCorrectly()
        {
            var factorFileBeforeSplit = GetFactorFile_LODE20191127();
            var factorFileAfterSplit = GetFactorFile_LODE20191129();
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(QuantConnect.Market.USA, Symbols.SPY, SecurityType.Equity);

            var eventTime = new DateTime(2019, 11, 29);
            var split = new Split(Symbols.LODE, eventTime, 0.06m, 5, SplitType.SplitOccurred);
            var events = new List<BaseData> { split, split, split };
            var actual = factorFileBeforeSplit.Apply(events, exchangeHours);

            Assert.AreEqual(factorFileAfterSplit.Count(), actual.Count());
            Assert.True(actual.First().Date == new DateTime(1998, 01, 02),
                $"Factor file first row changed from 1998-01-02 to {actual.First().Date:yyyy-MM-dd} after applying new event");
            Assert.True(actual.First().SplitFactor == 25m, "Factor File split factor is not computed correctly");
            foreach (var item in actual.Reverse().Zip(factorFileAfterSplit.Reverse(), (a, e) => new { actual = a, expected = e }))
            {
                Log.Trace($"expected: {item.expected} actual: {item.actual}  diff: {100 * (1 - item.actual.PriceFactor / item.expected.PriceFactor):0.0000}%");
                Assert.AreEqual(item.expected.Date, item.actual.Date);
                Assert.AreEqual(item.expected.ReferencePrice, item.actual.ReferencePrice);
                Assert.AreEqual(item.expected.SplitFactor, item.actual.SplitFactor);

                var delta = (double)item.expected.PriceFactor * 1e-5;
                Assert.AreEqual((double)item.expected.PriceFactor, (double)item.actual.PriceFactor, delta);
            }
        }


        [Test]
        public void AppliesSplitAndDividendAtSameTime()
        {
            var reference = new DateTime(2018, 08, 01);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(QuantConnect.Market.USA, Symbols.SPY, SecurityType.Equity);
            var expected = GetTestFactorFile("AAPL", reference);

            // remove the last entry that contains a split and dividend at the same time
            var factorFile = new FactorFile("AAPL", expected.SortedFactorFileData.Where(kvp => kvp.Value.PriceFactor >= .8m).Select(kvp => kvp.Value));
            var actual = factorFile.Apply(new List<BaseData>
            {
                new Split(Symbols.AAPL, reference.AddDays(-364), 100m, 1 / 2m, SplitType.SplitOccurred),
                new Dividend(Symbols.AAPL, reference.AddDays(-364), 12.5m, 100m)
            }, exchangeHours);

            foreach (var item in actual.Reverse().Zip(expected.Reverse(), (a, e) => new {actual = a, expected = e}))
            {
                Log.Trace($"expected: {item.expected} actual: {item.actual}  diff: {100 * (1 - item.actual.PriceFactor / item.expected.PriceFactor):0.0000}%");
                Assert.AreEqual(item.expected.Date, item.actual.Date);
                Assert.AreEqual(item.expected.ReferencePrice, item.actual.ReferencePrice);
                Assert.AreEqual(item.expected.SplitFactor, item.actual.SplitFactor);

                Assert.AreEqual(item.expected.PriceFactor.RoundToSignificantDigits(4), item.actual.PriceFactor.RoundToSignificantDigits(4));
            }
        }

        [Test]
        public void ReadsOldFactorFileFormat()
        {
            var lines = new[]
            {
                "19980102,1.0000000,0.5",
                "20130828,1.0000000,0.5",
                "20501231,1.0000000,1"
            };

            var factorFile = FactorFile.Parse("bno", lines);

            var firstRow = factorFile.SortedFactorFileData[new DateTime(1998, 01, 02)];
            Assert.AreEqual(1m, firstRow.PriceFactor);
            Assert.AreEqual(0.5m, firstRow.SplitFactor);
            Assert.AreEqual(0m, firstRow.ReferencePrice);

            var secondRow = factorFile.SortedFactorFileData[new DateTime(2013, 08, 28)];
            Assert.AreEqual(1m, secondRow.PriceFactor);
            Assert.AreEqual(0.5m, secondRow.SplitFactor);
            Assert.AreEqual(0m, firstRow.ReferencePrice);

            var thirdRow = factorFile.SortedFactorFileData[Time.EndOfTime];
            Assert.AreEqual(1m, thirdRow.PriceFactor);
            Assert.AreEqual(1m, thirdRow.SplitFactor);
            Assert.AreEqual(0m, firstRow.ReferencePrice);
        }

        [Test]
        public void ResolvesCorrectMostRecentFactorChangeDate()
        {
            var lines = new[]
            {
                "19980102,1.0000000,0.5",
                "20130828,1.0000000,0.5",
                "20501231,1.0000000,1"
            };

            var factorFile = FactorFile.Parse("bno", lines);
            Assert.AreEqual(new DateTime(2013, 08, 28), factorFile.MostRecentFactorChange);
        }

        [Test]
        [TestCase("")]
        [TestCase("20501231,1.0000000,1")]
        public void EmptyFactorFileReturnsEmptyListForSplitsAndDividends(string contents)
        {
            var lines = contents.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));

            var factorFile = FactorFile.Parse("bno", lines);
            Assert.IsEmpty(factorFile.GetSplitsAndDividends(Symbols.SPY, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork)));
        }

        private static FactorFile GetTestFactorFile(string symbol, DateTime reference)
        {
            var file = new FactorFile(symbol, new List<FactorFileRow>
            {
                new FactorFileRow(reference, 1, 1),
                new FactorFileRow(reference.AddDays(-7), .9m, 1, 100m),       // dividend
                new FactorFileRow(reference.AddDays(-14), .8m, 1, 100m),      // dividend
                new FactorFileRow(reference.AddDays(-21), .8m, .5m, 100m),    // split
                new FactorFileRow(reference.AddDays(-90), .8m, .25m, 100m),   // split
                new FactorFileRow(reference.AddDays(-365), .7m, .125m, 100m)  // split+dividend
            });
            return file;
        }

        private static FactorFile GetFactorFile(string permtick)
        {
            return FactorFile.Read(permtick, QuantConnect.Market.USA);
        }

        private static FactorFile GetFactorFile_LODE20191127()
        {
            const string factorFileContents = @"
19980102,1,5,8.5,qq
20171109,1,5,0.12,qq
20501231,1,1,0,qq
";
            DateTime? factorFileMinimumDate;
            var reader = new StreamReader(factorFileContents.ToStream());
            var enumerable = new StreamReaderEnumerable(reader).Where(line => line.Length > 0);
            var factorFileRows = FactorFileRow.Parse(enumerable, out factorFileMinimumDate);
            return new FactorFile("lode", factorFileRows, factorFileMinimumDate);
        }

        private static FactorFile GetFactorFile_LODE20191129()
        {
            const string factorFileContents = @"
19980102,1,25,8.5,qq
20171109,1,25,0.12,qq
20191127,1,5,0.06,qq
20501231,1,1,0,qq
";
            DateTime? factorFileMinimumDate;
            var reader = new StreamReader(factorFileContents.ToStream());
            var enumerable = new StreamReaderEnumerable(reader).Where(line => line.Length > 0);
            var factorFileRows = FactorFileRow.Parse(enumerable, out factorFileMinimumDate);
            return new FactorFile("lode", factorFileRows, factorFileMinimumDate);
        }

        private static FactorFile GetFactorFile_AAPL2018_05_11()
        {
            const string factorFileContents = @"
19980102,0.8893653,0.0357143,16.25
20000620,0.8893653,0.0357143,101
20050225,0.8893653,0.0714286,88.97
20120808,0.8893653,0.142857,619.85
20121106,0.8931837,0.142857,582.85
20130206,0.8972636,0.142857,457.285
20130508,0.9024937,0.142857,463.71
20130807,0.908469,0.142857,464.94
20131105,0.9144679,0.142857,525.58
20140205,0.9198056,0.142857,512.59
20140507,0.9253111,0.142857,592.34
20140606,0.9304792,0.142857,645.57
20140806,0.9304792,1,94.96
20141105,0.9351075,1,108.86
20150204,0.9391624,1,119.55
20150506,0.9428692,1,125.085
20150805,0.9468052,1,115.4
20151104,0.9510909,1,122.01
20160203,0.9551617,1,96.34
20160504,0.9603451,1,94.19
20160803,0.9661922,1,105.8
20161102,0.9714257,1,111.6
20170208,0.9764128,1,132.04
20170510,0.9806461,1,153.26
20170809,0.9846939,1,161.1
20171109,0.9885598,1,175.87
20180208,0.9921138,1,155.16
20180510,0.9961585,1,190.03
20501231,1,1,0
";

            DateTime? factorFileMinimumDate;
            var reader = new StreamReader(factorFileContents.ToStream());
            var enumerable = new StreamReaderEnumerable(reader).Where(line => line.Length > 0);
            var factorFileRows = FactorFileRow.Parse(enumerable, out factorFileMinimumDate);
            return new FactorFile("aapl", factorFileRows, factorFileMinimumDate);
        }

        // AAPL experiences a 0.73 dividend distribution on 2018.05.11
        private static FactorFile GetFactorFile_AAPL2018_05_08()
        {
            const string factorFileContents = @"
19980102,0.8927948,0.0357143,16.25
20000620,0.8927948,0.0357143,101
20050225,0.8927948,0.0714286,88.97
20120808,0.8927948,0.142857,619.85
20121106,0.8966279,0.142857,582.85
20130206,0.9007235,0.142857,457.285
20130508,0.9059737,0.142857,463.71
20130807,0.9119721,0.142857,464.94
20131105,0.9179942,0.142857,525.58
20140205,0.9233525,0.142857,512.59
20140507,0.9288793,0.142857,592.34
20140606,0.9340673,0.142857,645.57
20140806,0.9340673,1,94.96
20141105,0.9387135,1,108.86
20150204,0.942784,1,119.55
20150506,0.9465051,1,125.085
20150805,0.9504563,1,115.4
20151104,0.9547586,1,122.01
20160203,0.9588451,1,96.34
20160504,0.9640485,1,94.19
20160803,0.9699181,1,105.8
20161102,0.9751718,1,111.6
20170208,0.9801781,1,132.04
20170510,0.9844278,1,153.26
20170809,0.9884911,1,161.1
20171109,0.992372,1,175.87
20180208,0.9959397,1,155.16
20501231,1,1,0
";

            DateTime? factorFileMinimumDate;
            var reader = new StreamReader(factorFileContents.ToStream());
            var enumerable = new StreamReaderEnumerable(reader).Where(line => line.Length > 0);
            var factorFileRows = FactorFileRow.Parse(enumerable, out factorFileMinimumDate);
            return new FactorFile("aapl", factorFileRows, factorFileMinimumDate);
        }
    }
}
