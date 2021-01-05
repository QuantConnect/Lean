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
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Python;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides a framework for testing alpha models.
    /// </summary>
    public abstract class CommonAlphaModelTests
    {
        private QCAlgorithm _algorithm;

        [OneTimeSetUp]
        public void Initialize()
        {
            PythonInitializer.Initialize();

            _algorithm = new QCAlgorithm();
            _algorithm.PortfolioConstruction = new NullPortfolioConstructionModel();
            _algorithm.HistoryProvider = new SineHistoryProvider(_algorithm.Securities);
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            InitializeAlgorithm(_algorithm);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AddAlphaModel(Language language)
        {
            IAlphaModel model;
            IAlphaModel model2 = null;
            IAlphaModel model3 = null;
            if (!TryCreateModel(language, out model)
                || !TryCreateModel(language, out model2)
                || !TryCreateModel(language, out model3))
            {
                Assert.Ignore($"Ignore {GetType().Name}: Could not create {language} model.");
            }

            // Set the alpha model
            _algorithm.SetAlpha(model);
            _algorithm.AddAlpha(model2);
            _algorithm.AddAlpha(model3);
            _algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

            var changes = new SecurityChanges(AddedSecurities, RemovedSecurities);
            _algorithm.OnFrameworkSecuritiesChanged(changes);

            var actualInsights = new List<Insight>();
            _algorithm.InsightsGenerated += (s, e) => actualInsights.AddRange(e.Insights);

            var expectedInsights = ExpectedInsights().ToList();

            var consolidators = _algorithm.Securities.SelectMany(kvp => kvp.Value.Subscriptions).SelectMany(x => x.Consolidators);
            var slices = CreateSlices();

            foreach (var slice in slices.ToList())
            {
                _algorithm.SetDateTime(slice.Time);

                foreach (var symbol in slice.Keys)
                {
                    var data = slice[symbol];
                    _algorithm.Securities[symbol].SetMarketPrice(data);

                    foreach (var consolidator in consolidators)
                    {
                        consolidator.Update(data);
                    }
                }

                _algorithm.OnFrameworkData(slice);
            }

            Assert.AreEqual(expectedInsights.Count * 3, actualInsights.Count);

            for (var i = 0; i < actualInsights.Count; i = i + 3)
            {
                var expected = expectedInsights[i / 3];
                for (int j = i; j < 3; j++)
                {
                    var actual = actualInsights[j];
                    Assert.AreEqual(expected.Symbol, actual.Symbol);
                    Assert.AreEqual(expected.Type, actual.Type);
                    Assert.AreEqual(expected.Direction, actual.Direction);
                    Assert.AreEqual(expected.Period, actual.Period);
                    Assert.AreEqual(expected.Magnitude, actual.Magnitude);
                    Assert.AreEqual(expected.Confidence, actual.Confidence);
                }
            }
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
            _algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

            var changes = new SecurityChanges(AddedSecurities, RemovedSecurities);
            _algorithm.OnFrameworkSecuritiesChanged(changes);

            var actualInsights = new List<Insight>();
            _algorithm.InsightsGenerated += (s, e) => actualInsights.AddRange(e.Insights);

            var expectedInsights = ExpectedInsights().ToList();

            var consolidators = _algorithm.Securities.SelectMany(kvp => kvp.Value.Subscriptions).SelectMany(x => x.Consolidators);
            var slices = CreateSlices();

            foreach (var slice in slices.ToList())
            {
                _algorithm.SetDateTime(slice.Time);

                foreach (var symbol in slice.Keys)
                {
                    var data = slice[symbol];
                    _algorithm.Securities[symbol].SetMarketPrice(data);

                    foreach (var consolidator in consolidators)
                    {
                        consolidator.Update(data);
                    }
                }

                _algorithm.OnFrameworkData(slice);
            }

            Assert.AreEqual(expectedInsights.Count, actualInsights.Count);

            for (var i = 0; i < actualInsights.Count; i++)
            {
                var actual = actualInsights[i];
                var expected = expectedInsights[i];
                Assert.AreEqual(expected.Symbol, actual.Symbol);
                Assert.AreEqual(expected.Type, actual.Type);
                Assert.AreEqual(expected.Direction, actual.Direction);
                Assert.AreEqual(expected.Period, actual.Period);
                Assert.AreEqual(expected.Magnitude, actual.Magnitude);
                Assert.AreEqual(expected.Confidence, actual.Confidence);
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

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void ModelNameTest(Language language)
        {
            IAlphaModel model;
            if (!TryCreateModel(language, out model))
            {
                Assert.Ignore($"Ignore {GetType().Name}: Could not create {language} model.");
            }

            var actual = model.GetModelName();
            var expected = GetExpectedModelName(model);
            Assert.AreEqual(expected, actual);
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
        /// To be override for model types that implement <see cref="INamedModel"/>
        /// </summary>
        protected abstract string GetExpectedModelName(IAlphaModel model);

        /// <summary>
        /// Provides derived types a chance to initialize anything special they require
        /// </summary>
        protected virtual void InitializeAlgorithm(QCAlgorithm algorithm)
        {
            _algorithm.SetStartDate(2018, 1, 4);
            _algorithm.AddEquity(Symbols.SPY.Value, Resolution.Daily);
        }

        /// <summary>
        /// Creates an enumerable of Slice to update the alpha model
        /// </summary>
        protected virtual IEnumerable<Slice> CreateSlices()
        {
            var timeSliceFactory = new TimeSliceFactory(TimeZones.NewYork);
            var changes = SecurityChanges.None;
            var sliceDateTimes = GetSliceDateTimes(MaxSliceCount);

            for (var i = 0; i < sliceDateTimes.Count; i++)
            {
                var utcDateTime = sliceDateTimes[i];

                var packets = new List<DataFeedPacket>();

                // TODO : Give securities different values -- will require updating all derived types
                var last = Convert.ToDecimal(100 + 10 * Math.Sin(Math.PI * i / 180.0));
                var high = last * 1.005m;
                var low = last / 1.005m;
                foreach (var kvp in _algorithm.Securities)
                {
                    var security = kvp.Value;
                    var exchange = security.Exchange.Hours;
                    var configs = _algorithm.SubscriptionManager.SubscriptionDataConfigService
                        .GetSubscriptionDataConfigs(security.Symbol);
                    var extendedMarket = configs.IsExtendedMarketHours();
                    var localDateTime = utcDateTime.ConvertFromUtc(exchange.TimeZone);
                    if (!exchange.IsOpen(localDateTime, extendedMarket))
                    {
                        continue;
                    }
                    var configuration = security.Subscriptions.FirstOrDefault();
                    var period = configs.GetHighestResolution().ToTimeSpan();
                    var time = (utcDateTime - period).ConvertFromUtc(configuration.DataTimeZone);
                    var tradeBar = new TradeBar(time, security.Symbol, last, high, low, last, 1000, period);
                    packets.Add(new DataFeedPacket(security, configuration, new List<BaseData> { tradeBar }));
                }

                if (packets.Count > 0)
                {
                    yield return timeSliceFactory.Create(utcDateTime, packets, changes, new Dictionary<Universe, BaseDataCollection>()).Slice;
                }
            }
        }

        /// <summary>
        /// Gets the maximum number of slice objects to generate
        /// </summary>
        protected virtual int MaxSliceCount => 360;

        private List<DateTime> GetSliceDateTimes(int maxCount)
        {
            var i = 0;
            var sliceDateTimes = new List<DateTime>();
            var utcDateTime = _algorithm.StartDate;

            while (sliceDateTimes.Count < maxCount)
            {
                foreach (var kvp in _algorithm.Securities)
                {
                    var security = kvp.Value;
                    var configs = _algorithm.SubscriptionManager.SubscriptionDataConfigService
                        .GetSubscriptionDataConfigs(security.Symbol);
                    var resolution = configs.GetHighestResolution().ToTimeSpan();
                    utcDateTime = utcDateTime.Add(resolution);
                    if (resolution == Time.OneDay && utcDateTime.TimeOfDay == TimeSpan.Zero)
                    {
                        utcDateTime = utcDateTime.AddHours(17);
                    }
                    var exchange = security.Exchange.Hours;
                    var extendedMarket = configs.IsExtendedMarketHours();
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
    }
}