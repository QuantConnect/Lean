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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AccelerationBandsTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new AccelerationBands(period: 20, width: 4m);
        }

        protected override string TestFileName => "spy_acceleration_bands_20_4.txt";

        protected override string TestColumnName => "MiddleBand";

        [Test]
        public void ComparesWithExternalDataLowerBand()
        {
            var abands = CreateIndicator();
            TestHelper.TestIndicator(
                abands,
                "spy_acceleration_bands_20_4.txt",
                "LowerBand",
                (ind, expected) => Assert.AreEqual(expected, (double) ((AccelerationBands) ind).LowerBand.Current.Value,
                    delta: 1e-4, message: "Lower band test fail.")
            );
        }

        [Test]
        public void ComparesWithExternalDataUpperBand()
        {
            var abands = CreateIndicator();
            TestHelper.TestIndicator(
                abands,
                "spy_acceleration_bands_20_4.txt",
                "UpperBand",
                (ind, expected) => Assert.AreEqual(expected, (double) ((AccelerationBands) ind).UpperBand.Current.Value,
                    delta: 1e-4, message: "Upper band test fail.")
            );
        }

        [Test]
        public void WorksWithLowValues()
        {
            var abands = CreateIndicator();
            var random = new Random();
            var time = DateTime.UtcNow;
            for(int i = 0; i < 40; i++)
            {
                var value = random.NextDouble() * 0.000000000000000000000000000001;
                Assert.DoesNotThrow(() => abands.Update(new TradeBar { High = (decimal)value, Low = (decimal)value, Time = time.AddDays(i)}));
            }
        }
    }
}