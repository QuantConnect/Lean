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
using System.Linq;
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

        [Test]
        public void BuildsProtectiveCallStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ProtectiveCall(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ProtectiveCall.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(1, strategy.OptionLegs.Count);
            var optionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Call, optionLeg.Right);
            Assert.AreEqual(strike, optionLeg.Strike);
            Assert.AreEqual(expiration, optionLeg.Expiration);
            Assert.AreEqual(1, optionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(-100, underlyingLeg.Quantity);
        }

        [Test]
        public void BuildsCoveredPutStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.CoveredPut(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.CoveredPut.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(1, strategy.OptionLegs.Count);
            var optionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Put, optionLeg.Right);
            Assert.AreEqual(strike, optionLeg.Strike);
            Assert.AreEqual(expiration, optionLeg.Expiration);
            Assert.AreEqual(-1, optionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(-100, underlyingLeg.Quantity);
        }

        [Test]
        public void BuildsProtectivePutStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ProtectivePut(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ProtectivePut.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(1, strategy.OptionLegs.Count);
            var optionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Put, optionLeg.Right);
            Assert.AreEqual(strike, optionLeg.Strike);
            Assert.AreEqual(expiration, optionLeg.Expiration);
            Assert.AreEqual(1, optionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(100, underlyingLeg.Quantity);
        }

        [Test]
        public void BuildsNakedCallStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.NakedCall(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.NakedCall.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(1, strategy.OptionLegs.Count);
            var optionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Call, optionLeg.Right);
            Assert.AreEqual(strike, optionLeg.Strike);
            Assert.AreEqual(expiration, optionLeg.Expiration);
            Assert.AreEqual(-1, optionLeg.Quantity);

            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);
        }

        [Test]
        public void BuildsStraddleStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.Straddle(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.Straddle.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Call);
            Assert.AreEqual(strike, callLeg.Strike);
            Assert.AreEqual(expiration, callLeg.Expiration);
            Assert.AreEqual(1, callLeg.Quantity);

            var putLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Put);
            Assert.AreEqual(strike, putLeg.Strike);
            Assert.AreEqual(expiration, putLeg.Expiration);
            Assert.AreEqual(1, putLeg.Quantity);
        }

        [Test]
        public void BuildsShortStraddleStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ShortStraddle(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortStraddle.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Call);
            Assert.AreEqual(strike, callLeg.Strike);
            Assert.AreEqual(expiration, callLeg.Expiration);
            Assert.AreEqual(-1, callLeg.Quantity);

            var putLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Put);
            Assert.AreEqual(strike, putLeg.Strike);
            Assert.AreEqual(expiration, putLeg.Expiration);
            Assert.AreEqual(-1, putLeg.Quantity);
        }

        [Test]
        public void FailsBuildingStrangleStrategyWithInvalidStrikePrices()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var expiration = new DateTime(2023, 08, 18);

            // Same strikes
            var callStrike = 350m;
            var putStrike = 350m;
            Assert.Throws<ArgumentException>(() => OptionStrategies.Strangle(canonicalOptionSymbol, callStrike, putStrike, expiration));

            // Call strike < put strike
            callStrike = 340m;
            putStrike = 350m;
            Assert.Throws<ArgumentException>(() => OptionStrategies.Strangle(canonicalOptionSymbol, callStrike, putStrike, expiration));
        }

        [Test]
        public void BuildsStrangleStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var callStrike = 350m;
            var putStrike = 340m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.Strangle(canonicalOptionSymbol, callStrike, putStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.Strangle.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Call);
            Assert.AreEqual(callStrike, callLeg.Strike);
            Assert.AreEqual(expiration, callLeg.Expiration);
            Assert.AreEqual(1, callLeg.Quantity);

            var putLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Put);
            Assert.AreEqual(putStrike, putLeg.Strike);
            Assert.AreEqual(expiration, putLeg.Expiration);
            Assert.AreEqual(1, putLeg.Quantity);
        }

        [Test]
        public void BuildsShortStrangleStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var callStrike = 350m;
            var putStrike = 340m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ShortStrangle(canonicalOptionSymbol, callStrike, putStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortStrangle.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Call);
            Assert.AreEqual(callStrike, callLeg.Strike);
            Assert.AreEqual(expiration, callLeg.Expiration);
            Assert.AreEqual(-1, callLeg.Quantity);

            var putLeg = strategy.OptionLegs.Single(leg => leg.Right == OptionRight.Put);
            Assert.AreEqual(putStrike, putLeg.Strike);
            Assert.AreEqual(expiration, putLeg.Expiration);
            Assert.AreEqual(-1, putLeg.Quantity);
        }

        [Test]
        public void BuildsNakedPutStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.NakedPut(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.NakedPut.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(1, strategy.OptionLegs.Count);
            var optionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Put, optionLeg.Right);
            Assert.AreEqual(strike, optionLeg.Strike);
            Assert.AreEqual(expiration, optionLeg.Expiration);
            Assert.AreEqual(-1, optionLeg.Quantity);

            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);
        }

        [Test]
        public void FailsBuildingButterflyCallStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var expiration = new DateTime(2023, 08, 18);

            var higherStrike = 350m;
            var lowerStrike = 300m;
            var middleStrike = 325m;

            // Unordered strikes
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, lowerStrike, middleStrike, higherStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, lowerStrike, higherStrike, middleStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, middleStrike, lowerStrike, higherStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, middleStrike, higherStrike, lowerStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, higherStrike, lowerStrike, middleStrike, expiration));

            // Uneven inter-strike distances
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, lowerStrike, middleStrike + 1, higherStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyCall(canonicalOptionSymbol, lowerStrike, middleStrike - 1, higherStrike, expiration));
        }

        [Test]
        public void BuildsButterflyCallStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var higherStrike = 350m;
            var lowerStrike = 300m;
            var middleStrike = 325m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ButterflyCall(canonicalOptionSymbol, higherStrike, middleStrike, lowerStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ButterflyCall.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(3, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var higherStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == higherStrike);
            Assert.AreEqual(OptionRight.Call, higherStrikeLeg.Right);
            Assert.AreEqual(expiration, higherStrikeLeg.Expiration);
            Assert.AreEqual(1, higherStrikeLeg.Quantity);

            var middleStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == middleStrike);
            Assert.AreEqual(OptionRight.Call, middleStrikeLeg.Right);
            Assert.AreEqual(expiration, middleStrikeLeg.Expiration);
            Assert.AreEqual(-2, middleStrikeLeg.Quantity);

            var lowerStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == lowerStrike);
            Assert.AreEqual(OptionRight.Call, lowerStrikeLeg.Right);
            Assert.AreEqual(expiration, lowerStrikeLeg.Expiration);
            Assert.AreEqual(1, lowerStrikeLeg.Quantity);
        }

        [Test]
        public void BuildsShortButterflyCallStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var higherStrike = 350m;
            var lowerStrike = 300m;
            var middleStrike = 325m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ShortButterflyCall(canonicalOptionSymbol, higherStrike, middleStrike, lowerStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortButterflyCall.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(3, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var higherStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == higherStrike);
            Assert.AreEqual(OptionRight.Call, higherStrikeLeg.Right);
            Assert.AreEqual(expiration, higherStrikeLeg.Expiration);
            Assert.AreEqual(-1, higherStrikeLeg.Quantity);

            var middleStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == middleStrike);
            Assert.AreEqual(OptionRight.Call, middleStrikeLeg.Right);
            Assert.AreEqual(expiration, middleStrikeLeg.Expiration);
            Assert.AreEqual(2, middleStrikeLeg.Quantity);

            var lowerStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == lowerStrike);
            Assert.AreEqual(OptionRight.Call, lowerStrikeLeg.Right);
            Assert.AreEqual(expiration, lowerStrikeLeg.Expiration);
            Assert.AreEqual(-1, lowerStrikeLeg.Quantity);
        }

        [Test]
        public void FailsBuildingButterflyPutStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var expiration = new DateTime(2023, 08, 18);

            var higherStrike = 350m;
            var lowerStrike = 300m;
            var middleStrike = 325m;

            // Unordered strikes
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, lowerStrike, middleStrike, higherStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, lowerStrike, higherStrike, middleStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, middleStrike, lowerStrike, higherStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, middleStrike, higherStrike, lowerStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, higherStrike, lowerStrike, middleStrike, expiration));

            // Uneven inter-strike distances
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, lowerStrike, middleStrike + 1, higherStrike, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.ButterflyPut(canonicalOptionSymbol, lowerStrike, middleStrike - 1, higherStrike, expiration));
        }

        [Test]
        public void BuildsButterflyPutStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var higherStrike = 350m;
            var lowerStrike = 300m;
            var middleStrike = 325m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ButterflyPut(canonicalOptionSymbol, higherStrike, middleStrike, lowerStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ButterflyPut.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(3, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var higherStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == higherStrike);
            Assert.AreEqual(OptionRight.Put, higherStrikeLeg.Right);
            Assert.AreEqual(expiration, higherStrikeLeg.Expiration);
            Assert.AreEqual(1, higherStrikeLeg.Quantity);

            var middleStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == middleStrike);
            Assert.AreEqual(OptionRight.Put, middleStrikeLeg.Right);
            Assert.AreEqual(expiration, middleStrikeLeg.Expiration);
            Assert.AreEqual(-2, middleStrikeLeg.Quantity);

            var lowerStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == lowerStrike);
            Assert.AreEqual(OptionRight.Put, lowerStrikeLeg.Right);
            Assert.AreEqual(expiration, lowerStrikeLeg.Expiration);
            Assert.AreEqual(1, lowerStrikeLeg.Quantity);
        }

        [Test]
        public void BuildsShortButterflyPutStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var higherStrike = 350m;
            var lowerStrike = 300m;
            var middleStrike = 325m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ShortButterflyPut(canonicalOptionSymbol, higherStrike, middleStrike, lowerStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortButterflyPut.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(3, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var higherStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == higherStrike);
            Assert.AreEqual(OptionRight.Put, higherStrikeLeg.Right);
            Assert.AreEqual(expiration, higherStrikeLeg.Expiration);
            Assert.AreEqual(-1, higherStrikeLeg.Quantity);

            var middleStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == middleStrike);
            Assert.AreEqual(OptionRight.Put, middleStrikeLeg.Right);
            Assert.AreEqual(expiration, middleStrikeLeg.Expiration);
            Assert.AreEqual(2, middleStrikeLeg.Quantity);

            var lowerStrikeLeg = strategy.OptionLegs.Single(x => x.Strike == lowerStrike);
            Assert.AreEqual(OptionRight.Put, lowerStrikeLeg.Right);
            Assert.AreEqual(expiration, lowerStrikeLeg.Expiration);
            Assert.AreEqual(-1, lowerStrikeLeg.Quantity);
        }
    }
}
