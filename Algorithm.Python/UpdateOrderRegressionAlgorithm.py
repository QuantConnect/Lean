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

from clr import AddReference
AddReference("System.Core")
AddReference("System.Collections")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from System.Linq import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Orders import *
from QuantConnect.Securities import *
from QuantConnect.Util import *
from math import copysign
from datetime import datetime

### <summary>
### Provides a regression baseline focused on updating orders
### </summary>
### <meta name="tag" content="regression test" />
class UpdateOrderRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,1,1)    #Set Start Date
        self.SetEndDate(2015,1,1)      #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data

        self.security = self.AddEquity("SPY", Resolution.Daily)

        self.last_month = -1
        self.quantity = 100
        self.delta_quantity = 10

        self.stop_percentage = 0.025
        self.stop_percentage_delta = 0.005
        self.limit_percentage = 0.025
        self.limit_percentage_delta = 0.005

        OrderTypeEnum = [OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit, OrderType.MarketOnOpen, OrderType.MarketOnClose]
        self.order_types_queue = CircularQueue[OrderType](OrderTypeEnum)
        self.order_types_queue.CircleCompleted += self.onCircleCompleted
        self.tickets = []


    def onCircleCompleted(self, sender, event):
        '''Flip our signs when we've gone through all the order types'''
        self.quantity *= -1


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if not data.ContainsKey("SPY"):
            return

        if self.Time.month != self.last_month:
            # we'll submit the next type of order from the queue
            orderType = self.order_types_queue.Dequeue()
            #Log("")
            self.Log("\r\n--------------MONTH: {0}:: {1}\r\n".format(self.Time.strftime("%B"), orderType))
            #Log("")
            self.last_month = self.Time.month
            self.Log("ORDER TYPE:: {0}".format(orderType))
            isLong = self.quantity > 0
            stopPrice = (1 + self.stop_percentage)*data["SPY"].High if isLong else (1 - self.stop_percentage)*data["SPY"].Low
            limitPrice = (1 - self.limit_percentage)*stopPrice if isLong else (1 + self.limit_percentage)*stopPrice

            if orderType == OrderType.Limit:
                limitPrice = (1 + self.limit_percentage)*data["SPY"].High if not isLong else (1 - self.limit_percentage)*data["SPY"].Low

            request = SubmitOrderRequest(orderType, self.security.Symbol.SecurityType, "SPY", self.quantity, stopPrice, limitPrice, self.UtcTime, str(orderType))
            ticket = self.Transactions.AddOrder(request)
            self.tickets.append(ticket)

        elif len(self.tickets) > 0:
            ticket = self.tickets[-1]

            if self.Time.day > 8 and self.Time.day < 14:
                if len(ticket.UpdateRequests) == 0 and ticket.Status is not OrderStatus.Filled:
                    self.Log("TICKET:: {0}".format(ticket))
                    updateOrderFields = UpdateOrderFields()
                    updateOrderFields.Quantity = ticket.Quantity + copysign(self.delta_quantity, self.quantity)
                    updateOrderFields.Tag = "Change quantity: {0}".format(self.Time)
                    ticket.Update(updateOrderFields)

            elif self.Time.day > 13 and self.Time.day < 20:
                if len(ticket.UpdateRequests) == 1 and ticket.Status is not OrderStatus.Filled:
                    self.Log("TICKET:: {0}".format(ticket))
                    updateOrderFields = UpdateOrderFields()
                    updateOrderFields.LimitPrice = self.security.Price*(1 - copysign(self.limit_percentage_delta, ticket.Quantity))
                    updateOrderFields.StopPrice = self.security.Price*(1 + copysign(self.stop_percentage_delta, ticket.Quantity))
                    updateOrderFields.Tag = "Change prices: {0}".format(self.Time)
                    ticket.Update(updateOrderFields)
            else:
                if len(ticket.UpdateRequests) == 2 and ticket.Status is not OrderStatus.Filled:
                    self.Log("TICKET:: {0}".format(ticket))
                    ticket.Cancel("{0} and is still open!".format(self.Time))
                    self.Log("CANCELLED:: {0}".format(ticket.CancelRequest))


    def OnOrderEvent(self, orderEvent):
        order = self.Transactions.GetOrderById(orderEvent.OrderId)
        ticket = self.Transactions.GetOrderTicket(orderEvent.OrderId)

        #order cancelations update CanceledTime
        if order.Status == OrderStatus.Canceled and order.CanceledTime != orderEvent.UtcTime:
            raise ValueError("Expected canceled order CanceledTime to equal canceled order event time.")

        #fills update LastFillTime
        if (order.Status == OrderStatus.Filled or order.Status == OrderStatus.PartiallyFilled) and order.LastFillTime != orderEvent.UtcTime:
            raise ValueError("Expected filled order LastFillTime to equal fill order event time.")

        # check the ticket to see if the update was successfully processed
        if len([ur for ur in ticket.UpdateRequests if ur.Response is not None and ur.Response.IsSuccess]) > 0 and order.CreatedTime != self.UtcTime and order.LastUpdateTime is None:
            raise ValueError("Expected updated order LastUpdateTime to equal submitted update order event time")

        if orderEvent.Status == OrderStatus.Filled:
            self.Log("FILLED:: {0} FILL PRICE:: {1}".format(self.Transactions.GetOrderById(orderEvent.OrderId), orderEvent.FillPrice))
        else:
            self.Log(orderEvent.ToString())
            self.Log("TICKET:: {0}".format(ticket))