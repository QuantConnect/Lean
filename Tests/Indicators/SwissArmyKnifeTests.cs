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
        public override void ResetsProperly()
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

        [Test]
        public void ExposesAllToolsAsSubIndicators()
        {
            var indicator = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Gauss);
            var gauss = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Gauss);
            var butter = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Butter);
            var highPass = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.HighPass);
            var twoPoleHighPass = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.TwoPoleHighPass);
            var bandPass = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.BandPass);

            foreach (var data in TestHelper.GetDataStream(100))
            {
                indicator.Update(data);
                gauss.Update(data);
                butter.Update(data);
                highPass.Update(data);
                twoPoleHighPass.Update(data);
                bandPass.Update(data);

                Assert.AreEqual(gauss.Current.Value, indicator.Gauss.Current.Value);
                Assert.AreEqual(butter.Current.Value, indicator.Butter.Current.Value);
                Assert.AreEqual(highPass.Current.Value, indicator.HighPass.Current.Value);
                Assert.AreEqual(twoPoleHighPass.Current.Value, indicator.TwoPoleHighPass.Current.Value);
                Assert.AreEqual(bandPass.Current.Value, indicator.BandPass.Current.Value);
                Assert.AreEqual(indicator.Gauss.Current.Value, indicator.Current.Value);
            }
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
