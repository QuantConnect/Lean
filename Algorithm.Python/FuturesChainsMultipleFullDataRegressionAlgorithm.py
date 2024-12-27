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
### Regression algorithm illustrating the usage of the <see cref="QCAlgorithm.FuturesChains(IEnumerable{Symbol}, bool)"/>
### method to get multiple futures chains.
### </summary>
class FuturesChainsMultipleFullDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)

        es_future = self.add_future(Futures.Indices.SP_500_E_MINI).symbol
        gc_future = self.add_future(Futures.Metals.GOLD).symbol

        chains = self.futures_chains([es_future, gc_future], flatten=True)

        self._es_contract = self.get_contract(chains, es_future)
        self._gc_contract = self.get_contract(chains, gc_future)

        self.add_future_contract(self._es_contract)
        self.add_future_contract(self._gc_contract)

    def get_contract(self, chains: FuturesChains, canonical: Symbol) -> Symbol:
        df = chains.data_frame

        # Index by the requested underlying, by getting all data with canonicals which underlying is the requested underlying symbol:
        canonicals = df.index.get_level_values('canonical')
        condition = [symbol for symbol in canonicals if symbol == canonical]
        contracts = df.loc[condition]

        # Get contracts expiring within 6 months, with the latest expiration date, and lowest price
        contracts = contracts.loc[(df.expiry <= self.time + timedelta(days=180))]
        contracts = contracts.sort_values(['expiry', 'lastprice'], ascending=[False, True])

        return contracts.index[0][1]

    def on_data(self, data):
        # Do some trading with the selected contract for sample purposes
        if not self.portfolio.invested:
            self.set_holdings(self._es_contract, 0.25)
            self.set_holdings(self._gc_contract, 0.25)
        else:
            self.liquidate()
