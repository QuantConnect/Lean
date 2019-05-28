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
    public class StochasticTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new Stochastic(12, 3, 5);
        }

        protected override string TestFileName => "spy_with_stoch12k3.txt";

        protected override string TestColumnName => "%D 5";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)((Stochastic)indicator).StochD.Current.Value, 1e-3);

        [Test]
        public void ComparesAgainstExternalDataOnStochasticsK()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Stochastics 12 %K 3",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double) ((Stochastic) ind).StochK.Current.Value,
                    1e-3
                )
            );
        }

        [Test]
        public void PrimaryOutputIsFastStochProperty()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Stochastics 12 %K 3",
                (ind, expected) => Assert.AreEqual(
                    (double) ((Stochastic) ind).FastStoch.Current.Value,
                    ind.Current.Value
                )
            );
        }

        [Test]
        public new void ResetsProperly()
        {
            var stochastics = CreateIndicator() as Stochastic;

            foreach (var bar in TestHelper.GetTradeBarStream(TestFileName, false))
            {
                stochastics.Update(bar);
            }
            Assert.IsTrue(stochastics.IsReady);
            Assert.IsTrue(stochastics.FastStoch.IsReady);
            Assert.IsTrue(stochastics.StochK.IsReady);
            Assert.IsTrue(stochastics.StochD.IsReady);

            stochastics.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(stochastics);
            TestHelper.AssertIndicatorIsInDefaultState(stochastics.FastStoch);
            TestHelper.AssertIndicatorIsInDefaultState(stochastics.StochK);
            TestHelper.AssertIndicatorIsInDefaultState(stochastics.StochD);
        }

        [Test]
        public void HandlesEqualMinAndMax()
        {
            var reference = DateTime.Now;
            var stochastics = new Stochastic("sto", 2, 2, 2);
            for (var i = 0; i < 4; i++)
            {
                var bar = new TradeBar(reference.AddSeconds(i), Symbols.SPY, 1, 1, 1, 1, 1);
                stochastics.Update(bar);
                Assert.AreEqual(0m, stochastics.Current.Value);
            }
        }
    }
}