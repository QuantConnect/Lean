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
### This example demonstrates how to add futures with daily resolution.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="benchmarks" />
### <meta name="tag" content="futures" />
class BasicTemplateFuturesDailyAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 8)
        self.set_end_date(2014, 10, 10)
        self.set_cash(1000000)

        resolution = self.get_resolution()
        extended_market_hours = self.get_extended_market_hours()

        # Subscribe and set our expiry filter for the futures chain
        self.future_sp500 = self.add_future(Futures.Indices.SP_500_E_MINI, resolution, extended_market_hours=extended_market_hours)
        self.future_gold = self.add_future(Futures.Metals.GOLD, resolution, extended_market_hours=extended_market_hours)

        # set our expiry filter for this futures chain
        # SetFilter method accepts timedelta objects or integer for days.
        # The following statements yield the same filtering criteria
        self.future_sp500.set_filter(timedelta(0), timedelta(182))
        self.future_gold.set_filter(0, 182)

    def on_data(self,slice):
        if not self.portfolio.invested:
            for chain in slice.future_chains:
                 # Get contracts expiring no earlier than in 90 days
                contracts = list(filter(lambda x: x.expiry > self.time + timedelta(90), chain.value))

                # if there is any contract, trade the front contract
                if len(contracts) == 0: continue
                contract = sorted(contracts, key = lambda x: x.expiry)[0]

                # if found, trade it.
                self.market_order(contract.symbol, 1)
        # Same as above, check for cases like trading on a friday night.
        elif all(x.exchange.hours.is_open(self.time, True) for x in self.securities.values() if x.invested):
            self.liquidate()

    def get_resolution(self):
        return Resolution.DAILY

    def get_extended_market_hours(self):
        return False
