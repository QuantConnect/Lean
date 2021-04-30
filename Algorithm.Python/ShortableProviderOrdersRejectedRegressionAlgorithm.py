### QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
### Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
###
### Licensed under the Apache License, Version 2.0 (the "License");
### you may not use this file except in compliance with the License.
### You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
###
### Unless required by applicable law or agreed to in writing, software
### distributed under the License is distributed on an "AS IS" BASIS,
### WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
### See the License for the specific language governing permissions and
### limitations under the License.

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Brokerages import *
from QuantConnect.Data import *
from QuantConnect.Data.Shortable import *
from QuantConnect.Interfaces import *
from QuantConnect.Orders import *

class RegressionTestShortableBrokerageModel(DefaultBrokerageModel):
    def __init__(self):
        self.ShortableProvider = LocalDiskShortableProvider(SecurityType.Equity, "testbrokerage", Market.USA)

### <summary>
### Tests that orders are denied if they exceed the max shortable quantity.
### </summary>
class ShortableProviderOrdersRejectedRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.ordersAllowed = []
        self.ordersDenied = []
        self.initialize = False
        self.invalidatedAllowedOrder = False
        self.invalidatedNewOrderWithPortfolioHoldings = False

        self.SetStartDate(2013, 10, 4)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(10000000)

        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol
        self.aig = self.AddEquity("AIG", Resolution.Minute).Symbol

        self.SetBrokerageModel(RegressionTestShortableBrokerageModel())

    def OnData(self, data):
        if not self.initialize:
            self.HandleOrder(self.LimitOrder(self.spy, -1001, 10000)) # Should be canceled, exceeds the max shortable quantity
            self.HandleOrder(self.LimitOrder(self.spy, -1000, 10000)) # Allowed, orders at or below 1000 should be accepted
            self.HandleOrder(self.LimitOrder(self.spy, -10, 0.01)) # Should be canceled, the total quantity we would be short would exceed the max shortable quantity.
            self.initialize = True
            return

        if not self.invalidatedAllowedOrder:
            if len(self.ordersAllowed) != 1:
                raise Exception(f"Expected 1 successful order, found: {len(self.ordersAllowed)}")
            if len(self.ordersDenied) != 2:
                raise Exception(f"Expected 2 failed orders, found: {len(self.ordersDenied)}")

            allowedOrder = self.ordersAllowed[0]
            orderUpdate = UpdateOrderFields()
            orderUpdate.LimitPrice = 0.01
            orderUpdate.Quantity = -1001
            orderUpdate.Tag = "Testing updating and exceeding maximum quantity"

            response = allowedOrder.Update(orderUpdate)
            if response.ErrorCode != OrderResponseErrorCode.ExceedsShortableQuantity:
                raise Exception(f"Expected order to fail due to exceeded shortable quantity, found: {response.ErrorCode}")

            cancelResponse = allowedOrder.Cancel()
            if cancelResponse.IsError:
                raise Exception("Expected to be able to cancel open order after bad qty update")

            self.invalidatedAllowedOrder = True
            self.ordersDenied.clear()
            self.ordersAllowed.clear()
            return

        if not self.invalidatedNewOrderWithPortfolioHoldings:
            self.HandleOrder(self.MarketOrder(self.spy, -1000)) # Should succeed, no holdings and no open orders to stop this
            spyShares = self.Portfolio[self.spy].Quantity
            if spyShares != -1000:
                raise Exception(f"Expected -1000 shares in portfolio, found: {spyShares}")

            self.HandleOrder(self.LimitOrder(self.spy, -1, 0.01)) # Should fail, portfolio holdings are at the max shortable quantity.
            if len(self.ordersDenied) != 1:
                raise Exception(f"Expected limit order to fail due to existing holdings, but found {len(self.ordersDenied)} failures")

            self.ordersAllowed.clear()
            self.ordersDenied.clear()

            self.HandleOrder(self.MarketOrder(self.aig, -1001))
            if len(self.ordersAllowed) != 1:
                raise Exception(f"Expected market order of -1001 BAC to not fail")

            self.invalidatedNewOrderWithPortfolioHoldings = True

    def HandleOrder(self, orderTicket):
        if orderTicket.SubmitRequest.Status == OrderRequestStatus.Error:
            self.ordersDenied.append(orderTicket)
            return

        self.ordersAllowed.append(orderTicket)