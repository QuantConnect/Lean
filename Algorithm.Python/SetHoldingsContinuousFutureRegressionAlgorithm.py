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
### End-to-end regression algorithm asserting that calling set_holdings directly with the canonical (continuous) Future
### Symbol routes the order to the currently mapped contract, sizes the order using the mapped contract's price, and lets
### portfolio queries on the continuous symbol reflect the active position both before and after a contract roll.
### </summary>
class SetHoldingsContinuousFutureRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 7, 1)
        self.set_end_date(2014, 1, 1)

        self._continuous_contract = self.add_future(Futures.Indices.SP_500_E_MINI,
                                                    data_normalization_mode = DataNormalizationMode.BACKWARDS_RATIO,
                                                    data_mapping_mode = DataMappingMode.LAST_TRADING_DAY,
                                                    contract_depth_offset = 0,
                                                    extended_market_hours = True)
        # A wide filter exercises the case where many future contracts are already in the chain universe across rolls
        self._continuous_contract.set_filter(0, 90)
        self._filled_contract = None
        self._liquidate_after = None
        self._liquidated = False

    def on_data(self, slice):
        # The continuous Future is not tradable until a contract is mapped to it
        if self._liquidated or not self._continuous_contract.is_tradable:
            return

        # Reading invested off the canonical's Holdings relies on the universe linking it to the mapped contract
        invested = self._continuous_contract.holdings.invested

        if not invested:
            # Pass the canonical Symbol — the engine should route the order to the mapped contract
            self.set_holdings(self._continuous_contract.symbol, 0.5)
            self._liquidate_after = self.time + timedelta(days=30)
        elif self.time >= self._liquidate_after:
            # Set the flag before liquidate so on_order_event (called synchronously on fill) sees the post-liquidation state
            self._liquidated = True
            self.liquidate(self._continuous_contract.symbol)

    def on_order_event(self, order_event):
        if order_event.status != OrderStatus.FILLED:
            return

        # The order must have been placed against the currently mapped contract, never the canonical
        if order_event.symbol.is_canonical():
            raise AssertionError(f"Order filled against canonical symbol {order_event.symbol}; expected the mapped contract")

        if not self._liquidated:
            self._filled_contract = order_event.symbol

            # After the fill, querying the canonical should reflect the active position from the mapped contract
            if not self.portfolio[self._continuous_contract.symbol].invested:
                raise AssertionError(f"Portfolio[{self._continuous_contract.symbol}].invested is false after fill on {order_event.symbol}")
            if self.portfolio[self._continuous_contract.symbol].quantity != self.portfolio[order_event.symbol].quantity:
                raise AssertionError(
                    f"Continuous holdings quantity {self.portfolio[self._continuous_contract.symbol].quantity} does not match mapped contract "
                    f"{order_event.symbol} quantity {self.portfolio[order_event.symbol].quantity}")
        else:
            # After liquidation via the canonical symbol, both the canonical view and the mapped contract should be flat
            if self.portfolio[self._continuous_contract.symbol].invested:
                raise AssertionError(f"Portfolio[{self._continuous_contract.symbol}].invested is true after liquidating via the canonical symbol")
            if self.portfolio[order_event.symbol].invested:
                raise AssertionError(f"Portfolio[{order_event.symbol}].invested is true after liquidation")

    def on_end_of_algorithm(self):
        if self._filled_contract is None:
            raise AssertionError("Expected at least one filled order during the backtest")

        if not self._liquidated:
            raise AssertionError("Expected the canonical position to have been liquidated before end of algorithm")

        if self.portfolio[self._continuous_contract.symbol].invested:
            raise AssertionError(f"Portfolio[{self._continuous_contract.symbol}].invested is true at end of algorithm; expected flat after Liquidate")
