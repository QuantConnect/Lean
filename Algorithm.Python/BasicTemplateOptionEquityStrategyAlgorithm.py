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
### This example demonstrates how to execute a Call Butterfly option equity strategy
### It adds options for a given underlying equity security, and shows how you can prefilter contracts easily based on strikes and expirations
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
### <meta name="tag" content="trading and orders" />
class BasicTemplateOptionEquityStrategyAlgorithm(QCAlgorithm):
    underlying_ticker = "GOOG"

    def initialize(self) -> None:
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)

        equity = self.add_equity(self.underlying_ticker)
        option = self.add_option(self.underlying_ticker)
        self._option_symbol = option.symbol

        # set our strike/expiry filter for this option chain
        option.set_filter(lambda u: (u.strikes(-2, +2)
                                     # Expiration method accepts TimeSpan objects or integer for days.
                                     # The following statements yield the same filtering criteria
                                     .expiration(0, 180)))

    def on_data(self, slice: Slice) -> None:
        if self.portfolio.invested or not self.is_market_open(self._option_symbol):
            return

        chain = slice.option_chains.get(self._option_symbol)
        if not chain:
            return

        grouped_by_expiry = dict()
        for contract in [contract for contract in chain if contract.right == OptionRight.CALL]:
            grouped_by_expiry.setdefault(int(contract.expiry.timestamp()), []).append(contract)

        first_expiry = list(sorted(grouped_by_expiry))[0]
        call_contracts = sorted(grouped_by_expiry[first_expiry], key = lambda x: x.strike)
        
        expiry = call_contracts[0].expiry
        lower_strike = call_contracts[0].strike
        middle_strike = call_contracts[1].strike
        higher_strike = call_contracts[2].strike

        option_strategy = OptionStrategies.call_butterfly(self._option_symbol, higher_strike, middle_strike, lower_strike, expiry)
                    
        self.order(option_strategy, 10)

    def on_order_event(self, order_event: OrderEvent) -> None:
        self.log(str(order_event))
