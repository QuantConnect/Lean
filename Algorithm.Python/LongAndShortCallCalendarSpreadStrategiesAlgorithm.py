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
### In this case, the algorithm tests the Call Calendar Spread and Short Call Calendar Spread strategies.
### </summary>
class LongAndShortCallCalendarSpreadStrategiesAlgorithm(OptionStrategyFactoryMethodsBaseAlgorithm):

    def ExpectedOrdersCount(self) -> int:
        return 4

    def TradeStrategy(self, chain: OptionChain, option_symbol: Symbol):
        callContracts = sorted((contract for contract in chain if contract.Right == OptionRight.Call),
                           key=lambda x: abs(x.Strike - chain.Underlying.Value))
        for strike, group in itertools.groupby(callContracts, lambda x: x.Strike):
            contracts = sorted(group, key=lambda x: x.Expiry)
            if len(contracts) < 2: continue

            self._near_expiration = contracts[0].Expiry
            self._far_expiration = contracts[1].Expiry

            self._call_calendar_spread = OptionStrategies.CallCalendarSpread(option_symbol, strike, self._near_expiration, self._far_expiration)
            self._short_call_calendar_spread = OptionStrategies.ShortCallCalendarSpread(option_symbol, strike, self._near_expiration, self._far_expiration)
            self.Buy(self._call_calendar_spread, 2)
            return

    def AssertStrategyPositionGroup(self, positionGroup: IPositionGroup, option_symbol: Symbol):
        positions = list(positionGroup.Positions)
        if len(positions) != 2:
            raise Exception(f"Expected position group to have 2 positions. Actual: {len(positions)}")

        nearExpirationPosition = next((position for position in positions
                                       if position.Symbol.ID.OptionRight == OptionRight.Call and position.Symbol.ID.Date == self._near_expiration),
                                      None)
        if nearExpirationPosition is None or nearExpirationPosition.Quantity != -2:
            raise Exception(f"Expected near expiration position to be -2. Actual: {nearExpirationPosition.Quantity}")

        farExpirationPosition = next((position for position in positions
                                      if position.Symbol.ID.OptionRight == OptionRight.Call and position.Symbol.ID.Date == self._far_expiration),
                                     None)
        if farExpirationPosition is None or farExpirationPosition.Quantity != 2:
            raise Exception(f"Expected far expiration position to be 2. Actual: {farExpirationPosition.Quantity}")

    def LiquidateStrategy(self):
        # We should be able to close the position using the inverse strategy (a short call calendar spread)
        self.Buy(self._short_call_calendar_spread, 2)
