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
### Regression algorithm to test we can specify a custom brokerage model, and override some of its methods
### </summary>
class CustomBrokerageModelRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetBrokerageModel(CustomBrokerageModel())
        self.AddEquity("SPY", Resolution.Daily)
        self.AddEquity("AIG", Resolution.Daily)
        self.updateRequestSubmitted = False

        if self.BrokerageModel.DefaultMarkets[SecurityType.Equity] != Market.USA:
            raise Exception(f"The default market for Equity should be {Market.USA}")
        if self.BrokerageModel.DefaultMarkets[SecurityType.Crypto] != Market.Binance:
            raise Exception(f"The default market for Crypto should be {Market.Binance}")

    def OnData(self, slice):
        if not self.Portfolio.Invested:
            self.MarketOrder("SPY", 100.0);
            self.aigTicket = self.MarketOrder("AIG", 100.0);

    def OnOrderEvent(self, orderEvent):
        spyTicket = self.Transactions.GetOrderTicket(orderEvent.OrderId)
        if self.updateRequestSubmitted == False:
            updateOrderFields = UpdateOrderFields()
            updateOrderFields.Quantity = spyTicket.Quantity + 10
            spyTicket.Update(updateOrderFields)
            self.spyTicket = spyTicket
            self.updateRequestSubmitted = True

    def OnEndOfAlgorithm(self):
        submitExpectedMessage = "BrokerageModel declared unable to submit order: [2] Information - Code:  - Symbol AIG can not be submitted"
        if self.aigTicket.SubmitRequest.Response.ErrorMessage != submitExpectedMessage:
            raise Exception(f"Order with ID: {self.aigTicket.OrderId} should not have submitted symbol AIG")
        updateExpectedMessage = "OrderID: 1 Information - Code:  - This order can not be updated"
        if self.spyTicket.UpdateRequests[0].Response.ErrorMessage != updateExpectedMessage:
            raise Exception(f"Order with ID: {self.spyTicket.OrderId} should have been updated")

class CustomBrokerageModel(DefaultBrokerageModel):
    DefaultMarkets = { SecurityType.Equity: Market.USA, SecurityType.Crypto : Market.Binance  }

    def CanSubmitOrder(self, security: SecurityType, order: Order, message: BrokerageMessageEvent):
        if security.Symbol.Value == "AIG":
            message = BrokerageMessageEvent(BrokerageMessageType.Information, "", "Symbol AIG can not be submitted")
            return False, message
        return True, None

    def CanUpdateOrder(self, security: SecurityType, order: Order, request: UpdateOrderRequest, message: BrokerageMessageEvent):
        message = BrokerageMessageEvent(BrokerageMessageType.Information, "", "This order can not be updated")
        return False, message
