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

    /// <summary>
    /// Tests for the Klinger Volume Oscillator (KVO) indicator
    /// </summary>
    public class KlingerVolumeOscillatorTests : CommonIndicatorTests<TradeBar>
    {
        /// <summary>
        /// Generated Klinger Volume Oscillator test data from talipp
        /// </summary>
        protected override string TestFileName => "spy_with_kvo.csv";

        /// <summary>
        /// Generated column for KVO(5,10) from talipp
        /// </summary>
        protected override string TestColumnName => "KVO5_10";

        /// <summary>
        /// Required by CommonIndicatorTests: return a fresh instance of your indicator.
        /// </summary>
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            RenkoBarSize = 1m;

            // match generated data from talipp
            return new KlingerVolumeOscillator(fastPeriod: 5, slowPeriod: 10);
        }

        /// <summary>
        /// This indicator doesn't accept Renko Bars as input. Skip this test.
        /// </summary>
        public override void AcceptsRenkoBarsAsInput()
        {
        }

        [Test]
        public void SignalLineIsReadyAfterWarmUpPeriod()
        {
            var indicator = CreateIndicator() as KlingerVolumeOscillator;
            Assert.IsFalse(indicator.Signal.IsReady);
            // Warm up the indicator
            for (int i = 0; i < indicator.WarmUpPeriod; i++)
            {
                indicator.Update(new TradeBar { Time = DateTime.UtcNow.AddDays(i), Close = 100 + i, Volume = 1000 });
            }
            Assert.IsTrue(indicator.Signal.IsReady);
        }
    }
}
