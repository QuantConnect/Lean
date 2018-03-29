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

using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Packets;
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
            _algorithm.HistoryProvider = new SineHistoryProvider(_algorithm);
            _algorithm.SetStartDate(2018, 1, 4);
            _security = _algorithm.AddEquity(Symbols.SPY.Value);
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void InsightsGenerationTest(Language language)
        {
            IAlphaModel model;
            if (!TryCreateAlphaModel(language, out model))
            {
                Assert.Ignore($"Ignore {GetType().Name}: Could not create {language} model.");
            }

            var changes = new SecurityChanges(AddedSecurities, RemovedSecurities);
            model.OnSecuritiesChanged(_algorithm, changes);

            var allInsights = new List<Insight>();
            var consolidators = _security.Subscriptions.SelectMany(x => x.Consolidators);
            var slices = CreateSlices(changes);

            foreach (var slice in slices)
            {
                var data = slice[_security.Symbol];
                foreach (var consolidator in consolidators)
                {
                    consolidator.Update(data);
                }
                _security.SetMarketPrice(data);

                var insights = model.Update(_algorithm, slice);
                allInsights.AddRange(insights);
            }

            var actualArray = allInsights.ToArray();
            var expectedArray = ExpectedInsights().ToArray();

            Assert.AreEqual(actualArray.Length, expectedArray.Length);

            for (var i = 0; i < actualArray.Length; i++)
            {
                var actual = actualArray[i];
                var expected = expectedArray[i];
                Assert.True(actual.Equals(expected));
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AddedSecuritiesTest(Language language)
        {
            IAlphaModel model;
            if (!TryCreateAlphaModel(language, out model))
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
            if (!TryCreateAlphaModel(language, out model))
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
        protected virtual IEnumerable<Slice> CreateSlices(SecurityChanges changes)
        {
            return Enumerable
                .Range(0, 360)
                .Select(i =>
                {
                    var time = _algorithm.StartDate.AddMinutes(i);
                    var last = Convert.ToDecimal(100 + 10 * Math.Sin(Math.PI * i / 180.0));
                    var high = last * 1.005m;
                    var low = last / 1.005m;

                    var packets = _algorithm.Securities.Select(kvp =>
                    {
                        var security = kvp.Value;
                        var tradeBar = new TradeBar(time, security.Symbol, last, high, low, last, 1000);
                        var configuration = security.Subscriptions.FirstOrDefault();
                        return new DataFeedPacket(security, configuration, new List<BaseData> { tradeBar });
                    });

                    return TimeSlice.Create(time, TimeZones.NewYork, new CashBook(), packets.ToList(), changes).Slice;
                });
        }

        private bool TryCreateAlphaModel(Language language, out IAlphaModel model)
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

        /// <summary>
        /// Implements a History provider that always return a IEnumerable of Slice with prices following a sine function
        /// </summary>
        internal class SineHistoryProvider : IHistoryProvider
        {
            private QCAlgorithmFramework _algorithm;

            public int DataPointCount => 0;

            public SineHistoryProvider(QCAlgorithmFramework algorithm)
            {
                _algorithm = algorithm;
            }

            public void Initialize(AlgorithmNodePacket job, IDataProvider dataProvider, IDataCacheProvider dataCacheProvider, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
            {
            }

            public IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                foreach (var request in requests)
                {
                    Security security;
                    if (!_algorithm.Securities.TryGetValue(request.Symbol, out security))
                    {
                        continue;
                    }

                    var utc = request.StartTimeUtc;
                    var end = request.EndTimeUtc;
                    var span = request.Resolution.ToTimeSpan();
                    var range = 0;

                    while (utc < end)
                    {
                        var time = utc.ConvertFromUtc(sliceTimeZone);
                        if (request.ExchangeHours.IsOpen(time, request.IncludeExtendedMarketHours))
                        {
                            range++;
                        }
                        utc = utc.Add(span);
                    }

                    utc = request.StartTimeUtc;
                    var configuration = security.Subscriptions.FirstOrDefault();

                    for (var i = 0; i < range; i++)
                    {
                        var time = utc.AddSeconds(i * span.TotalSeconds);
                        var last = Convert.ToDecimal(100 + 10 * Math.Sin(Math.PI * (360 - range + i) / 180.0));
                        var high = last * 1.005m;
                        var low = last / 1.005m;
                        var data = new List<BaseData> { new TradeBar(time, security.Symbol, last, high, last, last, 1000) };
                        var packets = new List<DataFeedPacket> { new DataFeedPacket(security, configuration, data) };
                        yield return TimeSlice.Create(time, sliceTimeZone, new CashBook(), packets, SecurityChanges.None).Slice;
                    }
                }
            }
        }
    }
}