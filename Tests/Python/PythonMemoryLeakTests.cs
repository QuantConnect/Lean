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
using System.Threading;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonMemoryLeakTests
    {
        private Security _security;
        private static DateTime orderDateTime;

        [SetUp]
        public void SetUp()
        {
            _security = SecurityTests.GetSecurity();
            orderDateTime = new DateTime(2017, 2, 2, 13, 0, 0);
            var reference = orderDateTime;
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            _security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
        }

        [Test]
        public void DoesNotLeak()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    @"
from AlgorithmImports import *

def jose(security):
    security.SetFeeModel(MakerTakerModel())
class MakerTakerModel(FeeModel):
    def __init__(self, maker = -.0016, taker = .003):
        self.maker = maker 
        self.taker = taker

    def GetOrderFee(self, parameters: OrderFeeParameters) -> OrderFee:
        qty = parameters.Order.Quantity
        ord_type = parameters.Order.Type

        # fee_in_usd = .0008

        # make_ps = -.0016 #Rebate
        # take_ps = .003

        if ord_type in [OrderType.Market, OrderType.StopMarket]:
            fee_usd = self.taker * qty 
        else:
            fee_usd = self.maker * qty

        return OrderFee(CashAmount(fee_usd, 'USD'))"
                );

                module.GetAttr("jose").Invoke(_security.ToPython());

                var parameters = new OrderFeeParameters(
                    _security,
                    new MarketOrder(_security.Symbol, 1, orderDateTime)
                );
                // warmup
                var result = _security.FeeModel.GetOrderFee(parameters);
                Assert.IsNotNull(result);

                // let the system stabilize
                Thread.Sleep(1000);
                var start = GC.GetTotalMemory(true);
                for (var i = 0; i < 50000; i++)
                {
                    result = _security.FeeModel.GetOrderFee(parameters);

                    Assert.IsNotNull(result);

                    if (i % 10000 == 0)
                    {
                        Log.Debug($"Memory: {GC.GetTotalMemory(true)}");
                    }
                }
                Thread.Sleep(1000);
                var end = GC.GetTotalMemory(true);

                var message =
                    $"Start: {start}. End {end}. Variation {((end - start) / (decimal)start * 100).RoundToSignificantDigits(2)}%";
                Log.Debug(message);

                // 5% noise, leak was >10%
                Assert.LessOrEqual(end, start * 1.05, message);
            }
        }
    }
}
