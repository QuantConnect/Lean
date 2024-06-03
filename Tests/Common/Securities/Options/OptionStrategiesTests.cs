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
        public void BuildsProtectiveCollarStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var callStrike = 350m;
            var putStrike = 300m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ProtectiveCollar(canonicalOptionSymbol, callStrike, putStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ProtectiveCollar.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);
            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callOptionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Call, callOptionLeg.Right);
            Assert.AreEqual(callStrike, callOptionLeg.Strike);
            Assert.AreEqual(expiration, callOptionLeg.Expiration);
            Assert.AreEqual(-1, callOptionLeg.Quantity);

            var putOptionLeg = strategy.OptionLegs[1];
            Assert.AreEqual(OptionRight.Put, putOptionLeg.Right);
            Assert.AreEqual(putStrike, putOptionLeg.Strike);
            Assert.AreEqual(expiration, putOptionLeg.Expiration);
            Assert.AreEqual(1, putOptionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(100, underlyingLeg.Quantity);
        }

        [Test]
        public void BuildsConversionStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.Conversion(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.Conversion.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);
            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callOptionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Call, callOptionLeg.Right);
            Assert.AreEqual(strike, callOptionLeg.Strike);
            Assert.AreEqual(expiration, callOptionLeg.Expiration);
            Assert.AreEqual(-1, callOptionLeg.Quantity);

            var putOptionLeg = strategy.OptionLegs[1];
            Assert.AreEqual(OptionRight.Put, putOptionLeg.Right);
            Assert.AreEqual(strike, putOptionLeg.Strike);
            Assert.AreEqual(expiration, putOptionLeg.Expiration);
            Assert.AreEqual(1, putOptionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(100, underlyingLeg.Quantity);
        }

        [Test]
        public void BuildsReverseConversionStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ReverseConversion(canonicalOptionSymbol, strike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ReverseConversion.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);
            Assert.AreEqual(2, strategy.OptionLegs.Count);

            var callOptionLeg = strategy.OptionLegs[0];
            Assert.AreEqual(OptionRight.Call, callOptionLeg.Right);
            Assert.AreEqual(strike, callOptionLeg.Strike);
            Assert.AreEqual(expiration, callOptionLeg.Expiration);
            Assert.AreEqual(1, callOptionLeg.Quantity);

            var putOptionLeg = strategy.OptionLegs[1];
            Assert.AreEqual(OptionRight.Put, putOptionLeg.Right);
            Assert.AreEqual(strike, putOptionLeg.Strike);
            Assert.AreEqual(expiration, putOptionLeg.Expiration);
            Assert.AreEqual(-1, putOptionLeg.Quantity);

            Assert.AreEqual(1, strategy.UnderlyingLegs.Count);
            var underlyingLeg = strategy.UnderlyingLegs[0];
            Assert.AreEqual(underlying, underlyingLeg.Symbol);
            Assert.AreEqual(-100, underlyingLeg.Quantity);
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

        [Test]
        public void FailsBuildingCallCalendarSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var nearExpiration = new DateTime(2023, 08, 18);
            var farExpiration = new DateTime(2023, 09, 18);

            // Invalid expiration dates
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, DateTime.MinValue, farExpiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, DateTime.MaxValue, farExpiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, DateTime.MinValue));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, DateTime.MaxValue));

            // Switched expiration dates
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, farExpiration, nearExpiration));

            // Same expiration dates
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, nearExpiration));

        }

        [Test]
        public void BuildsCallCalendarSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var nearExpiration = new DateTime(2023, 08, 18);
            var farExpiration = new DateTime(2023, 09, 18);

            var strategy = OptionStrategies.CallCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, farExpiration);

            Assert.AreEqual(OptionStrategyDefinitions.CallCalendarSpread.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var nearExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == nearExpiration);
            Assert.AreEqual(OptionRight.Call, nearExpirationLeg.Right);
            Assert.AreEqual(nearExpiration, nearExpirationLeg.Expiration);
            Assert.AreEqual(-1, nearExpirationLeg.Quantity);

            var farExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == farExpiration);
            Assert.AreEqual(OptionRight.Call, farExpirationLeg.Right);
            Assert.AreEqual(farExpiration, farExpirationLeg.Expiration);
            Assert.AreEqual(1, farExpirationLeg.Quantity);
        }

        [Test]
        public void BuildsShortCallCalendarSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var nearExpiration = new DateTime(2023, 08, 18);
            var farExpiration = new DateTime(2023, 09, 18);

            var strategy = OptionStrategies.ShortCallCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, farExpiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortCallCalendarSpread.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var nearExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == nearExpiration);
            Assert.AreEqual(OptionRight.Call, nearExpirationLeg.Right);
            Assert.AreEqual(nearExpiration, nearExpirationLeg.Expiration);
            Assert.AreEqual(1, nearExpirationLeg.Quantity);

            var farExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == farExpiration);
            Assert.AreEqual(OptionRight.Call, farExpirationLeg.Right);
            Assert.AreEqual(farExpiration, farExpirationLeg.Expiration);
            Assert.AreEqual(-1, farExpirationLeg.Quantity);
        }

        [Test]
        public void FailsBuildingPutCalendarSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var nearExpiration = new DateTime(2023, 08, 18);
            var farExpiration = new DateTime(2023, 09, 18);

            // Invalid expiration dates
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, DateTime.MinValue, farExpiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, DateTime.MaxValue, farExpiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, DateTime.MinValue));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, DateTime.MaxValue));

            // Switched expiration dates
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, farExpiration, nearExpiration));

            // Same expiration dates
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, nearExpiration));

        }

        [Test]
        public void BuildsPutCalendarSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var nearExpiration = new DateTime(2023, 08, 18);
            var farExpiration = new DateTime(2023, 09, 18);

            var strategy = OptionStrategies.PutCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, farExpiration);

            Assert.AreEqual(OptionStrategyDefinitions.PutCalendarSpread.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var nearExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == nearExpiration);
            Assert.AreEqual(OptionRight.Put, nearExpirationLeg.Right);
            Assert.AreEqual(nearExpiration, nearExpirationLeg.Expiration);
            Assert.AreEqual(-1, nearExpirationLeg.Quantity);

            var farExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == farExpiration);
            Assert.AreEqual(OptionRight.Put, farExpirationLeg.Right);
            Assert.AreEqual(farExpiration, farExpirationLeg.Expiration);
            Assert.AreEqual(1, farExpirationLeg.Quantity);
        }

        [Test]
        public void BuildsShortPutCalendarSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike = 350m;
            var nearExpiration = new DateTime(2023, 08, 18);
            var farExpiration = new DateTime(2023, 09, 18);

            var strategy = OptionStrategies.ShortPutCalendarSpread(canonicalOptionSymbol, strike, nearExpiration, farExpiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortPutCalendarSpread.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(2, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var nearExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == nearExpiration);
            Assert.AreEqual(OptionRight.Put, nearExpirationLeg.Right);
            Assert.AreEqual(nearExpiration, nearExpirationLeg.Expiration);
            Assert.AreEqual(1, nearExpirationLeg.Quantity);

            var farExpirationLeg = strategy.OptionLegs.Single(x => x.Expiration == farExpiration);
            Assert.AreEqual(OptionRight.Put, farExpirationLeg.Right);
            Assert.AreEqual(farExpiration, farExpirationLeg.Expiration);
            Assert.AreEqual(-1, farExpirationLeg.Quantity);
        }

        [Test]
        public void FailsBuildingIronCondorStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var expiration = new DateTime(2023, 08, 18);

            var strike1 = 300m;
            var strike2 = 325m;
            var strike3 = 350m;
            var strike4 = 375m;

            // Unordered and repeated strikes
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.IronCondor(canonicalOptionSymbol, strike4, strike3, strike2, strike1, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.IronCondor(canonicalOptionSymbol, strike1, strike1, strike3, strike4, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.IronCondor(canonicalOptionSymbol, strike2, strike1, strike3, strike4, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.IronCondor(canonicalOptionSymbol, strike1, strike3, strike2, strike4, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.IronCondor(canonicalOptionSymbol, strike1, strike2, strike4, strike3, expiration));
        }

        [Test]
        public void BuildsIronCondorStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var strike1 = 300m;
            var strike2 = 325m;
            var strike3 = 350m;
            var strike4 = 375m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.IronCondor(canonicalOptionSymbol, strike1, strike2, strike3, strike4, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.IronCondor.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(4, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var longPutLeg = strategy.OptionLegs.Single(x => x.Strike == strike1);
            Assert.AreEqual(OptionRight.Put, longPutLeg.Right);
            Assert.AreEqual(expiration, longPutLeg.Expiration);
            Assert.AreEqual(1, longPutLeg.Quantity);

            var shortPutLeg = strategy.OptionLegs.Single(x => x.Strike == strike2);
            Assert.AreEqual(OptionRight.Put, shortPutLeg.Right);
            Assert.AreEqual(expiration, shortPutLeg.Expiration);
            Assert.AreEqual(-1, shortPutLeg.Quantity);

            var shortCallLeg = strategy.OptionLegs.Single(x => x.Strike == strike3);
            Assert.AreEqual(OptionRight.Call, shortCallLeg.Right);
            Assert.AreEqual(expiration, shortCallLeg.Expiration);
            Assert.AreEqual(-1, shortCallLeg.Quantity);

            var longCallLeg = strategy.OptionLegs.Single(x => x.Strike == strike4);
            Assert.AreEqual(OptionRight.Call, longCallLeg.Right);
            Assert.AreEqual(expiration, longCallLeg.Expiration);
            Assert.AreEqual(1, longCallLeg.Quantity);
        }

        [Test]
        public void FailsBuildingBoxSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var expiration = new DateTime(2023, 08, 18);

            var strike1 = 300m;
            var strike2 = 325m;

            // Unordered and repeated strikes
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.BoxSpread(canonicalOptionSymbol, strike1, strike2, expiration));
            Assert.Throws<ArgumentException>(
                () => OptionStrategies.BoxSpread(canonicalOptionSymbol, strike1, strike1, expiration));
        }

        [Test]
        public void BuildsBoxSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var higherStrike = 320m;
            var lowerStrike = 300m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.BoxSpread(canonicalOptionSymbol, higherStrike, lowerStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.BoxSpread.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(4, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var longPutLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Put && x.Strike == higherStrike);
            Assert.AreEqual(expiration, longPutLeg.Expiration);
            Assert.AreEqual(1, longPutLeg.Quantity);

            var shortPutLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Put && x.Strike == lowerStrike);
            Assert.AreEqual(expiration, shortPutLeg.Expiration);
            Assert.AreEqual(-1, shortPutLeg.Quantity);

            var shortCallLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Call && x.Strike == higherStrike);
            Assert.AreEqual(expiration, shortCallLeg.Expiration);
            Assert.AreEqual(-1, shortCallLeg.Quantity);

            var longCallLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Call && x.Strike == lowerStrike);
            Assert.AreEqual(expiration, longCallLeg.Expiration);
            Assert.AreEqual(1, longCallLeg.Quantity);
        }

        [Test]
        public void BuildsShortBoxSpreadStrategy()
        {
            var canonicalOptionSymbol = Symbols.SPY_Option_Chain;
            var underlying = Symbols.SPY;
            var higherStrike = 320m;
            var lowerStrike = 300m;
            var expiration = new DateTime(2023, 08, 18);

            var strategy = OptionStrategies.ShortBoxSpread(canonicalOptionSymbol, higherStrike, lowerStrike, expiration);

            Assert.AreEqual(OptionStrategyDefinitions.ShortBoxSpread.Name, strategy.Name);
            Assert.AreEqual(underlying, strategy.Underlying);
            Assert.AreEqual(canonicalOptionSymbol, strategy.CanonicalOption);

            Assert.AreEqual(4, strategy.OptionLegs.Count);
            Assert.AreEqual(0, strategy.UnderlyingLegs.Count);

            var longPutLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Put && x.Strike == lowerStrike);
            Assert.AreEqual(expiration, longPutLeg.Expiration);
            Assert.AreEqual(1, longPutLeg.Quantity);

            var shortPutLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Put && x.Strike == higherStrike);
            Assert.AreEqual(expiration, shortPutLeg.Expiration);
            Assert.AreEqual(-1, shortPutLeg.Quantity);

            var shortCallLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Call && x.Strike == lowerStrike);
            Assert.AreEqual(expiration, shortCallLeg.Expiration);
            Assert.AreEqual(-1, shortCallLeg.Quantity);

            var longCallLeg = strategy.OptionLegs.Single(x => x.Right == OptionRight.Call && x.Strike == higherStrike);
            Assert.AreEqual(expiration, longCallLeg.Expiration);
            Assert.AreEqual(1, longCallLeg.Quantity);
        }
    }
}
