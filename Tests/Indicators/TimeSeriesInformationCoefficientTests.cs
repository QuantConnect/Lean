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
    public class TimeSeriesInformationCoefficientTests
    {
        private static readonly decimal[] Prices =
        {
            100m, 98.94m, 96.87m, 97.75m, 95.24m, 95.45m, 94.68m, 92.17m, 92.21m, 89.65m, 89.29m, 86.99m, 84.85m, 84.47m, 86.13m
        };

        // scipy.stats.spearmanr of each period's 1-period rate of change and the next period's return, over the last 5 periods
        private static readonly double?[] ExpectedValues =
        {
            null, null, null, null, null, null, -0.8, -0.6, -0.6, -0.6, -0.6, -0.8, -0.9, -0.9, -0.3
        };

        [Test]
        public void ComputesSpearmanCorrelationOfSignalAndNextReturn()
        {
            var indicator = new TimeSeriesInformationCoefficient(new RateOfChange(1), 5);
            var time = new DateTime(2024, 1, 1);

            for (var i = 0; i < Prices.Length; i++)
            {
                indicator.Update(CreateBar(time.AddDays(i), Prices[i]));

                Assert.AreEqual(ExpectedValues[i].HasValue, indicator.IsReady, $"Index {i}");
                Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [Test]
        public void WarmUpPeriodIsTheSamplesNeededToBeReady()
        {
            var indicator = new TimeSeriesInformationCoefficient(new RelativeStrengthIndex(3), 4);
            var time = new DateTime(2024, 1, 1);

            for (var i = 0; i < indicator.WarmUpPeriod; i++)
            {
                Assert.IsFalse(indicator.IsReady);
                indicator.Update(CreateBar(time.AddDays(i), Prices[i]));
            }

            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void UpdatesItsSignal()
        {
            var signal = new RateOfChange(1);
            var indicator = new TimeSeriesInformationCoefficient(signal, 5);
            var time = new DateTime(2024, 1, 1);

            indicator.Update(CreateBar(time, 100m));
            indicator.Update(CreateBar(time.AddDays(1), 110m));

            Assert.AreEqual(2, signal.Samples);
            Assert.AreEqual(0.1m, signal.Current.Value);
        }

        [Test]
        public void ResetsItsSignal()
        {
            var signal = new RateOfChange(1);
            var indicator = new TimeSeriesInformationCoefficient(signal, 5);
            var time = new DateTime(2024, 1, 1);
            for (var i = 0; i < Prices.Length; i++)
            {
                indicator.Update(CreateBar(time.AddDays(i), Prices[i]));
            }
            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            Assert.IsFalse(indicator.IsReady);
            Assert.AreEqual(0, indicator.Current.Value);
            Assert.AreEqual(0, signal.Samples);

            for (var i = 0; i < Prices.Length; i++)
            {
                indicator.Update(CreateBar(time.AddDays(i), Prices[i]));
                Assert.AreEqual(ExpectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [Test]
        public void ConstantReturnsGiveZero()
        {
            var indicator = new TimeSeriesInformationCoefficient(new RateOfChange(1), 3);
            var time = new DateTime(2024, 1, 1);

            for (var i = 0; i < 10; i++)
            {
                indicator.Update(CreateBar(time.AddDays(i), 100m));
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(0m, indicator.Current.Value);
        }

        [Test]
        public void SkipsTheReturnFromAZeroPrice()
        {
            var prices = new[] { 100m, 101m, 0m, 102m, 103.5m, 103m, 105m, 104m, 106.5m };
            // scipy.stats.spearmanr over the last 3 pairs, leaving out the pair whose previous price is zero
            var expectedValues = new double?[] { null, null, null, null, null, -0.5, -1, -1, -1 };
            var indicator = new TimeSeriesInformationCoefficient(new RateOfChange(1), 3);
            var time = new DateTime(2024, 1, 1);

            for (var i = 0; i < prices.Length; i++)
            {
                indicator.Update(CreateBar(time.AddDays(i), prices[i]));

                Assert.AreEqual(expectedValues[i].HasValue, indicator.IsReady, $"Index {i}");
                Assert.AreEqual(expectedValues[i] ?? 0, (double)indicator.Current.Value, 1e-9, $"Index {i}");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ThrowsWhenItsSignalIsAlsoUpdatedElsewhere(bool updatedElsewhereFirst)
        {
            var signal = new RateOfChange(1);
            var indicator = new TimeSeriesInformationCoefficient(signal, 5);
            var time = new DateTime(2024, 1, 1);

            Assert.Throws<InvalidOperationException>(() =>
            {
                for (var i = 0; i < Prices.Length; i++)
                {
                    var bar = CreateBar(time.AddDays(i), Prices[i]);
                    if (updatedElsewhereFirst)
                    {
                        signal.Update(bar.EndTime, bar.Close);
                        indicator.Update(bar);
                    }
                    else
                    {
                        indicator.Update(bar);
                        signal.Update(bar.EndTime, bar.Close);
                    }
                }
            });
        }

        [Test]
        public void ThrowsWhenItsSignalRejectsAnInput()
        {
            var signal = new RateOfChange(1);
            var time = new DateTime(2024, 1, 1);
            signal.Update(CreateBar(time.AddDays(10), 100m));
            var indicator = new TimeSeriesInformationCoefficient(signal, 5);

            Assert.Throws<InvalidOperationException>(() => indicator.Update(CreateBar(time, 100m)));
        }

        [TestCase(2)]
        [TestCase(0)]
        public void ThrowsWhenPeriodIsLessThanThree(int period)
        {
            Assert.Throws<ArgumentException>(() => new TimeSeriesInformationCoefficient(new RateOfChange(1), period));
        }

        private static TradeBar CreateBar(DateTime time, decimal price)
        {
            return new TradeBar(time, Symbols.SPY, price, price, price, price, 1000, TimeSpan.FromDays(1));
        }
    }
}
