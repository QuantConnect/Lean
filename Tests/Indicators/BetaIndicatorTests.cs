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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class BetaIndicatorTests : CommonIndicatorTests<TradeBar>
    {
        protected override string TestFileName => "bi_datatest.csv";

        protected override string TestColumnName => "beta";

        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            var indicator = new BetaIndicator("testBetaIndicator", 5, "AMZN 2T", "SPX 2T");
            return indicator;
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = new BetaIndicator("testBetaIndicator", 5, "IBM", "SPY");
            var startDate = new DateTime(2019, 1, 1);

            for (var i = 10; i > 0; i--)
            {
                var stockInput = GetInput("IBM" ,startDate, i);
                var marketIndexInput = GetInput("SPY", startDate, i);
                indicator.Update(stockInput);
                indicator.Update(marketIndexInput);
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = new BetaIndicator("testBetaIndicator", 5, "IBM", "SPY");
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            var startDate = new DateTime(2019, 1, 1);

            for (var i = 0; i < period.Value; i++)
            {
                var stockInput = GetInput("IBM", startDate, i);
                var marketIndexInput = GetInput("SPY", startDate, i);
                indicator.Update(stockInput);
                indicator.Update(marketIndexInput);
            }

            Assert.AreEqual(2*period.Value, indicator.Samples);
        }
    }
}
