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
### Basic template algorithm that implements a fill model with partial fills
### <meta name="tag" content="trading and orders" />
### </summary>
class CustomPartialFillModelAlgorithm(QCAlgorithm):
    '''Basic template algorithm that implements a fill model with partial fills'''

    def Initialize(self):
        self.SetStartDate(2019, 1, 1)
        self.SetEndDate(2019, 3, 1)

        equity = self.AddEquity("SPY", Resolution.Hour)
        self.spy = equity.Symbol
        self.holdings = equity.Holdings

        # Set the fill model
        equity.SetFillModel(CustomPartialFillModel(self))


    def OnData(self, data):
        open_orders = self.Transactions.GetOpenOrders(self.spy)
        if len(open_orders) != 0: return

        if self.Time.day > 10 and self.holdings.Quantity <= 0:
            self.MarketOrder(self.spy, 105, True)

        elif self.Time.day > 20 and self.holdings.Quantity >= 0:
            self.MarketOrder(self.spy, -100, True)


class CustomPartialFillModel(FillModel):
    '''Implements a custom fill model that inherit from FillModel. Override the MarketFill method to simulate partially fill orders'''

    def __init__(self, algorithm):
        self.algorithm = algorithm
        self.absoluteRemainingByOrderId = {}

    def MarketFill(self, asset, order):
        absoluteRemaining = self.absoluteRemainingByOrderId.get(order.Id, order. AbsoluteQuantity)

        # Create the object
        fill = super().MarketFill(asset, order)

        # Set the fill amount to the maximum 10-multiple smaller than the order.Quantity for long orders
        # Set the fill amount to the minimum 10-multiple greater than the order.Quantity for short orders
        fill.FillQuantity = np.sign(order.Quantity) * 10 * math.floor(abs(order.Quantity) / 10)

        if (absoluteRemaining < 10) or (absoluteRemaining == abs(fill.FillQuantity)):
            fill.FillQuantity = np.sign(order.Quantity) * absoluteRemaining
            fill.Status = OrderStatus.Filled
            self.absoluteRemainingByOrderId.pop(order.Id, None)
        else:
            fill.Status = OrderStatus.PartiallyFilled
            self.absoluteRemainingByOrderId[order.Id] = absoluteRemaining - abs(fill.FillQuantity)
            price = fill.FillPrice
            # self.algorithm.Debug(f"{self.algorithm.Time} - Partial Fill - Remaining {absoluteRemaining} Price - {price}")

        return fill
