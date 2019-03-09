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
import numpy as np

### <summary>
### EMA cross with SP500 E-mini futures
### In this example, we demostrate how to trade futures contracts using
### a equity to generate the trading signals
### It also shows how you can prefilter contracts easily based on expirations.
### It also shows how you can inspect the futures chain to pick a specific contract to trade.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="futures" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="strategy example" />
class FuturesMomentumAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2016, 1, 1)
        self.SetEndDate(2016, 8, 18)
        self.SetCash(100000)
        fastPeriod = 20
        slowPeriod = 60
        self._tolerance = 1 + 0.001
        self.IsUpTrend = False
        self.IsDownTrend = False
        self.SetWarmUp(max(fastPeriod, slowPeriod))

        # Adds SPY to be used in our EMA indicators
        equity = self.AddEquity("SPY", Resolution.Daily)
        self._fast = self.EMA(equity.Symbol, fastPeriod, Resolution.Daily)
        self._slow = self.EMA(equity.Symbol, slowPeriod, Resolution.Daily)
        # Adds the future that will be traded and
        # set our expiry filter for this futures chain
        future = self.AddFuture(Futures.Indices.SP500EMini)
        future.SetFilter(timedelta(0), timedelta(182))


    def OnData(self, slice):
        if self._slow.IsReady and self._fast.IsReady:
            self.IsUpTrend = self._fast.Current.Value > self._slow.Current.Value * self._tolerance
            self.IsDownTrend = self._fast.Current.Value < self._slow.Current.Value * self._tolerance
            if (not self.Portfolio.Invested) and self.IsUpTrend:
                for chain in slice.FuturesChains:
                    # find the front contract expiring no earlier than in 90 days
                    contracts = list(filter(lambda x: x.Expiry > self.Time + timedelta(90), chain.Value))
                    # if there is any contract, trade the front contract
                    if len(contracts) == 0: continue
                    contract = sorted(contracts, key = lambda x: x.Expiry, reverse=True)[0]
                    self.MarketOrder(contract.Symbol , 1)

            if self.Portfolio.Invested and self.IsDownTrend:
                self.Liquidate()

    def OnEndOfDay(self):
        if self.IsUpTrend:
            self.Plot("Indicator Signal", "EOD",1)
        elif self.IsDownTrend:
            self.Plot("Indicator Signal", "EOD",-1)
        elif self._slow.IsReady and self._fast.IsReady:
            self.Plot("Indicator Signal", "EOD",0)


    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))