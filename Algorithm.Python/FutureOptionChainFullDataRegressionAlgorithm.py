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
### to get a future option chain.
### </summary>
class FutureOptionChainFullDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2020, 1, 6)
        self.set_end_date(2020, 1, 6)

        future_contract = self.add_future_contract(
            Symbol.create_future(Futures.Indices.SP_500_E_MINI, Market.CME, datetime(2020, 3, 20)),
            Resolution.MINUTE).symbol

        option_chain = self.option_chain(future_contract, flatten=True)

        # Demonstration using data frame:
        df = option_chain.data_frame
        # Get contracts expiring within 4 months, with the latest expiration date, highest strike and lowest price
        contracts = df.loc[(df.expiry <= self.time + timedelta(days=120))]
        contracts = contracts.sort_values(['expiry', 'strike', 'lastprice'], ascending=[False, False, True])
        self._option_contract = contracts.index[0]

        self.add_future_option_contract(self._option_contract)

    def on_data(self, data):
        # Do some trading with the selected contract for sample purposes
        if not self.portfolio.invested:
            self.set_holdings(self._option_contract, 0.5)
        else:
            self.liquidate()
