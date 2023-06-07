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
from QuantConnect.Securities.Positions import IPositionGroup

### <summary>
### This base algorithm demonstrates how to use OptionStrategies helper class to batch send orders for common strategies.
### </summary>
class OptionStrategyFactoryMethodsBaseAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(1000000)

        option = self.AddOption("GOOG")
        self._option_symbol = option.Symbol

        option.SetFilter(-2, +2, 0, 180)

        self.SetBenchmark("GOOG")

    def OnData(self, slice):
        if not self.Portfolio.Invested:
            chain = slice.OptionChains.get(self._option_symbol)
            if chain is not None:
                self.TradeStrategy(chain, self._option_symbol)
        else:
            # Verify that the strategy was traded
            positionGroup = list(self.Portfolio.Positions.Groups)[0]

            buyingPowerModel = positionGroup.BuyingPowerModel
            if not isinstance(buyingPowerModel, OptionStrategyPositionGroupBuyingPowerModel):
                raise Exception("Expected position group buying power model type: OptionStrategyPositionGroupBuyingPowerModel. "
                                f"Actual: {type(positionGroup.BuyingPowerModel).__name__}")

            self.AssertStrategyPositionGroup(positionGroup, self._option_symbol)

            # Now we should be able to close the position
            self.LiquidateStrategy()

            # We can quit now, no more testing required
            self.Quit()

    def OnEndOfAlgorithm(self):
        if self.Portfolio.Invested:
            raise Exception("Expected no holdings at end of algorithm")

        orders_count = len(list(self.Transactions.GetOrders(lambda order: order.Status == OrderStatus.Filled)))
        if orders_count != self.ExpectedOrdersCount():
            raise Exception(f"Expected {self.ExpectedOrdersCount()} orders to have been submitted and filled, "
                            f"half for buying the strategy and the other half for the liquidation. Actual {orders_count}")

    def ExpectedOrdersCount(self) -> int:
        raise NotImplementedError("ExpectedOrdersCount method is not implemented")

    def TradeStrategy(self, chain: OptionChain, option_symbol: Symbol) -> None:
        raise NotImplementedError("TradeStrategy method is not implemented")

    def AssertStrategyPositionGroup(self, positionGroup: IPositionGroup, option_symbol: Symbol) -> None:
        raise NotImplementedError("AssertStrategyPositionGroup method is not implemented")

    def LiquidateStrategy(self) -> None:
        raise NotImplementedError("LiquidateStrategy method is not implemented")
