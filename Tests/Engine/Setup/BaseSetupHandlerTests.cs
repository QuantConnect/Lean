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
using QuantConnect.Data.UniverseSelection;
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

        [Test]
        public void CurrencyConversionRateResolvedForWhiteListedCurrenciesOnly()
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
            algorithm.SetCash("EUR", 1000);
            algorithm.SetCash("USDT", 1000);

            // Have BaseSetupHandler resolve the currency conversion
            BaseSetupHandler.SetupCurrencyConversions(algorithm, algorithm.DataManager.UniverseSelection, new[] { "BTC" });

            // Bitcoin's conversion rate should be set
            Assert.IsNotNull(algorithm.Portfolio.CashBook["BTC"].CurrencyConversion);
            Assert.AreNotEqual(0, algorithm.Portfolio.CashBook["BTC"].ConversionRate);

            // The remaining currencies should not have conversion rate set
            Assert.AreEqual(0, algorithm.Portfolio.CashBook["EUR"].ConversionRate);
            Assert.AreEqual(0, algorithm.Portfolio.CashBook["USDT"].ConversionRate);
        }

        [Test]
        public void RuntimeCurrencyConversionRateIsSeeded()
        {
            // Regression test for the runtime currency-conversion seeding gap: when a currency that requires a
            // conversion feed is introduced after setup (e.g. a SetCash mid-algorithm, or a universe adding a
            // security whose quote currency isn't yet in the cashbook), the runtime path
            // (UniverseSelection.EnsureCurrencyDataFeeds) used to only create the conversion subscription without
            // seeding its price. The rate therefore stayed 0 until the first bar of the pair arrived, and any
            // conversion in that window threw 'The conversion rate for <currency> is not available'.
            // After the fix, the runtime path seeds the new conversion security just like BaseSetupHandler does
            // during setup, so the rate is non-zero immediately.

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

            algorithm.SetStartDate(2015, 1, 24);
            algorithm.SetCash("USD", 0);

            // Run setup so the engine is in the post-setup (runtime) state.
            BaseSetupHandler.SetupCurrencyConversions(algorithm, algorithm.DataManager.UniverseSelection);

            // Now introduce a new currency at runtime, after setup has already run. This mirrors a SetCash mid-run
            // or a universe adding a security with a new quote currency. Before the fix the conversion rate would
            // remain 0 here until the first BTCUSD bar arrived.
            algorithm.SetCash("BTC", 10);

            // The runtime path that wires up the conversion feed during universe selection.
            algorithm.DataManager.UniverseSelection.EnsureCurrencyDataFeeds(SecurityChanges.None);

            // The new currency should have its conversion security and a non-zero rate already, without waiting for
            // a live bar - so a conversion requested right now would no longer throw.
            Assert.IsNotNull(algorithm.Portfolio.CashBook["BTC"].CurrencyConversion);
            Assert.AreNotEqual(0, algorithm.Portfolio.CashBook["BTC"].ConversionRate);
            Assert.IsTrue(algorithm.Portfolio.CashBook["BTC"].ValueInAccountCurrency > 0);

            // Sanity: converting the runtime currency does not throw.
            Assert.DoesNotThrow(() => algorithm.Portfolio.CashBook.ConvertToAccountCurrency(10, "BTC"));
        }
    }
}
