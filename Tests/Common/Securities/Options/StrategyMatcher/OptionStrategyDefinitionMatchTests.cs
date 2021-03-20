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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Securities.Option.StrategyMatcher;
using static QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    [TestFixture]
    public class OptionStrategyDefinitionMatchTests
    {
        [Test]
        public void CreatesOptionStrategy_WithMinimumMultiplier_FromLegMatches()
        {
            // OptionStrategyDefinitions.BearCallSpread
            // 0: -1 Call
            // 1: +1 Call w/ Strike <= leg[0].Strike

            // these positions support matching index0 3 times and index1 2 times and the multiplier
            // for the definition match should be 2, despite leg0 having multiplier=3
            var positions = OptionPositionCollection.Empty.AddRange(
                new OptionPosition(Call[110], -3),
                new OptionPosition(Call[100], +2)
            );

            var match = OptionStrategyDefinitions.BearCallSpread.Match(positions).Single();
            Assert.AreEqual(3, match.Legs[0].Multiplier);
            Assert.AreEqual(2, match.Legs[1].Multiplier);
            Assert.AreEqual(2, match.Multiplier);

            var strategy = match.CreateStrategy();
            Assert.AreEqual(-2, strategy.OptionLegs[0].Quantity);
            Assert.AreEqual(+2, strategy.OptionLegs[1].Quantity);
        }
    }
}
