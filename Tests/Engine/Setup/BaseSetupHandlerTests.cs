using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.Setup;

namespace QuantConnect.Tests.Engine.Setup
{
    class BaseSetupHandlerTests
    {
        [Test]
        public void CurrencyConversionRateResolved()
        {
            // Unit test to prove that in the event that default resolution (minute) history request returns
            // no data for our currency conversion that BaseSetupHandler will use a daily history request
            // to determine the the conversion rate if possible.

            // Setup history provider and algorithm
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            var zipCache = new ZipDataCacheProvider(new DefaultDataProvider());
            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                null,
                null,
                new DefaultDataProvider(),
                zipCache,
                new LocalDiskMapFileProvider(),
                new LocalDiskFactorFileProvider(),
                null,
                false,
                new DataPermissionManager()));

            var algorithm = new BrokerageSetupHandlerTests.TestAlgorithm { UniverseSettings = { Resolution = Resolution.Minute } };
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
