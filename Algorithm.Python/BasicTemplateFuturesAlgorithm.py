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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Securities import *
from datetime import timedelta

### <summary>
### This example demonstrates how to add futures for a given underlying asset.
### It also shows how you can prefilter contracts easily based on expirations, and how you
### can inspect the futures chain to pick a specific contract to trade.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="benchmarks" />
### <meta name="tag" content="futures" />
class BasicTemplateFuturesAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 10)
        self.SetCash(1000000)

        # Subscribe and set our expiry filter for the futures chain
        futureES = self.AddFuture(Futures.Indices.SP500EMini)
        futureES.SetFilter(timedelta(0), timedelta(182))

        futureGC = self.AddFuture(Futures.Metals.Gold)
        futureGC.SetFilter(timedelta(0), timedelta(182))

        benchmark = self.AddEquity("SPY");
        self.SetBenchmark(benchmark.Symbol);


    def OnData(self,slice):
        if not self.Portfolio.Invested:
            for chain in slice.FutureChains:
                 # Get contracts expiring no earlier than in 90 days
                contracts = list(filter(lambda x: x.Expiry > self.Time + timedelta(90), chain.Value))

                # if there is any contract, trade the front contract
                if len(contracts) == 0: continue
                front = sorted(contracts, key = lambda x: x.Expiry, reverse=True)[0]
                self.MarketOrder(front.Symbol , 1)
        else:
            self.Liquidate()
