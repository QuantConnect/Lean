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

from math import copysign
from datetime import datetime

import clr
clr.AddReference("System.Core")
clr.AddReference("System.Collections")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Orders import *
from QuantConnect.Securities import *
from QuantConnect.Util import *
clr.ImportExtensions(Extensions)
clr.ImportExtensions(OrderExtensions)
clr.ImportExtensions(Linq)

class UpdateOrderRegressionAlgorithm(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    def __init__(self):
        self.__LastMonth = -1
        self.__Quantity = 100
        self.__DeltaQuantity = 10

        self.__StopPercentage = 0.025
        self.__StopPercentageDelta = 0.005
        self.__LimitPercentage = 0.025
        self.__LimitPercentageDelta = 0.005

        self.__SecType = SecurityType.Equity
        self.__Symbol = Symbol.Create("SPY", self.__SecType, "USA")  
        self.__Security = None
        
        OrderTypeEnum = [OrderType.Market, OrderType.Limit, OrderType.StopMarket, OrderType.StopLimit, OrderType.MarketOnOpen, OrderType.MarketOnClose]

        self.__orderTypesQueue = CircularQueue[OrderType](OrderTypeEnum)
        self.__tickets = []


    def onCircleCompleted(self, sender, event):
        '''Flip our signs when we've gone through all the order types'''
        self.__Quantity *= -1


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,01,01)  #Set Start Date
        self.SetEndDate(2015,01,01)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(self.__SecType, self.__Symbol, Resolution.Daily)
        self.__Security = self.Securities[self.__Symbol];
        self.__orderTypesQueue.CircleCompleted += self.onCircleCompleted
        

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not data.Bars.ContainsKey(self.__Symbol):
            return

        pyTime = datetime(self.Time)

        if pyTime.month != self.__LastMonth:
            # we'll submit the next type of order from the queue
            orderType = self.__orderTypesQueue.Dequeue();
            #Log("");
            self.Log("\r\n--------------MONTH: {0}:: {1}\r\n".format(pyTime.strftime("%B"), orderType))
            #Log("")
            self.__LastMonth = pyTime.month
            self.Log("ORDER TYPE:: {0}".format(orderType))
            isLong = self.__Quantity > 0
            stopPrice = (1 + self.__StopPercentage)*data.Bars[self.__Symbol].High if isLong else (1 - self.__StopPercentage)*data.Bars[self.__Symbol].Low
            limitPrice = (1 - self.__LimitPercentage)*stopPrice if isLong else (1 + self.__LimitPercentage)*stopPrice
            
            if orderType == OrderType.Limit:
                limitPrice = (1 + self.__LimitPercentage)*data.Bars[self.__Symbol].High if not isLong else (1 - self.__LimitPercentage)*data.Bars[self.__Symbol].Low

            request = SubmitOrderRequest(orderType, self.__SecType, self.__Symbol, self.__Quantity, stopPrice, limitPrice, self.Time, orderType.ToString())
            ticket = self.Transactions.AddOrder(request)
            self.__tickets.append(ticket)

        elif len(self.__tickets) > 0:
            ticket = self.__tickets[-1]
                    
            if pyTime.day > 8 and pyTime.day < 14:
                if len(ticket.UpdateRequests) == 0 and ticket.Status.IsOpen():
                    self.Log("TICKET:: {0}".format(ticket))
                    updateOrderFields = UpdateOrderFields()
                    updateOrderFields.Quantity = ticket.Quantity + copysign(self.__DeltaQuantity, self.__Quantity)
                    updateOrderFields.Tag = "Change quantity: {0}".format(pyTime)
                    ticket.Update(updateOrderFields)                   
                    self.Log("UPDATE1:: {0}".format(ticket.UpdateRequests.Last()))
                    
            elif pyTime.day > 13 and pyTime.day < 20:
                if len(ticket.UpdateRequests) == 1 and ticket.Status.IsOpen():
                    self.Log("TICKET:: {0}".format(ticket))
                    updateOrderFields = UpdateOrderFields()
                    updateOrderFields.LimitPrice = self.__Security.Price*(1 - copysign(self.__LimitPercentageDelta, ticket.Quantity))
                    updateOrderFields.StopPrice = self.__Security.Price*(1 + copysign(self.__StopPercentageDelta, ticket.Quantity))
                    updateOrderFields.Tag = "Change prices: {0}".format(pyTime)
                    ticket.Update(updateOrderFields)
                    self.Log("UPDATE2:: {0}".format(ticket.UpdateRequests.Last()))
            else:
                if len(ticket.UpdateRequests) == 2 and ticket.Status.IsOpen():
                    self.Log("TICKET:: {0}".format(ticket))
                    ticket.Cancel("{0} and is still open!".format(pyTime))
                    self.Log("CANCELLED:: {0}".format(ticket.CancelRequest))


    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Log("FILLED:: {0} FILL PRICE:: {1}".format(self.Transactions.GetOrderById(orderEvent.OrderId), orderEvent.FillPrice.SmartRounding()))
        else:
            self.Log(orderEvent.ToString())
            self.Log("TICKET:: {0}".format(self.__tickets[-1]))