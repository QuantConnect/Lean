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

using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionStrategiesTests
    {
        [Test]
        public void BuildsCoveredCallStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.CoveredCall(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.CoveredCall.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(1, strategy.OptionLegs.Count);
            var optionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Call, optionLeg.Right);
            Assert.AreEqual(strike, optionLeg.Strike);
            Assert.AreEqual(expiration, optionLeg.Expiration);
            Assert.AreEqual(-1, optionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(100, underlyingLeg.Quantity);
        }
    }
}
