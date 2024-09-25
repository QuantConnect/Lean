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
### Regression algorithm illustrating the usage of the <see cref="QCAlgorithm.OptionChain(Symbol)"/> method
### to get an option chain, which contains additional data besides the symbols, including prices, implied volatility and greeks.
### It also shows how this data can be used to filter the contracts based on certain criteria.
### </summary>
class OptionChainFullDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        goog = self.add_equity("GOOG").symbol

        option_chain = self.option_chain(goog)

        # Demonstration using data frame:
        # Get contracts expiring within 10 days, with an implied volatility greater than 0.5 and a delta less than 0.5
        contracts = [
            symbol
            # Index is a tuple and the first element is the symbol
            for (symbol,), contract_data in option_chain.data_frame.iterrows()
            if symbol.id.date - self.time <= timedelta(days=10) and contract_data["impliedvolatility"] > 0.5 and contract_data["delta"] < 0.5
        ]

        # Get the contract with the latest expiration date
        option_contract = sorted(contracts, key=lambda x: x.id.date, reverse=True)[0]

        # Can use the symbol instance to index the data frame
        self.debug(f"Option contract data:\n{option_chain.data_frame.loc[(option_contract)]}")

        self._option_contract = self.add_option_contract(option_contract)

    def on_data(self, data):
        # Do some trading with the selected contract for sample purposes
        if not self.portfolio.invested:
            self.market_order(self._option_contract, 1)
        else:
            self.liquidate()
