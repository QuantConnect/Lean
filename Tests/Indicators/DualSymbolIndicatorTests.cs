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
using System.Collections.Generic;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DualSymbolIndicatorTests
    {
        private DateTime _reference = new DateTime(2020, 1, 1);

        [Test]
        public void TimeMovesForward()
        {
            var indicator = new TestAverageIndicator(Symbols.IBM, Symbols.SPY, 5);

            for (var i = 10; i > 0; i--)
            {
                indicator.Update(new TradeBar() { Symbol = Symbols.IBM, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 500, Time = _reference.AddDays(1 + i) });
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public void ValidateCalculation()
        {
            var indicator = new TestAverageIndicator(Symbols.AAPL, Symbols.SPX, 3);

            var bars = new List<TradeBar>()
            {
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 10, Time = _reference.AddDays(1), EndTime = _reference.AddDays(2) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 35, Time = _reference.AddDays(1), EndTime = _reference.AddDays(2) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 2, Time = _reference.AddDays(2),EndTime = _reference.AddDays(3) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 2, Time = _reference.AddDays(2), EndTime = _reference.AddDays(3) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 15, Time = _reference.AddDays(3), EndTime = _reference.AddDays(4) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 80, Time = _reference.AddDays(3), EndTime = _reference.AddDays(4) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 4, Time = _reference.AddDays(4), EndTime = _reference.AddDays(5) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 4, Time = _reference.AddDays(4), EndTime = _reference.AddDays(5) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 37, Time = _reference.AddDays(5), EndTime = _reference.AddDays(6) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 90, Time = _reference.AddDays(5), EndTime = _reference.AddDays(6) },
                new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 105, Time = _reference.AddDays(6), EndTime = _reference.AddDays(7) },
                new TradeBar() { Symbol = Symbols.SPX, Low = 1, High = 2, Volume = 100, Close = 302, Time = _reference.AddDays(6), EndTime = _reference.AddDays(7) },
            };

            foreach (var bar in bars)
            {
                indicator.Update(bar);
            }

            var closeAAPL = new List<decimal>() { 10, 15, 90, 105 };
            var closeSPX = new List<decimal>() { 35, 80, 37, 302 };
            var expectedValue = 0m;
            for (var i = 0; i < closeAAPL.Count; i++)
            {
                expectedValue += (closeAAPL[i] + closeSPX[i]) / 2;
            }

            Assert.AreEqual(expectedValue, indicator.Current.Value);
        }

        [Test]
        public void WorksWithDifferentTimeZones()
        {
            var indicator = new TestAverageIndicator(Symbols.SPY, Symbols.BTCUSD, 5);

            for (int i = 0; i < 10; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 100, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.BTCUSD, Low = 1, High = 2, Volume = 100, Close = 100, Time = startTime, EndTime = endTime });
            }
            Assert.AreEqual(100 * 10, indicator.Current.Value);
        }

        [Test]
        public void TracksPreviousState()
        {
            var period = 5;
            var indicator = new TestAverageIndicator(Symbols.SPY, Symbols.AAPL, period);
            var previousValue = indicator.Current.Value;

            // Update the indicator and verify the previous values
            for (var i = 1; i < 2 * period; i++)
            {
                var startTime = _reference.AddDays(1 + i);
                var endTime = startTime.AddDays(1);
                indicator.Update(new TradeBar() { Symbol = Symbols.SPY, Low = 1, High = 2, Volume = 100, Close = 1000 + i * 10, Time = startTime, EndTime = endTime });
                indicator.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Close = 1000 + (i * 15), Time = startTime, EndTime = endTime });
                // Verify the previous value matches the indicator's previous value
                Assert.AreEqual(previousValue, indicator.Previous.Value);

                // Update previousValue to the current value for the next iteration
                previousValue = indicator.Current.Value;
            }
        }

        private class TestAverageIndicator : DualSymbolIndicator<IBaseDataBar>
        {
            public TestAverageIndicator(Symbol targetSymbol, Symbol referenceSymbol, int period)
                : base("TestIndicator", targetSymbol, referenceSymbol, period)
            {
            }

            public override bool IsReady => TargetDataPoints.IsReady && ReferenceDataPoints.IsReady;

            protected override decimal ComputeIndicator()
            {
                var prevValue = IndicatorValue;
                var result = IndicatorValue += (TargetDataPoints[0].Close + ReferenceDataPoints[0].Close) / 2;
                Console.WriteLine($"Previous Value: {prevValue}, Current Value: {IndicatorValue} (Inputs: {TargetDataPoints[^1]} and {ReferenceDataPoints[^1]})");
                return result;
            }
        }
    }
}
