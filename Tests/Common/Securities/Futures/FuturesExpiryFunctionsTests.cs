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

using System;
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

        [Test]
        public void TestSP500EMiniExpiryDateFunction()
        {
            var june2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 6, 15));
            var june2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2017.ID.Symbol);
            Assert.AreEqual(june2017Func(june2017.ID.Date), new DateTime(2017, 6, 16, 9, 30, 0));

            var september2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 9, 1));
            var september2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(september2017.ID.Symbol);
            Assert.AreEqual(september2017Func(september2017.ID.Date), new DateTime(2017, 9, 15, 9, 30, 0));

            var december2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 12, 31));
            var december2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(december2017.ID.Symbol);
            Assert.AreEqual(december2017Func(december2017.ID.Date), new DateTime(2017, 12, 15, 9, 30, 0));

            var march2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2018, 3, 31));
            var march2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(march2018.ID.Symbol);
            Assert.AreEqual(march2018Func(march2018.ID.Date), new DateTime(2018, 3, 16, 9, 30, 0));

            var june2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2018, 3, 31));
            var june2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2018.ID.Symbol);
            Assert.AreEqual(june2018Func(june2018.ID.Date), new DateTime(2018, 3, 16, 9, 30, 0));
        }
    }
}
