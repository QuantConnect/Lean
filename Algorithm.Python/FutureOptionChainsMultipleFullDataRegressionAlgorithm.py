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
### Regression algorithm illustrating the usage of the <see cref="QCAlgorithm.OptionChains(IEnumerable{Symbol})"/> method
### to get multiple future option chains.
### </summary>
class FutureOptionChainsMultipleFullDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2020, 1, 6)
        self.set_end_date(2020, 1, 6)

        es_future_contract = self.add_future_contract(
            Symbol.create_future(Futures.Indices.SP_500_E_MINI, Market.CME, datetime(2020, 3, 20)),
            Resolution.MINUTE).symbol

        gc_future_contract = self.add_future_contract(
            Symbol.create_future(Futures.Metals.GOLD, Market.COMEX, datetime(2020, 4, 28)),
            Resolution.MINUTE).symbol

        chains = self.option_chains([es_future_contract, gc_future_contract], flatten=True)

        self._es_option_contract = self.get_contract(chains, es_future_contract)
        self._gc_option_contract = self.get_contract(chains, gc_future_contract)

        self.add_future_option_contract(self._es_option_contract)
        self.add_future_option_contract(self._gc_option_contract)

    def get_contract(self, chains: OptionChains, underlying: Symbol) -> Symbol:
        df = chains.data_frame

        # Index by the requested underlying, by getting all data with canonicals which underlying is the requested underlying symbol:
        canonicals = df.index.get_level_values('canonical')
        condition = [canonical for canonical in canonicals if canonical.underlying == underlying]
        contracts = df.loc[condition]

        # Get contracts expiring within 4 months, with the latest expiration date, highest strike and lowest price
        contracts = contracts.loc[(df.expiry <= self.time + timedelta(days=120))]
        contracts = contracts.sort_values(['expiry', 'strike', 'lastprice'], ascending=[False, False, True])

        return contracts.index[0][1]

    def on_data(self, data):
        # Do some trading with the selected contract for sample purposes
        if not self.portfolio.invested:
            self.set_holdings(self._es_option_contract, 0.25)
            self.set_holdings(self._gc_option_contract, 0.25)
        else:
            self.liquidate()
