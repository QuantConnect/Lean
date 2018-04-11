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

using NUnit.Framework;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides a framework for testing alpha models. 
    /// </summary>
    public abstract class CommonAlphaModelTests
    {
        private QCAlgorithmFramework _algorithm;
        private Security _security;

        [TestFixtureSetUp]
        public void Initialize()
        {
            var pythonPath = new DirectoryInfo("../../../Algorithm.Framework/Alphas");
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath.FullName);

            _algorithm = new QCAlgorithmFramework();
            _algorithm.PortfolioConstruction = new NullPortfolioConstructionModel();
            _algorithm.HistoryProvider = new SineHistoryProvider(_algorithm.Securities);
            _algorithm.SetStartDate(2018, 1, 4);
            _security = _algorithm.AddEquity(Symbols.SPY.Value, Resolution.Daily);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void InsightsGenerationTest(Language language)
        {
            IAlphaModel model;
            if (!TryCreateModel(language, out model))
            {
                Assert.Ignore($"Ignore {GetType().Name}: Could not create {language} model.");
            }

            // Set the alpha model
            _algorithm.SetAlpha(model);

            var changes = new SecurityChanges(AddedSecurities, RemovedSecurities);
            _algorithm.OnFrameworkSecuritiesChanged(changes);

            var actualInsights = new List<Insight>();
            _algorithm.InsightsGenerated += (s, e) => actualInsights.AddRange(e.Insights);

            var expectedInsights = ExpectedInsights().ToList();

            var consolidators = _security.Subscriptions.SelectMany(x => x.Consolidators);
            var slices = CreateSlices();

            foreach (var slice in slices.ToList())
            {
                _algorithm.SetDateTime(slice.Time);

                var data = slice[_security.Symbol];
                _security.SetMarketPrice(data);

                foreach (var consolidator in consolidators)
                {
                    consolidator.Update(data);
                }

                _algorithm.OnFrameworkData(slice);
            }

            Assert.AreEqual(actualInsights.Count, expectedInsights.Count);

            for (var i = 0; i < actualInsights.Count; i++)
            {
                var actual = actualInsights[i];
                var expected = expectedInsights[i];
                Assert.AreEqual(actual.Symbol, expected.Symbol);
                Assert.AreEqual(actual.Type, expected.Type);
                Assert.AreEqual(actual.Direction, expected.Direction);
                Assert.AreEqual(actual.Period, expected.Period);
                Assert.AreEqual(actual.Magnitude, expected.Magnitude);
                Assert.AreEqual(actual.Confidence, expected.Confidence);
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AddedSecuritiesTest(Language language)
        {
            IAlphaModel model;
            if (!TryCreateModel(language, out model))
            {
                Assert.Ignore($"Ignore {GetType().Name}: Could not create {language} model.");
            }

            var changes = new SecurityChanges(AddedSecurities, RemovedSecurities);

            Assert.DoesNotThrow(() => model.OnSecuritiesChanged(_algorithm, changes));
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void RemovedSecuritiesTest(Language language)
        {
            IAlphaModel model;
            if (!TryCreateModel(language, out model))
            {
                Assert.Ignore($"Ignore {GetType().Name}: Could not create {language} model.");
            }

            var changes = new SecurityChanges(RemovedSecurities, AddedSecurities);

            Assert.DoesNotThrow(() => model.OnSecuritiesChanged(_algorithm, changes));
        }

        /// <summary>
        /// Returns a new instance of the alpha model to test
        /// </summary>
        protected abstract IAlphaModel CreateCSharpAlphaModel();

        /// <summary>
        /// Returns a new instance of the alpha model to test
        /// </summary>
        protected abstract IAlphaModel CreatePythonAlphaModel();

        /// <summary>
        /// Returns an enumerable with the expected insights
        /// </summary>
        protected abstract IEnumerable<Insight> ExpectedInsights();

        /// <summary>
        /// List of securities to be added to the model
        /// </summary>
        protected virtual IEnumerable<Security> AddedSecurities => _algorithm.Securities.Values;

        /// <summary>
        /// List of securities to be removed to the model
        /// </summary>
        protected virtual IEnumerable<Security> RemovedSecurities => Enumerable.Empty<Security>();

        /// <summary>
        /// Creates an enumerable of Slice to update the alpha model
        /// </summary>
        protected virtual IEnumerable<Slice> CreateSlices()
        {
            var cashBook = new CashBook();
            var changes = SecurityChanges.None;
            var sliceDateTimes = GetSliceDateTimes(360);

            for (var i = 0; i < sliceDateTimes.Count; i++)
            {
                var utcDateTime = sliceDateTimes[i];
                var last = Convert.ToDecimal(100 + 10 * Math.Sin(Math.PI * i / 180.0));
                var high = last * 1.005m;
                var low = last / 1.005m;

                var packets = new List<DataFeedPacket>();

                foreach (var kvp in _algorithm.Securities)
                {
                    var security = kvp.Value;
                    var exchange = security.Exchange.Hours;
                    var extendedMarket = security.IsExtendedMarketHours;
                    var localDateTime = utcDateTime.ConvertFromUtc(exchange.TimeZone);
                    if (!exchange.IsOpen(localDateTime, extendedMarket))
                    {
                        continue;
                    }
                    var configuration = security.Subscriptions.FirstOrDefault();
                    var period = security.Resolution.ToTimeSpan();
                    var time = (utcDateTime - period).ConvertFromUtc(configuration.DataTimeZone);
                    var tradeBar = new TradeBar(time, security.Symbol, last, high, low, last, 1000, period);
                    packets.Add(new DataFeedPacket(security, configuration, new List<BaseData> { tradeBar }));
                }

                if (packets.Count > 0)
                {
                    yield return TimeSlice.Create(utcDateTime, TimeZones.NewYork, cashBook, packets, changes).Slice;
                }
            }
        }

        private List<DateTime> GetSliceDateTimes(int maxCount)
        {
            var i = 0;
            var sliceDateTimes = new List<DateTime>();
            var utcDateTime = _algorithm.StartDate;

            while (sliceDateTimes.Count < maxCount)
            {
                foreach (var kvp in _algorithm.Securities)
                {
                    var resolution = kvp.Value.Resolution.ToTimeSpan();
                    utcDateTime = utcDateTime.Add(resolution);
                    if (resolution == Time.OneDay && utcDateTime.TimeOfDay == TimeSpan.Zero)
                    {
                        utcDateTime = utcDateTime.AddHours(17);
                    }

                    var security = kvp.Value;
                    var exchange = security.Exchange.Hours;
                    var extendedMarket = security.IsExtendedMarketHours;
                    var localDateTime = utcDateTime.ConvertFromUtc(exchange.TimeZone);
                    if (exchange.IsOpen(localDateTime, extendedMarket))
                    {
                        sliceDateTimes.Add(utcDateTime);
                    }
                    i++;
                }
            }

            return sliceDateTimes;
        }

        private bool TryCreateModel(Language language, out IAlphaModel model)
        {
            model = default(IAlphaModel);

            try
            {
                switch (language)
                {
                    case Language.CSharp:
                        model = CreateCSharpAlphaModel();
                        return true;
                    case Language.Python:
                        _algorithm.SetPandasConverter();
                        model = CreatePythonAlphaModel();
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}