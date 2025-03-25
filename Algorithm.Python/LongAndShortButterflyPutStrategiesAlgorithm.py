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
### In this case, the algorithm tests the Butterfly Put and Short Butterfly Put strategies.
### </summary>
class LongAndShortButterflyPutStrategiesAlgorithm(OptionStrategyFactoryMethodsBaseAlgorithm):

    def expected_orders_count(self) -> int:
        return 6

    def trade_strategy(self, chain: OptionChain, option_symbol: Symbol):
        put_contracts = (contract for contract in chain if contract.right == OptionRight.PUT)

        for expiry, group in itertools.groupby(put_contracts, lambda x: x.expiry):
            contracts = list(group)
            if len(contracts) < 3:
                continue

            strikes = sorted([contract.strike for contract in contracts])
            atm_strike = min(strikes, key=lambda strike: abs(strike - chain.underlying.price))
            spread = min(atm_strike - strikes[0], strikes[-1] - atm_strike)
            itm_strike = atm_strike + spread
            otm_strike = atm_strike - spread

            if otm_strike in strikes and itm_strike in strikes:
                # Ready to trade
                self._butterfly_put = OptionStrategies.butterfly_put(option_symbol, itm_strike, atm_strike, otm_strike, expiry)
                self._short_butterfly_put = OptionStrategies.short_butterfly_put(option_symbol, itm_strike, atm_strike, otm_strike, expiry)
                self.buy(self._butterfly_put, 2)
                return

    def assert_strategy_position_group(self, position_group: IPositionGroup, option_symbol: Symbol):
        positions = list(position_group.positions)
        if len(positions) != 3:
            raise AssertionError(f"Expected position group to have 3 positions. Actual: {len(positions)}")

        higher_strike = max(leg.strike for leg in self._butterfly_put.option_legs)
        higher_strike_position = next((position for position in positions
                                      if position.symbol.id.option_right == OptionRight.PUT and position.symbol.id.strike_price == higher_strike),
                                     None)

        if higher_strike_position.quantity != 2:
            raise AssertionError(f"Expected higher strike position quantity to be 2. Actual: {higher_strike_position.quantity}")

        lower_strike = min(leg.strike for leg in self._butterfly_put.option_legs)
        lower_strike_position = next((position for position in positions
                                    if position.symbol.id.option_right == OptionRight.PUT and position.symbol.id.strike_price == lower_strike),
                                   None)

        if lower_strike_position.quantity != 2:
            raise AssertionError(f"Expected lower strike position quantity to be 2. Actual: {lower_strike_position.quantity}")

        middle_strike = [leg.strike for leg in self._butterfly_put.option_legs if leg.strike < higher_strike and leg.strike > lower_strike][0]
        middle_strike_position = next((position for position in positions
                                     if position.symbol.id.option_right == OptionRight.PUT and position.symbol.id.strike_price == middle_strike),
                                    None)

        if middle_strike_position.quantity != -4:
            raise AssertionError(f"Expected middle strike position quantity to be -4. Actual: {middle_strike_position.quantity}")

    def liquidate_strategy(self):
        # We should be able to close the position using the inverse strategy (a short butterfly put)
        self.buy(self._short_butterfly_put, 2);
