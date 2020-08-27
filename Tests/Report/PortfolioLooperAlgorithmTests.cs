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
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Orders;
using QuantConnect.Report;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class PortfolioLooperAlgorithmTests
    {
        private PortfolioLooperAlgorithm CreateAlgorithm(IEnumerable<Order> orders)
        {
            var algorithm = new PortfolioLooperAlgorithm(100000m, orders);

            // Create MHDB and Symbol properties DB instances for the DataManager
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(new QuantConnect.Report.MockDataFeed(),
                new UniverseSelection(
                    algorithm,
                    new SecurityService(algorithm.Portfolio.CashBook,
                        marketHoursDatabase,
                        symbolPropertiesDataBase,
                        algorithm,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCacheProvider(algorithm.Portfolio)),
                    dataPermissionManager,
                    new DefaultDataProvider()),
                algorithm,
                algorithm.TimeKeeper,
                marketHoursDatabase,
                false,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager);

            var securityService = new SecurityService(algorithm.Portfolio.CashBook,
                marketHoursDatabase,
                symbolPropertiesDataBase,
                algorithm,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(algorithm.Portfolio));

            // Initialize security services and other properties so that we
            // don't get null reference exceptions during our re-calculation
            algorithm.Securities.SetSecurityService(securityService);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            return algorithm;
        }

        [Test]
        public void Algorithm_CanSetLeverageOnAllSecurityTypes()
        {
            var orders = new Symbol[]
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 120m, new DateTime(2020, 5, 21)),
                Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
                Symbol.Create("XAUUSD", SecurityType.Cfd, Market.Oanda),
                Symbol.CreateFuture(Futures.Energies.CrudeOilWTI, Market.NYMEX, new DateTime(2020, 5, 21)),
                Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX)
            }.Select(s => new MarketOrder(s, 1m, new DateTime(2020, 1, 1))).ToList();

            var algorithm = CreateAlgorithm(orders);
            Assert.DoesNotThrow(() => algorithm.FromOrders(orders));
        }

        [Test]
        public void Algorithm_UsesExpectedLeverageOnAllSecurityTypes()
        {
            var orders = new Symbol[]
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 120m, new DateTime(2020, 5, 21)),
                Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
                Symbol.Create("XAUUSD", SecurityType.Cfd, Market.Oanda),
                Symbol.CreateFuture(Futures.Energies.CrudeOilWTI, Market.NYMEX, new DateTime(2020, 5, 21)),
                Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX)
            }.Select(s => new MarketOrder(s, 1m, new DateTime(2020, 1, 1)));

            var algorithm = CreateAlgorithm(orders);

            Assert.IsTrue(algorithm.Securities.Where(x => x.Key.SecurityType == SecurityType.Equity).All(x => x.Value.BuyingPowerModel.GetLeverage(x.Value) == 10000m));
            Assert.IsTrue(algorithm.Securities.Where(x => x.Key.SecurityType == SecurityType.Option).All(x => x.Value.BuyingPowerModel.GetLeverage(x.Value) == 1m));
            Assert.IsTrue(algorithm.Securities.Where(x => x.Key.SecurityType == SecurityType.Forex).All(x => x.Value.BuyingPowerModel.GetLeverage(x.Value) == 10000m));
            Assert.IsTrue(algorithm.Securities.Where(x => x.Key.SecurityType == SecurityType.Cfd).All(x => x.Value.BuyingPowerModel.GetLeverage(x.Value) == 10000m));
            Assert.IsTrue(algorithm.Securities.Where(x => x.Key.SecurityType == SecurityType.Future).All(x => x.Value.BuyingPowerModel.GetLeverage(x.Value) == 1m));
            Assert.IsTrue(algorithm.Securities.Where(x => x.Key.SecurityType == SecurityType.Crypto).All(x => x.Value.BuyingPowerModel.GetLeverage(x.Value) == 10000m));
        }
    }
}
