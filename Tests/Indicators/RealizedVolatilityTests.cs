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
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class RealizedVolatilityTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            return new RealizedVolatility(9);
        }

        protected override string TestFileName => "spy_with_rv.csv";
        protected override string TestColumnName => "RV9";

        private static decimal Expected(params (decimal High, decimal Low)[] bars)
        {
            var sum = 0.0;
            foreach (var (high, low) in bars)
            {
                sum += Math.Pow(Math.Log((double)high / (double)low), 2) / (4 * Math.Log(2));
            }
            return (decimal)Math.Sqrt(sum / bars.Length);
        }

        private static TradeBar Bar(int i, decimal high, decimal low)
        {
            var time = new DateTime(2020, 1, 1).AddDays(i);
            return new TradeBar(time, Symbols.SPY, low, high, low, high, 100, TimeSpan.FromDays(1));
        }

        [Test]
        public void FlatBarCountsAsZeroTermInWindow()
        {
            var rv = new RealizedVolatility(3);
            rv.Update(Bar(0, 11m, 10m));
            rv.Update(Bar(1, 10m, 10m));
            Assert.IsFalse(rv.IsReady);
            rv.Update(Bar(2, 12m, 10m));

            Assert.IsTrue(rv.IsReady);
            Assert.AreEqual((double)Expected((11m, 10m), (10m, 10m), (12m, 10m)), (double)rv.Current.Value, 1e-9);
        }

        [Test]
        public void InvalidBarDuringWarmUpIsSkipped()
        {
            var rv = new RealizedVolatility(3);
            rv.Update(Bar(0, 11m, 10m));
            rv.Update(Bar(1, 9m, 10m));
            rv.Update(Bar(2, 12m, 10m));
            Assert.IsFalse(rv.IsReady);
            rv.Update(Bar(3, 13m, 10m));

            Assert.IsTrue(rv.IsReady);
            Assert.AreEqual((double)Expected((11m, 10m), (12m, 10m), (13m, 10m)), (double)rv.Current.Value, 1e-9);
        }

        [Test]
        public void InvalidBarKeepsPreviousValue()
        {
            var rv = new RealizedVolatility(2);
            rv.Update(Bar(0, 11m, 10m));
            rv.Update(Bar(1, 12m, 10m));
            var value = rv.Current.Value;
            rv.Update(Bar(2, 9m, 10m));

            Assert.AreEqual(value, rv.Current.Value);
        }
    }
}
