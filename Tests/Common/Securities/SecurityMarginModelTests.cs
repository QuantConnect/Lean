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
using QuantConnect.Algorithm;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityMarginModelTests
    {
        [Test]
        public void ZeroTargetWithZeroHoldingsIsNotAnError()
        {
            var algorithm = new QCAlgorithm();
            var security = algorithm.AddSecurity(SecurityType.Equity, "SPY");

            var model = new SecurityMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetValue(algorithm.Portfolio, security, 0);

            Assert.AreEqual(0, result.Quantity);
            Assert.AreEqual(string.Empty, result.Reason);
            Assert.AreEqual(false, result.IsError);
        }

        [Test]
        public void ZeroTargetWithNonZeroHoldingsReturnsNegativeOfQuantity()
        {
            var algorithm = new QCAlgorithm();
            var security = algorithm.AddSecurity(SecurityType.Equity, "SPY");
            security.Holdings.SetHoldings(200, 10);

            var model = new SecurityMarginModel();
            var result = model.GetMaximumOrderQuantityForTargetValue(algorithm.Portfolio, security, 0);

            Assert.AreEqual(-10, result.Quantity);
            Assert.AreEqual(string.Empty, result.Reason);
            Assert.AreEqual(false, result.IsError);
        }
    }
}