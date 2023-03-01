# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *
from collections import deque

### <summary>
### Basic algorithm demonstrating how to place Stop Limit orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />`
### <meta name="tag" content="stop limit order"/>
class StopLimitRegressionAlgorithm(QCAlgorithm):
    _expectedEvents = deque([
            "Time: 10/08/2013 19:37:00 OrderID: 69 EventID: 16 Symbol: SPY Status: Filled Quantity: 3 FillQuantity: 3 FillPrice: 143.5491 USD LimitPrice: 143.5491 StopPrice: 143.9 OrderFee: 1 USD",
            "Time: 10/09/2013 14:33:00 OrderID: 73 EventID: 63 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 143.3427 USD LimitPrice: 143.3427 StopPrice: 142.99 OrderFee: 1 USD",
            "Time: 10/09/2013 17:27:00 OrderID: 74 EventID: 184 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 143.4464 USD LimitPrice: 143.4464 StopPrice: 143.1 OrderFee: 1 USD",
            "Time: 10/10/2013 13:31:00 OrderID: 75 EventID: 164 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 143.5243 USD LimitPrice: 143.5243 StopPrice: 143.17 OrderFee: 1 USD"
    ])

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)
        self.AddEquity("SPY")

    def OnData(self, data):
        if data.ContainsKey("SPY"):
            if len(self.Transactions.GetOpenOrders()) == 0:
                self.negative = 1 if self.Time.day < 9 else -1
                orderRequest = SubmitOrderRequest(OrderType.StopLimit, SecurityType.Equity, "SPY",
                                                  self.negative * 10,
                                                  data["SPY"].Price + 0.25 * self.negative,
                                                  data["SPY"].Price - 0.10 * self.negative, 0, self.UtcTime,
                                                  f"StopLimit - Quantity: {self.negative * 10}")
                self._request = self.Transactions.AddOrder(orderRequest)
                return

            if self._request is not None:
                if self._request.Quantity == 1:
                    self.Transactions.CancelOpenOrders()
                    self._request = None
                    return

                new_quantity = int(self._request.Quantity - self.negative)
                self._request.UpdateQuantity(new_quantity, f"StopLimit - Quantity: {new_quantity}")     
                self._request.UpdateTriggerPrice(Extensions.RoundToSignificantDigits(self._request.Get(OrderField.TriggerPrice), 5));

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            expected = self._expectedEvents.popleft()
            if orderEvent.ToString() != expected:
                raise Exception(f"orderEvent {orderEvent.Id} differed from {expected}")
