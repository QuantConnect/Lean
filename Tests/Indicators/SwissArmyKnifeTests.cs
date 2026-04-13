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
        public void SubIndicatorsComputeAllToolsSimultaneously()
        {
            var sak = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Gauss);

            var gaussStandalone = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Gauss);
            var butterStandalone = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.Butter);
            var hpStandalone = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.HighPass);
            var tphpStandalone = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.TwoPoleHighPass);
            var bpStandalone = new SwissArmyKnife(20, 0.1, SwissArmyKnifeTool.BandPass);

            foreach (var data in TestHelper.GetDataStream(25))
            {
                sak.Update(data);
                gaussStandalone.Update(data);
                butterStandalone.Update(data);
                hpStandalone.Update(data);
                tphpStandalone.Update(data);
                bpStandalone.Update(data);

                Assert.AreEqual(gaussStandalone.Current.Value, sak.Gauss.Current.Value);
                Assert.AreEqual(butterStandalone.Current.Value, sak.Butter.Current.Value);
                Assert.AreEqual(hpStandalone.Current.Value, sak.HighPass.Current.Value);
                Assert.AreEqual(tphpStandalone.Current.Value, sak.TwoPoleHighPass.Current.Value);
                Assert.AreEqual(bpStandalone.Current.Value, sak.BandPass.Current.Value);
            }
        }

        [Test]
        public void SubIndicatorsResetProperly()
        {
            var sak = new SwissArmyKnife(4, 0.1, SwissArmyKnifeTool.Gauss);

            foreach (var data in TestHelper.GetDataStream(5))
            {
                sak.Update(data);
            }

            Assert.IsTrue(sak.Gauss.IsReady);
            Assert.IsTrue(sak.Butter.IsReady);
            Assert.IsTrue(sak.HighPass.IsReady);
            Assert.IsTrue(sak.TwoPoleHighPass.IsReady);
            Assert.IsTrue(sak.BandPass.IsReady);

            sak.Reset();

            Assert.IsFalse(sak.Gauss.IsReady);
            Assert.IsFalse(sak.Butter.IsReady);
            Assert.IsFalse(sak.HighPass.IsReady);
            Assert.IsFalse(sak.TwoPoleHighPass.IsReady);
            Assert.IsFalse(sak.BandPass.IsReady);
        }

        [Test]
        public void PrimaryToolMatchesCurrentValue()
        {
            foreach (SwissArmyKnifeTool tool in Enum.GetValues(typeof(SwissArmyKnifeTool)))
            {
                var sak = new SwissArmyKnife(20, 0.1, tool);

                foreach (var data in TestHelper.GetDataStream(25))
                {
                    sak.Update(data);

                    switch (tool)
                    {
                        case SwissArmyKnifeTool.Gauss:
                            Assert.AreEqual(sak.Gauss.Current.Value, sak.Current.Value);
                            break;
                        case SwissArmyKnifeTool.Butter:
                            Assert.AreEqual(sak.Butter.Current.Value, sak.Current.Value);
                            break;
                        case SwissArmyKnifeTool.HighPass:
                            Assert.AreEqual(sak.HighPass.Current.Value, sak.Current.Value);
                            break;
                        case SwissArmyKnifeTool.TwoPoleHighPass:
                            Assert.AreEqual(sak.TwoPoleHighPass.Current.Value, sak.Current.Value);
                            break;
                        case SwissArmyKnifeTool.BandPass:
                            Assert.AreEqual(sak.BandPass.Current.Value, sak.Current.Value);
                            break;
                    }
                }
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
