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
    public class SwissArmyKnifeTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Gauss);
        }

        protected override string TestFileName => "spy_swiss.txt";

        protected override string TestColumnName => "Gauss";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion =>
            (indicator, expected) => 
                Assert.AreEqual(expected, (double) indicator.Current.Value, 0.01);

        [Test]
        public void ResetsProperly()
        {
            var sak = new SwissArmyKnife(4, 0.1, SwissArmyKnifeTool.BandPass);

            foreach (var data in TestHelper.GetDataStream(5))
            {
                sak.Update(data);
            }
            Assert.IsTrue(sak.IsReady);
            Assert.AreNotEqual(0m, sak.Current.Value);
            Assert.AreNotEqual(0, sak.Samples);

            sak.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(sak);
        }

        [Test]
        public void ComparesBandPassAgainstExternalData()
        {
            var indicator = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.BandPass);
            RunTestIndicator(indicator, "BP", 0.043m);
        }

        [Test]
        public void Compares2PHPAgainstExternalData()
        {
            var indicator = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.TwoPoleHighPass);
            RunTestIndicator(indicator, "2PHP", 0.01m);
        }

        [Test]
        public void ComparesHPAgainstExternalData()
        {
            var indicator = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.HighPass);
            RunTestIndicator(indicator, "HP", 0.01m);
        }

        [Test]
        public void ComparesButterAgainstExternalData()
        {
            var indicator = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Butter);
            RunTestIndicator(indicator, "Butter", 0.01m);
        }

        private void RunTestIndicator(IndicatorBase<IndicatorDataPoint> indicator, string field, decimal variance)
        {
            TestHelper.TestIndicator(indicator, TestFileName, field, (actual, expected) => { AssertResult(expected, actual.Current.Value, variance); });
        }

        private static void AssertResult(double expected, decimal actual, decimal variance)
        {
            System.Diagnostics.Debug.WriteLine(expected + "," + actual + "," + Math.Abs((decimal)expected - actual));
            Assert.IsTrue(Math.Abs((decimal)expected - actual) < variance);
        }
    }
}