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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class TradeBarCreatorTests
    {
        [Test]
        public void AggregatesNewTradeBarsProperly()
        {
            TradeBar newTradeBar = null;
            var creator = new TradeBarCreator(2);
            creator.TradeBarCreated += (sender, tradeBar) =>
            {
                newTradeBar = tradeBar;
            };
            var reference = DateTime.Today;
            var bar1 = new TradeBar
            {
                Symbol = "SPY",
                Time = reference,
                Close = 5,
                High = 10,
                Low = 1,
                Open = 4,
                Volume = 100
            };
            creator.Update(bar1);
            Assert.IsNull(newTradeBar);

            var bar2 = new TradeBar
            {
                Symbol = "SPY",
                Time = reference.AddHours(1),
                Close = 6,
                High = 20,
                Low = -1,
                Open = 5,
                Volume = 200
            };
            creator.Update(bar2);
            Assert.IsNotNull(newTradeBar);

            Assert.AreEqual("SPY", newTradeBar.Symbol);
            Assert.AreEqual(bar1.Time, newTradeBar.Time);
            Assert.AreEqual(bar1.Open, newTradeBar.Open);
            Assert.AreEqual(bar2.High, newTradeBar.High);
            Assert.AreEqual(bar2.Low, newTradeBar.Low);
            Assert.AreEqual(bar2.Close, newTradeBar.Close);
            Assert.AreEqual(bar1.Volume + bar2.Volume, newTradeBar.Volume);
        }
    }
}
