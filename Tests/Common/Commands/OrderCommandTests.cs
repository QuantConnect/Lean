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
using QuantConnect.Commands;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Commands
{
    [TestFixture]
    public class OrderCommandTests
    {
        [TestCase(true, true, true, 0)]
        [TestCase(true, true, false, 0)]
        [TestCase(true, false, true, 0)]
        [TestCase(true, false, false, 0)]
        [TestCase(false, true, true, 0)]
        [TestCase(false, true, false, 0)]
        [TestCase(false, false, true, 0)]
        [TestCase(false, false, false, 0)]
        [TestCase(true, true, true, 1)]
        [TestCase(true, true, false, 1)]
        [TestCase(true, false, true, 1)]
        [TestCase(true, false, false, 1)]
        [TestCase(false, true, false, 1)]
        [TestCase(false, false, true, 1)]
        [TestCase(false, false, false, 1)]
        [TestCase(false, true, true, 1)]
        public void RunOrderCommand(bool warminUp, bool hasData, bool securityAdded, int quantity)
        {
            var command = new OrderCommand
            {
                Symbol = Symbols.AAPL,
                Quantity = quantity,
                OrderType = OrderType.Market
            };

            var algorithm = new AlgorithmStub();
            if (!warminUp)
            {
                algorithm.SetFinishedWarmingUp();
            }
            if (securityAdded)
            {
                var security = algorithm.AddEquity("AAPL");
                if (hasData)
                {
                    security.SetMarketPrice(new Tick { Value = 10 });
                }
            }

            var response = command.Run(algorithm);
            if (!warminUp && hasData && securityAdded && quantity > 0)
            {
                Assert.IsTrue(response.Success);
            }
            else
            {
                Assert.IsFalse(response.Success);
            }
        }
    }
}
