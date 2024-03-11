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
### In this case, the algorithm tests the Covered and Protective Put strategies.
### </summary>
class CoveredAndProtectivePutStrategiesAlgorithm(OptionStrategyFactoryMethodsBaseAlgorithm):

    def ExpectedOrdersCount(self) -> int:
        return 4

    def TradeStrategy(self, chain: OptionChain, option_symbol: Symbol):
        contracts = sorted(sorted(chain, key = lambda x: abs(chain.Underlying.Price - x.Strike)),
                           key = lambda x: x.Expiry, reverse=True)

        if len(contracts) == 0: return
        contract = contracts[0]
        if contract != None:
            self._covered_put = OptionStrategies.CoveredPut(option_symbol, contract.Strike, contract.Expiry)
            self._protective_put = OptionStrategies.ProtectivePut(option_symbol, contract.Strike, contract.Expiry)
            self.Buy(self._covered_put, 2)


    def AssertStrategyPositionGroup(self, positionGroup: IPositionGroup, option_symbol: Symbol):
        positions = list(positionGroup.Positions)
        if len(positions) != 2:
            raise Exception(f"Expected position group to have 2 positions. Actual: {len(positions)}")

        optionPosition = [position for position in positions if position.Symbol.SecurityType == SecurityType.Option][0]
        if optionPosition.Symbol.ID.OptionRight != OptionRight.Put:
            raise Exception(f"Expected option position to be a put. Actual: {optionPosition.Symbol.ID.OptionRight}")

        underlyingPosition = [position for position in positions if position.Symbol.SecurityType == SecurityType.Equity][0]
        expectedOptionPositionQuantity = -2
        expectedUnderlyingPositionQuantity = -2 * self.Securities[option_symbol].SymbolProperties.ContractMultiplier

        if optionPosition.Quantity != expectedOptionPositionQuantity:
            raise Exception(f"Expected option position quantity to be {expectedOptionPositionQuantity}. Actual: {optionPosition.Quantity}")

        if underlyingPosition.Quantity != expectedUnderlyingPositionQuantity:
            raise Exception(f"Expected underlying position quantity to be {expectedUnderlyingPositionQuantity}. Actual: {underlyingPosition.Quantity}")

    def LiquidateStrategy(self):
        # We should be able to close the position using the inverse strategy (a protective put)
        self.Buy(self._protective_put, 2)
