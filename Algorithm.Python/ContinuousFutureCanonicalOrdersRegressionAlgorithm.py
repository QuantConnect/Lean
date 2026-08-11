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
### Regression algorithm asserting the canonical continuous future symbol cannot be traded directly and fails loudly,
### while its currently mapped contract can: calculate_order_quantity returns zero with an instructive error,
### set_holdings submits no orders and direct orders produce an invalid ticket pointing to future.mapped.
### Also asserts future.canonical and that future.mapped is None until the continuous contract universe makes
### its first selection, after initialize.
### </summary>
class ContinuousFutureCanonicalOrdersRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 10)

        self._continuous_contract = self.add_future(Futures.Indices.SP_500_E_MINI,
                                                    data_normalization_mode=DataNormalizationMode.BACKWARDS_RATIO,
                                                    data_mapping_mode=DataMappingMode.OPEN_INTEREST,
                                                    contract_depth_offset=0)

        if self._continuous_contract.mapped is not None:
            raise AssertionError("Expected future.mapped to be None during initialize: "
                                 "the continuous contract universe does not make its first selection until after initialize")

        if self._continuous_contract.canonical != self._continuous_contract.symbol:
            raise AssertionError("Expected future.canonical to be the continuous contract symbol itself")

        self._canonical_checks_done = False
        self._traded = False

    def on_data(self, slice):
        if self._continuous_contract.mapped is None or not slice.bars.contains_key(self._continuous_contract.symbol):
            return

        if not self._canonical_checks_done:
            self._canonical_checks_done = True
            canonical = self._continuous_contract.symbol

            # Continuous contract data is keyed by the canonical symbol, and the future object itself can be used as the key
            if slice.bars.get(canonical) is None or slice.bars.get(self._continuous_contract) is None:
                raise AssertionError("Expected the continuous contract bar to be accessible through the canonical symbol and the future object")

            # The canonical symbol is not tradable: no order quantity can be computed for it
            if self.calculate_order_quantity(canonical, 1.0) != 0:
                raise AssertionError("Expected calculate_order_quantity to return 0 for the canonical symbol")

            # set_holdings must not submit orders for the canonical symbol
            if len(self.set_holdings(canonical, 0.5)) != 0 or self.portfolio.invested:
                raise AssertionError("Expected set_holdings to not submit orders for the canonical symbol")

            # Direct orders on the canonical symbol are rejected with an instructive message
            ticket = self.market_order(canonical, 1)
            if ticket.status != OrderStatus.INVALID:
                raise AssertionError("Expected a market order on the canonical symbol to be invalid")
            if "canonical" not in ticket.submit_request.response.error_message:
                raise AssertionError("Expected the invalid canonical order error message to explain the symbol is canonical, "
                                     f"but was: '{ticket.submit_request.response.error_message}'")

        if not self._traded:
            self._traded = True

            # The currently mapped contract is the tradable one
            ticket = self.market_order(self._continuous_contract.mapped, 1)
            if ticket.status == OrderStatus.INVALID:
                raise AssertionError("Expected a market order on the mapped contract to be valid")

    def on_end_of_algorithm(self):
        if not self._canonical_checks_done:
            raise AssertionError("No data was received so the canonical symbol checks were not performed")

        if not self.portfolio.invested:
            raise AssertionError("Expected to hold a position in the mapped contract")
