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

import itertools

from OptionStrategyFactoryMethodsBaseAlgorithm import *

### <summary>
### This algorithm demonstrate how to use OptionStrategies helper class to batch send orders for common strategies.
### In this case, the algorithm tests the Iron Condor strategy.
### </summary>
class IronCondorStrategyAlgorithm(OptionStrategyFactoryMethodsBaseAlgorithm):

    def ExpectedOrdersCount(self) -> int:
        return 8

    def TradeStrategy(self, chain: OptionChain, option_symbol: Symbol):
        for expiry, group in itertools.groupby(chain, lambda x: x.Expiry):
            contracts = sorted(group, key=lambda x: x.Strike)
            if len(contracts) < 4:continue

            putContracts = [x for x in contracts if x.Right == OptionRight.Put]
            if len(putContracts) < 2: continue
            longPutStrike = putContracts[0].Strike
            shortPutStrike = putContracts[1].Strike

            callContracts = [x for x in contracts if x.Right == OptionRight.Call and x.Strike > shortPutStrike]
            if len(callContracts) < 2: continue
            shortCallStrike = callContracts[0].Strike
            longCallStrike = callContracts[1].Strike

            self._iron_condor = OptionStrategies.IronCondor(option_symbol, longPutStrike, shortPutStrike, shortCallStrike, longCallStrike, expiry)
            self.Buy(self._iron_condor, 2)
            return

    def AssertStrategyPositionGroup(self, positionGroup: IPositionGroup, option_symbol: Symbol):
        positions = list(positionGroup.Positions)
        if len(positions) != 4:
            raise Exception(f"Expected position group to have 4 positions. Actual: {len(positions)}")

        orderedStrikes = sorted((leg.Strike for leg in self._iron_condor.OptionLegs))

        longPutStrike = orderedStrikes[0]
        longPutPosition = next((x for x in positionGroup.Positions
                                if x.Symbol.ID.OptionRight == OptionRight.Put and x.Symbol.ID.StrikePrice == longPutStrike),
                               None)
        if longPutPosition is None or longPutPosition.Quantity != 2:
            raise Exception(f"Expected long put position quantity to be 2. Actual: {longPutPosition.Quantity}")

        shortPutStrike = orderedStrikes[1]
        shortPutPosition = next((x for x in positionGroup.Positions
                                 if x.Symbol.ID.OptionRight == OptionRight.Put and x.Symbol.ID.StrikePrice == shortPutStrike),
                                None)
        if shortPutPosition is None or shortPutPosition.Quantity != -2:
            raise Exception(f"Expected short put position quantity to be -2. Actual: {shortPutPosition.Quantity}")

        shortCallStrike = orderedStrikes[2]
        shortCallPosition = next((x for x in positionGroup.Positions
                                  if x.Symbol.ID.OptionRight == OptionRight.Call and x.Symbol.ID.StrikePrice == shortCallStrike),
                                 None)
        if shortCallPosition is None or shortCallPosition.Quantity != -2:
            raise Exception(f"Expected short call position quantity to be -2. Actual: {shortCallPosition.Quantity}")

        longCallStrike = orderedStrikes[3]
        longCallPosition = next((x for x in positionGroup.Positions
                                 if x.Symbol.ID.OptionRight == OptionRight.Call and x.Symbol.ID.StrikePrice == longCallStrike),
                                None)
        if longCallPosition is None or longCallPosition.Quantity != 2:
            raise Exception(f"Expected long call position quantity to be 2. Actual: {longCallPosition.Quantity}")

    def LiquidateStrategy(self):
        # We should be able to close the position by selling the strategy
        self.Sell(self._iron_condor, 2)
