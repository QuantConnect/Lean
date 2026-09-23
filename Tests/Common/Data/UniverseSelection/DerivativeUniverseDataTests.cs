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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class DerivativeUniverseDataTests
    {
        [Test]
        public void FutureRowUsesTheContractMonth()
        {
            var symbol = Symbol.CreateFuture("ES", QuantConnect.Market.CME, new DateTime(2020, 3, 20));
            var data = new DerivativeUniverseData(new TradeBar(new DateTime(2020, 1, 6), symbol, 10, 12, 9, 11, 100));
            data.UpdateByOpenInterest(new OpenInterest(new DateTime(2020, 1, 6), symbol, 5000));

            Assert.AreEqual("202003,10,12,9,11,100,5000", data.ToCsv());
        }

        [Test]
        public void OptionRowUsesTheContractDetails()
        {
            var symbol = Symbol.CreateOption(Symbols.SPY, QuantConnect.Market.USA, OptionStyle.American, OptionRight.Put, 300, new DateTime(2020, 3, 20));
            var data = new DerivativeUniverseData(new TradeBar(new DateTime(2020, 1, 6), symbol, 10, 12, 9, 11, 100));
            data.UpdateByOpenInterest(new OpenInterest(new DateTime(2020, 1, 6), symbol, 5000));

            Assert.AreEqual("20200320,300,P,10,12,9,11,100,5000,,0,0,0,0,0", data.ToCsv());
        }

        [Test]
        public void UnderlyingRowHasNoContractDetails()
        {
            var data = new DerivativeUniverseData(new TradeBar(new DateTime(2020, 1, 6), Symbols.SPY, 10, 12, 9, 11, 100));

            Assert.AreEqual(",,,10,12,9,11,100,,,0,0,0,0,0", data.ToCsv());
        }
    }
}
