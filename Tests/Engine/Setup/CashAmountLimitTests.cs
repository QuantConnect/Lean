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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.Setup
{
    [TestFixture]
    public class CashAmountLimitTests
    {
        [Test]
        public void RoundTripSerialization()
        {
            var expected = new CashAmountLimit
            {
                Cash = new CashAmount(10, Currencies.EUR),
                Force = true
            };

            var serialize = JsonConvert.SerializeObject(expected);

            var result = JsonConvert.DeserializeObject<CashAmountLimit>(serialize);

            Assert.AreEqual(expected.Force, result.Force);
            Assert.AreEqual(expected.Cash.Currency, result.Cash.Currency);
            Assert.AreEqual(expected.Cash.Amount, result.Cash.Amount);
            Assert.AreEqual(expected.Cash, result.Cash);
        }
    }
}