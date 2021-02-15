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


from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from collections import deque
from datetime import timedelta


### <summary>
### Basic algorithm demonstrating how to place LimitIfTouched orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />`
### <meta name="tag" content="limit if touched order"/>
class LimitIfTouchedRegressionAlgorithm(QCAlgorithm):
    _expectedEvents = deque([
        "Time: 10/10/2013 13:31:00 OrderID: 72 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 152.8807 USD LimitPrice: 152.519 TriggerPrice: 151.769 OrderFee: 1 USD",
        "Time: 10/10/2013 15:55:00 OrderID: 73 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 153.9225 USD LimitPrice: 153.8898 TriggerPrice: 153.1398 OrderFee: 1 USD",
        "Time: 10/11/2013 14:02:00 OrderID: 74 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 154.9643 USD LimitPrice: 154.9317 TriggerPrice: 154.1817 OrderFee: 1 USD"
    ])

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)
        self.AddEquity("SPY")

    def OnData(self, data):
        if data.ContainsKey("SPY"):
            if len(self.Transactions.GetOpenOrders()) == 0:
                self._negative = 1 if self.Time.day < 9 else -1
                orderRequest = SubmitOrderRequest(OrderType.LimitIfTouched, SecurityType.Equity, "SPY",
                                                  self._negative * 10, 0,
                                                  data["SPY"].Price - self._negative,
                                                  data["SPY"].Price - 0.25 * self._negative, self.UtcTime,
                                                  f"LIT - Quantity: {self._negative * 10}")
                self._request = self.Transactions.AddOrder(orderRequest)
                return

            if self._request is not None:
                if self._request.Quantity == 1:
                    self.Transactions.CancelOpenOrders()
                    self._request = None
                    return

                new_quantity = int(self._request.Quantity - self._negative)
                self._request.UpdateQuantity(new_quantity, f"LIT - Quantity: {new_quantity}")

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            expected = self._expectedEvents.popleft()
            if orderEvent.ToString() != expected:
                raise Exception(f"orderEvent {orderEvent.Id} differed from {expected}")
