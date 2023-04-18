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
    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2014, 10, 10)
        self.SetCash(1000000)

        resolution = self.GetResolution()
        extendedMarketHours = self.GetExtendedMarketHours()

        # Subscribe and set our expiry filter for the futures chain
        self.futureSP500 = self.AddFuture(Futures.Indices.SP500EMini, resolution, extendedMarket=extendedMarketHours)
        self.futureGold = self.AddFuture(Futures.Metals.Gold, resolution, extendedMarket=extendedMarketHours)

        # set our expiry filter for this futures chain
        # SetFilter method accepts timedelta objects or integer for days.
        # The following statements yield the same filtering criteria
        self.futureSP500.SetFilter(timedelta(0), timedelta(182))
        self.futureGold.SetFilter(0, 182)

    def OnData(self,slice):
        if not self.Portfolio.Invested:
            for chain in slice.FutureChains:
                 # Get contracts expiring no earlier than in 90 days
                contracts = list(filter(lambda x: x.Expiry > self.Time + timedelta(90), chain.Value))

                # if there is any contract, trade the front contract
                if len(contracts) == 0: continue
                contract = sorted(contracts, key = lambda x: x.Expiry)[0]

                # if found, trade it.
                # Also check if exchange is open for regular or extended hours. Since daily data comes at 8PM, this allows us prevent the
                # algorithm from trading on friday when there is not after-market.
                if self.Securities[contract.Symbol].Exchange.Hours.IsOpen(self.Time, True):
                    self.MarketOrder(contract.Symbol, 1)
        # Same as above, check for cases like trading on a friday night.
        elif all(x.Exchange.Hours.IsOpen(self.Time, True) for x in self.Securities.Values if x.Invested):
            self.Liquidate()

    def GetResolution(self):
        return Resolution.Daily

    def GetExtendedMarketHours(self):
        return False
