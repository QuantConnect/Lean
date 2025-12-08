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
### Regression algorithm illustrating the usage of the <see cref="QCAlgorithm.FuturesChain(Symbol, bool)"/>
### method to get a future chain.
### </summary>
class FuturesChainFullDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)

        future = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE).symbol

        chain = self.futures_chain(future, flatten=True)

        # Demonstration using data frame:
        df = chain.data_frame

        for index, row in df.iterrows():
            if row['bidprice'] == 0 and row['askprice'] == 0 and row['volume'] == 0:
                raise AssertionError("FuturesChain() returned contract with no data.");

        # Get contracts expiring within 6 months, with the latest expiration date, and lowest price
        contracts = df.loc[(df.expiry <= self.time + timedelta(days=180))]
        contracts = contracts.sort_values(['expiry', 'lastprice'], ascending=[False, True])
        self._future_contract = contracts.index[0]

        self.add_future_contract(self._future_contract)

    def on_data(self, data):
        # Do some trading with the selected contract for sample purposes
        if not self.portfolio.invested:
            self.set_holdings(self._future_contract, 0.5)
        else:
            self.liquidate()
