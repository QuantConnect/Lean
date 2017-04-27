using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture]
    public class FuturesExpiryFunctionsTests
    {
        [Test]
        public void TestGoldExpiryDateFunction()
        {
            var april2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 4, 1));
            var april2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(april2017.ID.Symbol);
            Assert.AreEqual(april2017Func(april2017.ID.Date), new DateTime(2017, 4, 26));

            var may2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 5, 31));
            var may2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(may2017.ID.Symbol);
            Assert.AreEqual(may2017Func(may2017.ID.Date), new DateTime(2017, 5, 26));

            var june2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 6, 15));
            var june2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2017.ID.Symbol);
            Assert.AreEqual(june2017Func(june2017.ID.Date), new DateTime(2017, 6, 28));

            var july2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 7, 15));
            var july2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(july2017.ID.Symbol);
            Assert.AreEqual(july2017Func(july2017.ID.Date), new DateTime(2017, 7, 27));

            var october2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2018, 10, 15));
            var october2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(october2018.ID.Symbol);
            Assert.AreEqual(october2018Func(october2018.ID.Date), new DateTime(2018, 10, 29));

            var december2021 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2021, 12, 15));
            var december2021Func = FuturesExpiryFunctions.FuturesExpiryFunction(december2021.ID.Symbol);
            Assert.AreEqual(december2021Func(december2021.ID.Date), new DateTime(2021, 12, 29));
        }
    }
}
