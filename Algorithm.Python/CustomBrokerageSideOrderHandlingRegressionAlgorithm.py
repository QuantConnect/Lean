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
### Algorithm demonstrating the usage of custom brokerage message handler and the new brokerage-side order handling/filtering.
### This test is supposed to be ran by the CustomBrokerageMessageHandlerTests unit test fixture.
###
### All orders are sent from the brokerage, none of them will be placed by the algorithm.
### </summary>
class CustomBrokerageSideOrderHandlingRegressionAlgorithm(QCAlgorithm):
    '''Algorithm demonstrating the usage of custom brokerage message handler and the new brokerage-side order handling/filtering.
     This test is supposed to be ran by the CustomBrokerageMessageHandlerTests unit test fixture.

     All orders are sent from the brokerage, none of them will be placed by the algorithm.'''

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.SetBrokerageMessageHandler(CustomBrokerageMessageHandler(self))

        self._spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)

    def OnEndOfAlgorithm(self):
        # The security should have been added
        if not self.Securities.ContainsKey(self._spy):
            raise Exception("Expected security to have been added")

        if self.Transactions.OrdersCount == 0:
            raise Exception("Expected orders to be added from brokerage side")

        if len(list(self.Portfolio.Positions.Groups)) != 1:
            raise Exception("Expected only one position")

class CustomBrokerageMessageHandler(IBrokerageMessageHandler):
    __namespace__ = "CustomBrokerageSideOrderHandlingRegressionAlgorithm"

    def __init__(self, algorithm):
        self._algorithm = algorithm

    def HandleMessage(self, message):
        toLog = f"{self._algorithm.Time} Event: {message.Message}"
        self._algorithm.Debug(toLog)
        self._algorithm.Log(toLog)

    def HandleOrder(self, eventArgs):
        order = eventArgs.Order
        if order.Tag is None or not order.Tag.isdigit():
            raise Exception("Expected all new brokerage-side orders to have a valid tag")

        # We will only process orders with even tags
        return int(order.Tag) % 2 == 0
