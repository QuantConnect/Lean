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
using QuantConnect.Algorithm;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using System;
using System.Linq;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmUniverseSettingsTests
    {
        [TestCase(DataNormalizationMode.Raw)]
        [TestCase(DataNormalizationMode.Adjusted)]
        [TestCase(DataNormalizationMode.SplitAdjusted)]
        [TestCase(DataNormalizationMode.TotalReturn)]
        public void CheckUniverseSelectionSecurityDataNormalizationMode(DataNormalizationMode dataNormalizationMode)
        {
            var tuple = GetAlgorithmAndDataManager();
            var algorithm = tuple.Item1;
            var dataManager = tuple.Item2;

            var symbol = Symbols.SPY;
            algorithm.UniverseSettings.DataNormalizationMode = dataNormalizationMode;
            algorithm.AddUniverse(coarse => new[] { symbol });

            var changes = dataManager.UniverseSelection
                .ApplyUniverseSelection(
                    algorithm.UniverseManager.First().Value,
                    algorithm.UtcTime,
                    new BaseDataCollection(algorithm.UtcTime, null, Enumerable.Empty<CoarseFundamental>()));

            Assert.AreEqual(1, changes.AddedSecurities.Count());

            var security = changes.AddedSecurities.First();
            Assert.AreEqual(symbol, security.Symbol);
            Assert.AreEqual(dataNormalizationMode, security.DataNormalizationMode);
        }

        [Test]
        public void CheckManualSecurityDataNormalizationModePersistence()
        {
            var tuple = GetAlgorithmAndDataManager();
            var algorithm = tuple.Item1;
            var dataManager = tuple.Item2;

            var expected = DataNormalizationMode.Adjusted;
            var spy = algorithm.AddEquity("SPY");
            Assert.AreEqual(expected, spy.DataNormalizationMode);

            var symbol = spy.Symbol;
            algorithm.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            algorithm.AddUniverse(coarse => new[] { symbol });

            var changes = dataManager.UniverseSelection
                .ApplyUniverseSelection(
                    algorithm.UniverseManager.First().Value,
                    algorithm.UtcTime,
                    new BaseDataCollection(algorithm.UtcTime, null, Enumerable.Empty<CoarseFundamental>()));

            Assert.AreEqual(1, changes.AddedSecurities.Count());

            var security = changes.AddedSecurities.First();
            Assert.AreEqual(symbol, security.Symbol);
            Assert.AreEqual(spy, security);
            Assert.AreEqual(expected, security.DataNormalizationMode);
        }

        private Tuple<QCAlgorithm, DataManager> GetAlgorithmAndDataManager()
        {
            var algorithm = new QCAlgorithm();

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

            var dataManager = new DataManager(
                new MockDataFeed(),
                new UniverseSelection(
                    algorithm,
                    new SecurityService(
                        algorithm.Portfolio.CashBook,
                        marketHoursDatabase,
                        symbolPropertiesDatabase,
                        algorithm,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCacheProvider(algorithm.Portfolio))),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null);

            var securityService = new SecurityService(
                algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDatabase,
                algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(algorithm.Portfolio));

            algorithm.SubscriptionManager.SetDataManager(dataManager);
            algorithm.Securities.SetSecurityService(securityService);
            return Tuple.Create(algorithm, dataManager);
        }
    }
}