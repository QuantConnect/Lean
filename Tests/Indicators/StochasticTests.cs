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

using QuantConnect.Indicators;
using NUnit.Framework;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class StochasticTests
    {
        [Test]
        public void ComparesAgainstExternalDataOnStochasticsK()
        {
            var stochastics = new Stochastic("sto", 12, 3, 5);

            const double epsilon = 1e-3;

            TestHelper.TestIndicator(stochastics, "spy_with_stoch12k3.txt", "Stochastics 12 %K 3",
            (ind, expected) => Assert.AreEqual(expected, (double)((Stochastic)ind).StochK.Current.Value, epsilon)

            );
        }

        [Test]
        public void ComparesAgainstExternalDataOnStochasticsD()
        {
            var stochastics = new Stochastic("sto", 12, 3, 5);

            const double epsilon = 1e-3;
            TestHelper.TestIndicator(stochastics, "spy_with_stoch12k3.txt", "%D 5",
                 (ind, expected) => Assert.AreEqual(expected, (double)((Stochastic)ind).StochD.Current.Value, epsilon)

                );
        }
    }
}
