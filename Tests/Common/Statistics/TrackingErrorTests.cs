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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Util;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class TrackingErrorTests
    {
        private List<TradeBar> _spy = new List<TradeBar>();
        private List<TradeBar> _aapl = new List<TradeBar>();
        private List<double> _spyPerformance = new List<double>();
        private List<double> _aaplPerformance = new List<double>();

        [OneTimeSetUp]
        public void GetData()
        {
            var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var spyPath = LeanData.GenerateZipFilePath(Globals.DataFolder, spy, new DateTime(2020, 3, 1), Resolution.Daily, TickType.Trade);
            var spyConfig = new QuantConnect.Data.SubscriptionDataConfig(typeof(TradeBar), spy, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
            var endDate = new DateTime(2020, 3, 8);

            foreach (var line in QuantConnect.Compression.ReadLines(spyPath))
            {
                var bar = TradeBar.ParseEquity(spyConfig, line, DateTime.Now.Date);
                if (bar.EndTime < endDate)
                {
                    _spy.Add(bar);
                }
            }

            for (var i = 1; i < _spy.Count(); i++)
            {
                _spyPerformance.Add((double)((_spy[i].Close / _spy[i - 1].Close) - 1));
            }

            var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var aaplPath = LeanData.GenerateZipFilePath(Globals.DataFolder, aapl, new DateTime(2020, 3, 1), Resolution.Daily, TickType.Trade);
            var aaplConfig = new QuantConnect.Data.SubscriptionDataConfig(typeof(TradeBar), aapl, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);

            foreach (var line in QuantConnect.Compression.ReadLines(aaplPath))
            {
                var bar = TradeBar.ParseEquity(aaplConfig, line, DateTime.Now.Date);
                if (bar.EndTime < endDate)
                {
                    _aapl.Add(bar);
                }
            }

            for (var i = 1; i < _aapl.Count(); i++)
            {
                _aaplPerformance.Add((double)((_aapl[i].Close / _aapl[i - 1].Close) - 1));
            }
        }

        [OneTimeTearDown]
        public void Delete()
        {
            _spy.Clear();
            _aapl.Clear();
            _spyPerformance.Clear();
            _aaplPerformance.Clear();
        }

        [Test]
        public void OneYearPerformance()
        {
            var result = QuantConnect.Statistics.Statistics.TrackingError(_aaplPerformance.Take(252).ToList(), _spyPerformance.Take(252).ToList());

            Assert.AreEqual(0.52780899407691173, result);
        }

        [Test]
        public void TotalPerformance()
        {
            // This might seem arbitrary, but there's 1 missing date vs. AAPL for SPY data, and it happens to be at line 5555 for date 2020-01-31
            var result = QuantConnect.Statistics.Statistics.TrackingError(_aaplPerformance.Take(5555).ToList(), _spyPerformance.Take(5555).ToList());

            Assert.AreEqual(0.43115365020121948, result, 0.00001);
        }

        [Test]
        public void IdenticalPerformance()
        {
            var random = new Random();

            var benchmarkPerformance = Enumerable.Repeat(random.NextDouble(), 252).ToList();
            var algoPerformance = benchmarkPerformance.Select(element => element).ToList();

            var result = QuantConnect.Statistics.Statistics.TrackingError(algoPerformance, benchmarkPerformance);

            Assert.AreEqual(0.0, result);
        }

        [Test]
        public void DifferentPerformance()
        {
            var benchmarkPerformance = new List<double>();
            var algoPerformance = new List<double>();

            // Gives us two sequences whose difference is always -175
            // This sequence will have variance 0
            var baseReturn = -176;
            for (var i = 1; i <= 252; i++)
            {
                benchmarkPerformance.Add(baseReturn + 1);
                algoPerformance.Add((baseReturn * 2) + 2);
            }

            var result = QuantConnect.Statistics.Statistics.TrackingError(algoPerformance, benchmarkPerformance);

            Assert.AreEqual(0.0, result);
        }

        [Test]
        public void AllZeros()
        {
            var benchmarkPerformance = Enumerable.Repeat(0.0, 252).ToList();
            var algoPerformance = Enumerable.Repeat(0.0, 252).ToList();

            var result = QuantConnect.Statistics.Statistics.TrackingError(algoPerformance, benchmarkPerformance);

            Assert.AreEqual(0.0, result);
        }
    }
}
