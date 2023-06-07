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

### <summary>
### This algorithm demonstrate how to use OptionStrategies helper class to batch send orders for common strategies.
### In this case, the algorithm tests the Butterfly Put and Short Butterfly Put strategies.
### </summary>
class LongAndShortButterflyPutStrategiesAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(1000000)

        option = self.AddOption("GOOG")
        self._option_symbol = option.Symbol

        option.SetFilter(-2, +2, 0, 180)

        self.SetBenchmark("GOOG")

    def OnData(self,slice):
        if not self.Portfolio.Invested:
            chain = slice.OptionChains.get(self._option_symbol)
            if chain is not None:
                putContracts = (contract for contract in chain if contract.Right == OptionRight.Put)

                for expiry, group in itertools.groupby(putContracts, lambda x: x.Expiry):
                    contracts = list(group)
                    if len(contracts) < 3:
                        continue

                    strikes = sorted([contract.Strike for contract in contracts])
                    atmStrike = min(strikes, key=lambda strike: abs(strike - chain.Underlying.Price))
                    spread = min(atmStrike - strikes[0], strikes[-1] - atmStrike)
                    itmStrike = atmStrike + spread
                    otmStrike = atmStrike - spread

                    if otmStrike in strikes and itmStrike in strikes:
                        # Ready to trade
                        self._butterfly_put = OptionStrategies.ButterflyPut(self._option_symbol, itmStrike, atmStrike, otmStrike, expiry)
                        self._short_butterfly_put = OptionStrategies.ShortButterflyPut(self._option_symbol, itmStrike, atmStrike, otmStrike, expiry)
                        self.Buy(self._butterfly_put, 2)
        else:
            # Verify that the strategy was traded
            positionGroup = list(self.Portfolio.Positions.Groups)[0]

            buyingPowerModel = positionGroup.BuyingPowerModel
            if not isinstance(buyingPowerModel, OptionStrategyPositionGroupBuyingPowerModel):
                raise Exception("Expected position group buying power model type: OptionStrategyPositionGroupBuyingPowerModel. "
                                f"Actual: {type(positionGroup.BuyingPowerModel).__name__}")

            positions = list(positionGroup.Positions)
            if len(positions) != 3:
                raise Exception(f"Expected position group to have 3 positions. Actual: {len(positions)}")

            higherStrike = max(leg.Strike for leg in self._butterfly_put.OptionLegs)
            higherStrikePosition = next((position for position in positions
                                          if position.Symbol.ID.OptionRight == OptionRight.Put and position.Symbol.ID.StrikePrice == higherStrike),
                                         None)

            if higherStrikePosition.Quantity != 2:
                raise Exception(f"Expected higher strike position quantity to be 2. Actual: {higherStrikePosition.Quantity}")

            lowerStrike = min(leg.Strike for leg in self._butterfly_put.OptionLegs)
            lowerStrikePosition = next((position for position in positions
                                        if position.Symbol.ID.OptionRight == OptionRight.Put and position.Symbol.ID.StrikePrice == lowerStrike),
                                       None)

            if lowerStrikePosition.Quantity != 2:
                raise Exception(f"Expected lower strike position quantity to be 2. Actual: {lowerStrikePosition.Quantity}")

            middleStrike = [leg.Strike for leg in self._butterfly_put.OptionLegs if leg.Strike < higherStrike and leg.Strike > lowerStrike][0]
            middleStrikePosition = next((position for position in positions
                                         if position.Symbol.ID.OptionRight == OptionRight.Put and position.Symbol.ID.StrikePrice == middleStrike),
                                        None)

            if middleStrikePosition.Quantity != -4:
                raise Exception(f"Expected middle strike position quantity to be -4. Actual: {middleStrikePosition.Quantity}")

            # Now we should be able to close the position using the inverse strategy (a short butterfly put)
            self.Buy(self._short_butterfly_put, 2);

            # We can quit now, no more testing required
            self.Quit();

    def OnEndOfAlgorithm(self):
        if self.Portfolio.Invested:
            raise Exception("Expected no holdings at end of algorithm")

        orders_count = len(list(self.Transactions.GetOrders(lambda order: order.Status == OrderStatus.Filled)))
        if orders_count != 6:
            raise Exception("Expected 6 orders to have been submitted and filled, 3 for buying the butterfly put and 3 for the liquidation. "
                            f"Actual {orders_count}")

    def OnOrderEvent(self, orderEvent):
        self.Debug(str(orderEvent))
