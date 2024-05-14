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
 *
*/

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.Tests.Engine.Setup
{
    [TestFixture]
    public class BaseSetupHandlerTests
    {
        [Test]
        public void CurrencyConversionRateResolved()
        {
            // Unit test to prove that in the event that default resolution (minute) history request returns
            // no data for our currency conversion that BaseSetupHandler will use a daily history request
            // to determine the the conversion rate if possible.

            // Setup history provider and algorithm
            var historyProvider = new SubscriptionDataReaderHistoryProvider();

            var algorithm = new BrokerageSetupHandlerTests.TestAlgorithm { UniverseSettings = { Resolution = Resolution.Minute } };

            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                TestGlobals.DataProvider,
                TestGlobals.DataCacheProvider,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                null,
                false,
                new DataPermissionManager(),
                algorithm.ObjectStore,
                algorithm.Settings));
            algorithm.SetHistoryProvider(historyProvider);

            // Pick a date range where we do NOT have BTCUSD minute data
            algorithm.SetStartDate(2015, 1, 24);
            algorithm.SetCash("USD", 0);
            algorithm.SetCash("BTC", 10);

            // Have BaseSetupHandler resolve the currency conversion
            BaseSetupHandler.SetupCurrencyConversions(algorithm, algorithm.DataManager.UniverseSelection);

            // Assert that our portfolio has some value and that value is bitcoin
            Assert.IsTrue(algorithm.Portfolio.Cash > 0);
            Assert.IsTrue(algorithm.Portfolio.CashBook["BTC"].ValueInAccountCurrency > 0);
        }
    }
}
