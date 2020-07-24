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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class BollingerBandsTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new BollingerBands(20, 2.0m);
        }

        protected override string TestFileName => "spy_bollinger_bands.txt";

        protected override string TestColumnName => "Bollinger Bands® 20 2 Bottom";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double) ((BollingerBands) indicator).LowerBand.Current.Value, 1e-3);

        [Test]
        public void ComparesWithExternalDataMiddleBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator() as BollingerBands,
                TestFileName,
                "Moving Average 20",
                ind => (double) ind.MiddleBand.Current.Value
            );
        }

        [Test]
        public void ComparesWithExternalDataUpperBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator() as BollingerBands,
                TestFileName,
                "Bollinger Bands® 20 2 Top",
                ind => (double) ind.UpperBand.Current.Value
            );
        }

        [Test]
        public void ComparesWithExternalDataBandWidth()
        {
            TestHelper.TestIndicator(
                CreateIndicator() as BollingerBands,
                TestFileName,
                "BandWidth",
                ind => (double)ind.BandWidth.Current.Value
            );
        }

        [Test]
        public void ComparesWithExternalDataPercentB()
        {
            TestHelper.TestIndicator(
                CreateIndicator() as BollingerBands,
                TestFileName,
                "%B",
                ind => (double)ind.PercentB.Current.Value
            );
        }

        [Test]
        public void ResetsProperly()
        {
            var bb = new BollingerBands(2, 2m);
            bb.Update(DateTime.Today, 1m);

            Assert.IsFalse(bb.IsReady);
            bb.Update(DateTime.Today.AddSeconds(1), 2m);
            Assert.IsTrue(bb.IsReady);
            Assert.IsTrue(bb.StandardDeviation.IsReady);
            Assert.IsTrue(bb.LowerBand.IsReady);
            Assert.IsTrue(bb.MiddleBand.IsReady);
            Assert.IsTrue(bb.UpperBand.IsReady);
            Assert.IsTrue(bb.BandWidth.IsReady);
            Assert.IsTrue(bb.PercentB.IsReady);

            bb.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(bb);
            TestHelper.AssertIndicatorIsInDefaultState(bb.StandardDeviation);
            TestHelper.AssertIndicatorIsInDefaultState(bb.LowerBand);
            TestHelper.AssertIndicatorIsInDefaultState(bb.MiddleBand);
            TestHelper.AssertIndicatorIsInDefaultState(bb.UpperBand);
            TestHelper.AssertIndicatorIsInDefaultState(bb.BandWidth);
            TestHelper.AssertIndicatorIsInDefaultState(bb.PercentB);
        }

        [Test]
        public void LowerUpperBandUpdateOnce()
        {
            var bb = new BollingerBands(2, 2m);
            var lowerBandUpdateCount = 0;
            var upperBandUpdateCount = 0;
            bb.LowerBand.Updated += (sender, updated) =>
            {
                lowerBandUpdateCount++;
            };
            bb.UpperBand.Updated += (sender, updated) =>
            {
                upperBandUpdateCount++;
            };

            Assert.AreEqual(0, lowerBandUpdateCount);
            Assert.AreEqual(0, upperBandUpdateCount);
            bb.Update(DateTime.Today, 1m);

            Assert.AreEqual(1, lowerBandUpdateCount);
            Assert.AreEqual(1, upperBandUpdateCount);
        }
    }
}