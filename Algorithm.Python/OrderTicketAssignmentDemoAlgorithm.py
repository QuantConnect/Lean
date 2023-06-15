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

from datetime import timedelta
from AlgorithmImports import *

### <summary>
### Demonstration on how to access order tickets right after placing an order.
### </summary>
class OrderTicketAssignmentDemoAlgorithm(QCAlgorithm):
    '''Demonstration on how to access order tickets right after placing an order.'''
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.symbol = self.AddEquity("SPY").Symbol

        self.trade_count = 0
        self.Consolidate(self.symbol, timedelta(hours=1), self.HourConsolidator)

    def HourConsolidator(self, bar: TradeBar):
        # Reset self.ticket to None on each new bar
        self.ticket = None
        self.ticket = self.MarketOrder(self.symbol, 1, asynchronous=True)
        self.Debug(f"{self.Time}: Buy: Price {bar.Price}, orderId: {self.ticket.OrderId}")
        self.trade_count += 1

    def OnOrderEvent(self, orderEvent: OrderEvent):
        # We cannot access self.ticket directly because it is assigned asynchronously:
        # this order event could be triggered before self.ticket is assigned.
        ticket = orderEvent.Ticket
        if ticket is None:
            raise Exception("Expected order ticket in order event to not be null")
        if orderEvent.Status == OrderStatus.Submitted and self.ticket is not None:
            raise Exception("Field self.ticket not expected no be assigned on the first order event")

        self.Debug(ticket.ToString())

    def OnEndOfAlgorithm(self):
        # Just checking that orders were placed
        if not self.Portfolio.Invested or self.trade_count != self.Transactions.OrdersCount:
            raise Exception(f"Expected the portfolio to have holdings and to have {self.tradeCount} trades, but had {self.Transactions.OrdersCount}")
