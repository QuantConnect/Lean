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
### Basic template algorithm that implements a fill model with combo orders
### <meta name="tag" content="trading and orders" />
### </summary>
class ComboOrdersFillModelAlgorithm(QCAlgorithm):
    '''Basic template algorithm that implements a fill model with combo orders'''

    def Initialize(self):
        self.SetStartDate(2019, 1, 1)
        self.SetEndDate(2019, 1, 20)

        self.spy = self.AddEquity("SPY", Resolution.Hour)
        self.ibm = self.AddEquity("IBM", Resolution.Hour)

        # Set the fill model
        self.spy.SetFillModel(CustomPartialFillModel())
        self.ibm.SetFillModel(CustomPartialFillModel())

        self.orderTypes = {}

    def OnData(self, data):
        if not self.Portfolio.Invested:
            legs = [Leg.Create(self.spy.Symbol, 1), Leg.Create(self.ibm.Symbol, -1)]
            self.ComboMarketOrder(legs, 100)
            self.ComboLimitOrder(legs, 100, self.spy.BidPrice * 0.95)

            legs = [Leg.Create(self.spy.Symbol, 1, self.spy.BidPrice + 1), Leg.Create(self.ibm.Symbol, -1, self.ibm.BidPrice + 1)]
            self.ComboLegLimitOrder(legs, 100)

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            orderType = self.Transactions.GetOrderById(orderEvent.OrderId).Type
            if orderType == OrderType.ComboMarket and orderEvent.AbsoluteFillQuantity != 50:
                raise Exception(f"The absolute quantity filled for all combo market orders should be 50, but for order {orderEvent.OrderId} was {orderEvent.AbsoluteFillQuantity}")
            elif orderType == OrderType.ComboLimit and orderEvent.AbsoluteFillQuantity != 20:
                raise Exception(f"The absolute quantity filled for all combo limit orders should be 20, but for order {orderEvent.OrderId} was {orderEvent.AbsoluteFillQuantity}")
            elif orderType == OrderType.ComboLegLimit and orderEvent.AbsoluteFillQuantity != 10:
                raise Exception(f"The absolute quantity filled for all combo leg limit orders should be 10, but for order {orderEvent.OrderId} was {orderEvent.AbsoluteFillQuantity}")

            self.orderTypes[orderType] = 1

    def OnEndOfAlgorithm(self):
        if len(self.orderTypes) != 3:
            raise Exception(f"Just 3 different types of order were submitted in this algorithm, but the amount of order types was {len(self.orderTypes)}")

        if OrderType.ComboMarket not in self.orderTypes.keys():
            raise Exception(f"One Combo Market Order should have been submitted but it was not")

        if OrderType.ComboLimit not in self.orderTypes.keys():
            raise Exception(f"One Combo Limit Order should have been submitted but it was not")

        if OrderType.ComboLegLimit not in self.orderTypes.keys():
            raise Exception(f"One Combo Leg Limit Order should have been submitted but it was not")


class CustomPartialFillModel(FillModel):
    '''Implements a custom fill model that inherit from FillModel. Overrides ComboMarketFill, ComboLimitOrder and ComboLegLimitOrder
       methods to test FillModelPythonWrapper works as expected'''

    def __init__(self):
        self.absoluteRemainingByOrderId = {}

    def FillOrdersPartially(self, parameters, fills, quantity):
        partialFills = []
        if len(fills) == 0:
            return partialFills

        for kvp, fill in zip(sorted(parameters.SecuritiesForOrders, key=lambda x: x.Key.Id), fills):
            order = kvp.Key;

            absoluteRemaining = self.absoluteRemainingByOrderId.get(order.Id, order.AbsoluteQuantity)

            # Set the fill amount
            fill.FillQuantity = np.sign(order.Quantity) * quantity

            if (min(abs(fill.FillQuantity), absoluteRemaining) == absoluteRemaining):
                fill.FillQuantity = np.sign(order.Quantity) * absoluteRemaining
                fill.Status = OrderStatus.Filled
                self.absoluteRemainingByOrderId.pop(order.Id, None)
            else:
                fill.Status = OrderStatus.PartiallyFilled
                self.absoluteRemainingByOrderId[order.Id] = absoluteRemaining - abs(fill.FillQuantity)
                price = fill.FillPrice
                # self.algorithm.Debug(f"{self.algorithm.Time} - Partial Fill - Remaining {self.absoluteRemainingByOrderId[order.Id]} Price - {price}")

            partialFills.append(fill)

        return partialFills

    def ComboMarketFill(self, order, parameters):
        fills = super().ComboMarketFill(order, parameters)
        partialFills = self.FillOrdersPartially(parameters, fills, 50)
        return partialFills

    def ComboLimitFill(self, order, parameters):
        fills = super().ComboLimitFill(order, parameters);
        partialFills = self.FillOrdersPartially(parameters, fills, 20)
        return partialFills

    def ComboLegLimitFill(self, order, parameters):
        fills = super().ComboLegLimitFill(order, parameters);
        partialFills = self.FillOrdersPartially(parameters, fills, 10)
        return partialFills
