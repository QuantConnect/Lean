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
        self.futureSP500 = self.AddFuture(Futures.Indices.SP500EMini, resolution, extendedMarketHours=extendedMarketHours)
        self.futureGold = self.AddFuture(Futures.Metals.Gold, resolution, extendedMarketHours=extendedMarketHours)

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
                # Let's check if market is actually open to place market orders. For example: for daily resolution, data can come at a
                # time when market is closed, like 7:00PM.
                if self.Securities[contract.Symbol].Exchange.ExchangeOpen:
                    self.MarketOrder(contract.Symbol, 1)
                else:
                    # MOO are not allowed for futures, so to make sure, use limit order instead. We use a very big limit price here
                    # to make the order fill on next bar.
                    self.LimitOrder(contract.Symbol, 1, contract.AskPrice * 2)
        else:
            # Same as above, let's check if market is open to place market orders.
            if any([not self.Securities[x.Symbol].Exchange.ExchangeOpen for x in self.Portfolio.values()]):
                for holdings in sorted(self.Portfolio.values(), key=lambda x: x.Symbol):
                    # use a very low limit price here to make the order fill on next bar.
                    self.LimitOrder(holdings.Symbol, -holdings.Quantity, 1.0)
            else:
                self.Liquidate()

    def GetResolution(self):
        return Resolution.Daily

    def GetExtendedMarketHours(self):
        return False
