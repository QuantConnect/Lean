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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Util;
using System;
using QuantConnect.Data.Market;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.Setup;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class AnnualPerformanceTests
    {
        private List<TradeBar> _spy = new List<TradeBar>();

        /// <summary>
        /// Instance of QC Algorithm. 
        /// Use to get <see cref="Interfaces.IAlgorithmSettings.TradingDaysPerYear"/> for clear calculation in <seealso cref="QuantConnect.Statistics.Statistics.AnnualPerformance"/>
        /// </summary>
        private QCAlgorithm _algorithm;

        [SetUp]
        public void GetSPY()
        {
            _algorithm = new QCAlgorithm();
            BaseSetupHandler.SetBrokerageTradingDayPerYear(_algorithm);

            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var path = LeanData.GenerateZipFilePath(Globals.DataFolder, symbol, new DateTime(2020, 3, 1), Resolution.Daily, TickType.Trade);
            var config = new QuantConnect.Data.SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);

            foreach (var line in QuantConnect.Compression.ReadLines(path))
            {
                var bar = TradeBar.ParseEquity(config, line, DateTime.Now.Date);
                _spy.Add(bar);
            }
        }

        [TearDown]
        public void Delete()
        {
            _spy.Clear();
        }

        [Test]
        public void TotalMarketPerformance()
        {
            var performance = new List<double>();

            for (var i = 1; i < _spy.Count(); i++)
            {
                performance.Add((double)((_spy[i].Close / _spy[i - 1].Close) - 1));
            }

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, _algorithm.Settings.TradingDaysPerYear.Value);

            Assert.AreEqual(0.082859685889996371, result);
        }

        [Test]
        public void BearMarketPerformance()
        {
            var performance = new List<double>();
            var start = new DateTime(2008, 5, 1);
            var end = new DateTime(2009, 1, 1);
            for (var i = 1; i < _spy.Count(); i++)
            {
                if ((_spy[i].EndTime < start) || (_spy[i].EndTime > end))
                {
                    continue;
                }
                performance.Add((double)((_spy[i].Close / _spy[i - 1].Close) - 1));
            }

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, _algorithm.Settings.TradingDaysPerYear.Value);

            Assert.AreEqual(-0.41546561808009674, result);
        }

        [Test]
        public void BullMarketPerformance()
        {
            var performance = new List<double>();
            var start = new DateTime(2017, 1, 1);
            var end = new DateTime(2018, 1, 1);
            for (var i = 1; i < _spy.Count(); i++)
            {
                if ((_spy[i].EndTime < start) || (_spy[i].EndTime > end))
                {
                    continue;
                }
                performance.Add((double)((_spy[i].Close / _spy[i - 1].Close) - 1));
            }

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, _algorithm.Settings.TradingDaysPerYear.Value);

            Assert.AreEqual(0.19741738320179447, result);
        }

        [Test]
        public void FullYearPerformance()
        {
            // Ensure mean is 1
            var performance = Enumerable.Repeat(0.5, 176).ToList();
            performance.AddRange(Enumerable.Repeat(1.5, 176).ToList());

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, 4);

            Assert.AreEqual(15.0, result);
        }

        [Test]
        public void AllZeros()
        {
            var performance = Enumerable.Repeat(0.0, 252).ToList();

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, _algorithm.Settings.TradingDaysPerYear.Value);

            Assert.AreEqual(0.0, result);
        }
    }
}