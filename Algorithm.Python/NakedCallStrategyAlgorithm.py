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

from OptionStrategyFactoryMethodsBaseAlgorithm import *

### <summary>
### This algorithm demonstrate how to use OptionStrategies helper class to batch send orders for common strategies.
### In this case, the algorithm tests the Naked Call strategy.
### </summary>
class NakedCallStrategyAlgorithm(OptionStrategyFactoryMethodsBaseAlgorithm):

    def ExpectedOrdersCount(self) -> int:
        return 2

    def TradeStrategy(self, chain: OptionChain, option_symbol: Symbol):
        contracts = sorted(sorted(chain, key = lambda x: abs(chain.Underlying.Price - x.ScaledStrike)),
                           key = lambda x: x.Expiry, reverse=True)

        if len(contracts) == 0: return
        contract = contracts[0]
        if contract != None:
            self._naked_call = OptionStrategies.NakedCall(option_symbol, contract.Strike, contract.Expiry)
            self.Buy(self._naked_call, 2)

    def AssertStrategyPositionGroup(self, positionGroup: IPositionGroup, option_symbol: Symbol):
        positions = list(positionGroup.Positions)
        if len(positions) != 1:
            raise Exception(f"Expected position group to have 1 positions. Actual: {len(positions)}")

        optionPosition = [position for position in positions if position.Symbol.SecurityType == SecurityType.Option][0]
        if optionPosition.Symbol.ID.OptionRight != OptionRight.Call:
            raise Exception(f"Expected option position to be a call. Actual: {optionPosition.Symbol.ID.OptionRight}")

        expectedOptionPositionQuantity = -2

        if optionPosition.Quantity != expectedOptionPositionQuantity:
            raise Exception(f"Expected option position quantity to be {expectedOptionPositionQuantity}. Actual: {optionPosition.Quantity}")

    def LiquidateStrategy(self):
        # We can liquidate by selling the strategy
        self.Sell(self._naked_call, 2)
