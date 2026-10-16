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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class TDSequentialTests
    {
        private static TradeBar CreateBar(DateTime time, decimal close, decimal high = 0m, decimal low = 0m, decimal open = 0m)
        {
            if (open == 0m)
            {
                open = close;
            }
            if (high == 0m)
            {
                high = close + 1m;
            }
            if (low == 0m)
            {
                low = close - 1m;
            }

            return new TradeBar(time, Symbols.SPY, open, high, low, close, 1000m);
        }

        [Test]
        public void TestWarmUpPeriod()
        {
            var td = new TDSequential();
            Assert.AreEqual(5, td.WarmUpPeriod);
            Assert.IsFalse(td.IsReady);

            var time = new DateTime(2023, 1, 1);
            for (var i = 1; i <= 4; i++)
            {
                var bar = CreateBar(time.AddDays(i), 100m, 105m, 95m);
                td.Update(bar);
                Assert.IsFalse(td.IsReady, $"Indicator should not be ready at sample {i}");
                Assert.AreEqual(0m, td.SetupValue.Value);
                Assert.AreEqual(0m, td.CountdownValue.Value);
                Assert.AreEqual(0m, td.Current.Value);
            }

            var fifthBar = CreateBar(time.AddDays(5), 90m, 95m, 85m);
            td.Update(fifthBar);
            Assert.IsTrue(td.IsReady, "Indicator should be ready after 5 samples (WarmUpPeriod = 5)");
        }

        [Test]
        public void TestBuySetupSequence()
        {
            var td = new TDSequential();
            var time = new DateTime(2023, 1, 1);

            // Feed 4 warmup bars
            var warmupCloses = new[] { 200m, 195m, 190m, 185m };
            for (var i = 0; i < warmupCloses.Length; i++)
            {
                td.Update(CreateBar(time.AddDays(i + 1), warmupCloses[i], warmupCloses[i] + 5m, warmupCloses[i] - 5m));
                Assert.IsFalse(td.IsReady);
                Assert.AreEqual(0m, td.SetupValue.Value);
            }

            // Feed 9 consecutive bars where Close < Close of bar 4 bars ago
            // Bar 5 compares to Bar 1 (180 < 200) -> Setup 1 (-1)
            // Bar 6 compares to Bar 2 (175 < 195) -> Setup 2 (-2)
            // ...
            // Bar 13 compares to Bar 9 (140 < 160) -> Setup 9 (-9)
            for (var i = 1; i <= 9; i++)
            {
                var close = 180m - (i - 1) * 5m;
                var dayIndex = 4 + i;
                td.Update(CreateBar(time.AddDays(dayIndex), close, close + 5m, close - 5m));

                Assert.IsTrue(td.IsReady);
                Assert.AreEqual(-i, td.SetupValue.Value, $"Buy Setup should be -{i} at step {i}");
                Assert.AreEqual(-i, td.Current.Value, $"Current.Value should match Buy Setup -{i} at step {i}");
                Assert.AreEqual(0m, td.CountdownValue.Value, "CountdownValue should remain 0 during setup phase");
            }
        }

        [Test]
        public void TestSellSetupSequence()
        {
            var td = new TDSequential();
            var time = new DateTime(2023, 1, 1);

            // Feed 4 warmup bars
            var warmupCloses = new[] { 100m, 105m, 110m, 115m };
            for (var i = 0; i < warmupCloses.Length; i++)
            {
                td.Update(CreateBar(time.AddDays(i + 1), warmupCloses[i], warmupCloses[i] + 5m, warmupCloses[i] - 5m));
                Assert.IsFalse(td.IsReady);
                Assert.AreEqual(0m, td.SetupValue.Value);
            }

            // Feed 9 consecutive bars where Close > Close of bar 4 bars ago
            // Bar 5 compares to Bar 1 (120 > 100) -> Setup 1 (+1)
            // Bar 6 compares to Bar 2 (125 > 105) -> Setup 2 (+2)
            // ...
            // Bar 13 compares to Bar 9 (160 > 120) -> Setup 9 (+9)
            for (var i = 1; i <= 9; i++)
            {
                var close = 120m + (i - 1) * 5m;
                var dayIndex = 4 + i;
                td.Update(CreateBar(time.AddDays(dayIndex), close, close + 5m, close - 5m));

                Assert.IsTrue(td.IsReady);
                Assert.AreEqual(i, td.SetupValue.Value, $"Sell Setup should be {i} at step {i}");
                Assert.AreEqual(i, td.Current.Value, $"Current.Value should match Sell Setup {i} at step {i}");
                Assert.AreEqual(0m, td.CountdownValue.Value, "CountdownValue should remain 0 during setup phase");
            }
        }

        [Test]
        public void TestInterruptedSetup()
        {
            var td = new TDSequential();
            var time = new DateTime(2023, 1, 1);

            // Warmup: 4 bars
            td.Update(CreateBar(time.AddDays(1), 200m, 205m, 195m));
            td.Update(CreateBar(time.AddDays(2), 195m, 200m, 190m));
            td.Update(CreateBar(time.AddDays(3), 190m, 195m, 185m));
            td.Update(CreateBar(time.AddDays(4), 185m, 190m, 180m));

            // Buy Setup bars 1 to 4:
            td.Update(CreateBar(time.AddDays(5), 180m, 185m, 175m)); // < 200 -> -1
            Assert.AreEqual(-1m, td.SetupValue.Value);

            td.Update(CreateBar(time.AddDays(6), 175m, 180m, 170m)); // < 195 -> -2
            Assert.AreEqual(-2m, td.SetupValue.Value);

            td.Update(CreateBar(time.AddDays(7), 170m, 175m, 165m)); // < 190 -> -3
            Assert.AreEqual(-3m, td.SetupValue.Value);

            td.Update(CreateBar(time.AddDays(8), 165m, 170m, 160m)); // < 185 -> -4
            Assert.AreEqual(-4m, td.SetupValue.Value);

            // Bar 9: Bar 4 bars ago is bar 5 (Close 180).
            // Break condition: Close 190 >= 180
            td.Update(CreateBar(time.AddDays(9), 190m, 195m, 185m));
            Assert.AreEqual(0m, td.SetupValue.Value, "Setup count should reset to 0 when sequence condition breaks before reaching 9");
            Assert.AreEqual(0m, td.Current.Value, "Current value should reset to 0 when setup is interrupted");
            Assert.AreEqual(0m, td.CountdownValue.Value);

            // Verify Sell Setup interruption as well
            td.Reset();
            td.Update(CreateBar(time.AddDays(1), 100m, 105m, 95m));
            td.Update(CreateBar(time.AddDays(2), 105m, 110m, 100m));
            td.Update(CreateBar(time.AddDays(3), 110m, 115m, 105m));
            td.Update(CreateBar(time.AddDays(4), 115m, 120m, 110m));

            td.Update(CreateBar(time.AddDays(5), 120m, 125m, 115m)); // > 100 -> +1
            Assert.AreEqual(1m, td.SetupValue.Value);

            td.Update(CreateBar(time.AddDays(6), 125m, 130m, 120m)); // > 105 -> +2
            Assert.AreEqual(2m, td.SetupValue.Value);

            // Bar 7: Bar 4 bars ago is bar 3 (Close 110). Break condition: Close 100 <= 110
            td.Update(CreateBar(time.AddDays(7), 100m, 105m, 95m));
            Assert.AreEqual(0m, td.SetupValue.Value, "Sell setup count should reset to 0 when sequence condition breaks before reaching 9");
            Assert.AreEqual(0m, td.Current.Value);
        }

        [Test]
        public void TestCountdownPhase()
        {
            var td = new TDSequential();
            var time = new DateTime(2023, 1, 1);

            // 1. Feed 4 warmup bars
            var warmupCloses = new[] { 300m, 290m, 280m, 270m };
            for (var i = 0; i < warmupCloses.Length; i++)
            {
                td.Update(CreateBar(time.AddDays(i + 1), warmupCloses[i], warmupCloses[i] + 5m, warmupCloses[i] - 5m));
            }

            // 2. Feed 9 Buy Setup bars to complete Setup
            for (var i = 1; i <= 9; i++)
            {
                var close = 260m - (i - 1) * 5m;
                var dayIndex = 4 + i;
                td.Update(CreateBar(time.AddDays(dayIndex), close, close + 5m, close - 5m));
            }

            Assert.AreEqual(-9m, td.SetupValue.Value, "Buy Setup should have completed with -9");
            Assert.AreEqual(0m, td.CountdownValue.Value, "CountdownValue should be 0 on bar 9 of Setup");

            // 3. Post-Setup completion: feed 13 Countdown bars where Close <= Low of 2 bars prior
            // For Buy Countdown, condition is Close <= Low of 2 bars prior.
            // By maintaining a downward trend where each bar's close is below the low of 2 bars prior:
            // At bar 13 (step 9 setup): Close = 220, Low = 215.
            // Bar 14 (countdown 1): Close = 210 (<= Bar 12 Low: 220), Low = 205
            // Bar 15 (countdown 2): Close = 200 (<= Bar 13 Low: 215), Low = 195
            // ...
            for (var k = 1; k <= 13; k++)
            {
                var close = 220m - k * 10m;
                var low = close - 5m;
                var high = close + 5m;
                var dayIndex = 13 + k;

                td.Update(CreateBar(time.AddDays(dayIndex), close, high, low));

                Assert.AreEqual(-k, td.CountdownValue.Value, $"Countdown value should be -{k} at countdown step {k}");
                Assert.AreEqual(-k, td.Current.Value, $"Current value should match Countdown value -{k} at countdown step {k}");
                Assert.AreEqual(0m, td.SetupValue.Value, "SetupValue should be 0 during countdown phase");
            }

            // 4. Test Sell Countdown sequence progression up to 13 as well
            var tdSell = new TDSequential();
            for (var i = 0; i < 4; i++)
            {
                var c = 100m + i * 5m;
                tdSell.Update(CreateBar(time.AddDays(i + 1), c, c + 5m, c - 5m));
            }

            // 9 Sell Setup bars
            for (var i = 1; i <= 9; i++)
            {
                var close = 120m + (i - 1) * 5m;
                tdSell.Update(CreateBar(time.AddDays(4 + i), close, close + 5m, close - 5m));
            }

            Assert.AreEqual(9m, tdSell.SetupValue.Value);
            Assert.AreEqual(0m, tdSell.CountdownValue.Value);

            // 13 Sell Countdown bars where Close >= High of 2 bars prior
            for (var k = 1; k <= 13; k++)
            {
                var close = 160m + k * 10m;
                var high = close + 5m;
                var low = close - 5m;

                tdSell.Update(CreateBar(time.AddDays(13 + k), close, high, low));

                Assert.AreEqual(k, tdSell.CountdownValue.Value, $"Sell Countdown value should be {k} at countdown step {k}");
                Assert.AreEqual(k, tdSell.Current.Value);
                Assert.AreEqual(0m, tdSell.SetupValue.Value);
            }
        }

        [Test]
        public void TestResetBehavior()
        {
            var td = new TDSequential();
            var time = new DateTime(2023, 1, 1);

            // Feed enough bars to warm up and advance into setup
            for (var i = 1; i <= 10; i++)
            {
                var price = 200m - i * 5m;
                td.Update(CreateBar(time.AddDays(i), price, price + 5m, price - 5m));
            }

            Assert.IsTrue(td.IsReady);
            Assert.AreNotEqual(0m, td.SetupValue.Value);
            Assert.AreNotEqual(0, td.Samples);

            // Call Reset
            td.Reset();

            // Verify reset state
            Assert.IsFalse(td.IsReady, "IsReady should be false after reset");
            Assert.AreEqual(0, td.Samples, "Samples should be 0 after reset");
            Assert.AreEqual(0m, td.SetupValue.Value, "SetupValue should be 0 after reset");
            Assert.AreEqual(0m, td.CountdownValue.Value, "CountdownValue should be 0 after reset");
            Assert.AreEqual(0m, td.Current.Value, "Current.Value should be 0 after reset");
            Assert.AreEqual(DateTime.MinValue, td.SetupValue.Time);
            Assert.AreEqual(DateTime.MinValue, td.CountdownValue.Time);

            // Verify warmup is required again (samples 1..4 not ready, sample 5 ready)
            for (var i = 1; i <= 4; i++)
            {
                td.Update(CreateBar(time.AddDays(10 + i), 100m, 105m, 95m));
                Assert.IsFalse(td.IsReady, $"Indicator should not be ready at sample {i} post-reset");
            }

            td.Update(CreateBar(time.AddDays(15), 90m, 95m, 85m));
            Assert.IsTrue(td.IsReady, "Indicator should be ready on 5th bar post-reset");
        }
    }
}

