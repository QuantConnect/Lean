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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class TradeBarCreatorBaseTests
    {
        // we could add more tests here, but they'd really be copies of TradeBarConsolidator
        // Since TradeBarConsolidator uses this for determining the when to fire, many of those tests
        // could (and probably should) just be moved over to here.

        [Test]
        public void FiresEventOnNewTradeBar()
        {
            bool eventFired = false;
            var creator = new TradeBarCreatorFake(1);
            creator.TradeBarCreated += (sender, tradeBar) =>
            {
                eventFired = true;
            };
            var bar = new IndicatorDataPoint
            {
                Symbol = "SPY",
                Time = DateTime.Today,
                Value = 10
            };
            creator.Update(bar);
            Assert.IsTrue(eventFired);
        }

        private class TradeBarCreatorFake : TradeBarCreatorBase<IndicatorDataPoint>
        {
            public TradeBarCreatorFake(TimeSpan period)
                : base(period)
            {
            }

            public TradeBarCreatorFake(int maxCount)
                : base(maxCount)
            {
            }

            public TradeBarCreatorFake(int maxCount, TimeSpan period)
                : base(maxCount, period)
            {
            }

            protected override void AggregateBar(ref TradeBar workingBar, IndicatorDataPoint data)
            {
                if (workingBar == null)
                {
                    workingBar = new TradeBar
                    {
                        Symbol = data.Symbol,
                        Time = data.Time,
                        Open = data.Value,
                        High = data.Value,
                        Low = data.Value,
                        Close = data.Value
                    };
                }
                else
                {
                    //Aggregate the working bar
                    workingBar.Close = data.Value;
                    if (data.Value < workingBar.Low) workingBar.Low = data.Value;
                    if (data.Value > workingBar.High) workingBar.High = data.Value;
                }
            }
        }
    }
}