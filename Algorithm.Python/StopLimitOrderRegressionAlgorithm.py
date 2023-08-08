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

### <summary>
### Basic algorithm demonstrating how to place stop limit orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="stop limit order"/>
class StopLimitOrderRegressionAlgorithm(QCAlgorithm):
    '''Basic algorithm demonstrating how to place stop limit orders.'''

    Tolerance = 0.001
    FastPeriod = 30
    SlowPeriod = 60

    def Initialize(self):
        self.SetStartDate(2013, 1, 1)
        self.SetEndDate(2017, 1, 1)
        self.SetCash(100000)

        self._symbol = self.AddEquity("SPY", Resolution.Daily).Symbol

        self._fast = self.EMA(self._symbol, self.FastPeriod, Resolution.Daily)
        self._slow = self.EMA(self._symbol, self.SlowPeriod, Resolution.Daily)

        self._buyOrderTicket: OrderTicket = None
        self._sellOrderTicket: OrderTicket = None
        self._previousSlice: Slice = None

    def OnData(self, slice: Slice):
        if not self.IsReady():
            return

        security = self.Securities[self._symbol]
        if self._buyOrderTicket is None and self.TrendIsUp():
            self._buyOrderTicket = self.StopLimitOrder(self._symbol, 100, stopPrice=security.High * 1.10, limitPrice=security.High * 1.11)
        elif self._buyOrderTicket.Status == OrderStatus.Filled and self._sellOrderTicket is None and self.TrendIsDown():
            self._sellOrderTicket = self.StopLimitOrder(self._symbol, -100, stopPrice=security.Low * 0.99, limitPrice=security.Low * 0.98)

    def OnOrderEvent(self, orderEvent: OrderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            order: StopLimitOrder = self.Transactions.GetOrderById(orderEvent.OrderId)
            if not order.StopTriggered:
                raise Exception("StopLimitOrder StopTriggered should haven been set if the order filled.")

            if orderEvent.Direction == OrderDirection.Buy:
                limitPrice = self._buyOrderTicket.Get(OrderField.LimitPrice)
                if orderEvent.FillPrice > limitPrice:
                    raise Exception(f"Buy stop limit order should have filled with price less than or equal to the limit price {limitPrice}. "
                                    f"Fill price: {orderEvent.FillPrice}")
            else:
                limitPrice = self._sellOrderTicket.Get(OrderField.LimitPrice)
                if orderEvent.FillPrice < limitPrice:
                    raise Exception(f"Sell stop limit order should have filled with price greater than or equal to the limit price {limitPrice}. "
                                    f"Fill price: {orderEvent.FillPrice}")

    def IsReady(self):
        return self._fast.IsReady and self._slow.IsReady

    def TrendIsUp(self):
        return self.IsReady() and self._fast.Current.Value > self._slow.Current.Value * (1 + self.Tolerance)

    def TrendIsDown(self):
        return self.IsReady() and self._fast.Current.Value < self._slow.Current.Value * (1 + self.Tolerance)
