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

        protected override string TestColumnName => "Beta";

        private DateTime _reference = new DateTime(2020, 1, 1);

        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            VolumeRenkoBarSize = 100000;
            var indicator = new Beta("testBetaIndicator", 5, "AMZN 2T", "SPX 2T");
            return indicator;
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = new Beta("testBetaIndicator", 5, Symbols.IBM, Symbols.SPY);

            for (var i = 10; i > 0; i--)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = new Beta("testBetaIndicator", 5, Symbols.IBM, Symbols.SPY);
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            for (var i = 0; i < period.Value; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2*period.Value, indicator.Samples);
        }

        [Test]
        public void EqualBetaValue()
        {
            var indicator = new Beta("testBetaIndicator", 5, Symbols.AAPL, Symbols.SPX);

            for (int i = 0 ; i < 3 ; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = i + 1 ,Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(1, (double) indicator.Current.Value, 0.0001);
        }

        [Test]
        public void NotEqualBetaValue()
        {
            var indicator = new Beta("testBetaIndicator", 5, Symbols.AAPL, Symbols.SPX);

            for (int i = 0; i < 3; i++)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = i + 1, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = i + 2, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreNotEqual(1, (double)indicator.Current.Value);
        }
    }
}
