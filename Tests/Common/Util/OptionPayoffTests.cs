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

using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class OptionPayoffTests
    {
        // A call is in the money below the underlying price, a put above it, both are at the money at the price
        [TestCase(OptionRight.Call, 100, 90, true, false, false)]
        [TestCase(OptionRight.Call, 100, 100, false, true, false)]
        [TestCase(OptionRight.Call, 100, 110, false, false, true)]
        [TestCase(OptionRight.Put, 100, 90, false, false, true)]
        [TestCase(OptionRight.Put, 100, 100, false, true, false)]
        [TestCase(OptionRight.Put, 100, 110, true, false, false)]
        public void ClassifiesMoneyness(OptionRight right, double underlyingPrice, double strike, bool inTheMoney, bool atTheMoney, bool outOfTheMoney)
        {
            Assert.AreEqual(inTheMoney, OptionPayoff.IsInTheMoney((decimal)underlyingPrice, (decimal)strike, right));
            Assert.AreEqual(atTheMoney, OptionPayoff.IsAtTheMoney((decimal)underlyingPrice, (decimal)strike, right));
            Assert.AreEqual(outOfTheMoney, OptionPayoff.IsOutOfTheMoney((decimal)underlyingPrice, (decimal)strike, right));

            Assert.AreEqual(inTheMoney, OptionPayoff.IsInTheMoney(underlyingPrice, strike, right));
            Assert.AreEqual(atTheMoney, OptionPayoff.IsAtTheMoney(underlyingPrice, strike, right));
            Assert.AreEqual(outOfTheMoney, OptionPayoff.IsOutOfTheMoney(underlyingPrice, strike, right));

            // in the money contracts are the ones with intrinsic value
            Assert.AreEqual(inTheMoney, OptionPayoff.GetIntrinsicValue((decimal)underlyingPrice, (decimal)strike, right) > 0);
        }
    }
}
