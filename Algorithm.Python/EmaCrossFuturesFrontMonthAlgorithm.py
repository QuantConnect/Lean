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
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Data.Market import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Securities import *

### <summary>
### This example demonstrates how to implement a cross moving average for the futures front contract
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="indicator" />
### <meta name="tag" content="futures" />
class EmaCrossFuturesFrontMonthAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 10)
        self.SetCash(1000000)

        future = self.AddFuture(Futures.Metals.Gold);

        # Only consider the front month contract
        # Update the universe once per day to improve performance
        future.SetFilter(lambda x: x.FrontMonth().OnlyApplyFilterAtMarketOpen())

        # Symbol of the current contract
        self.symbol = None

        # Create two exponential moving averages
        self.fast = ExponentialMovingAverage(100)
        self.slow = ExponentialMovingAverage(300)
        self.tolerance = 0.001
        self.consolidator = None

        # Add a custom chart to track the EMA cross
        chart = Chart('EMA Cross')
        chart.AddSeries(Series('Fast', SeriesType.Line, 0))
        chart.AddSeries(Series('Slow', SeriesType.Line, 0))
        self.AddChart(chart)

    def OnData(self,slice):

        holding = None if self.symbol is None else self.Portfolio.get(self.symbol)
        if holding is not None:
            # Buy the futures' front contract when the fast EMA is above the slow one
            if self.fast.Current.Value > self.slow.Current.Value * (1 + self.tolerance):
                if not holding.Invested:
                    self.SetHoldings(self.symbol, .1)
                    self.PlotEma()
            elif holding.Invested:
                self.Liquidate(self.symbol)
                self.PlotEma()

    def OnSecuritiesChanged(self, changes):
        if len(changes.RemovedSecurities) > 0:
            # Remove the consolidator for the previous contract
            # and reset the indicators
            if self.symbol is not None and self.consolidator is not None:
                self.SubscriptionManager.RemoveConsolidator(self.symbol, self.consolidator)
                self.fast.Reset()
                self.slow.Reset()
            # We don't need to call Liquidate(_symbol),
            # since its positions are liquidated because the contract has expired.

        # Only one security will be added: the new front contract
        self.symbol = changes.AddedSecurities[0].Symbol

        # Create a new consolidator and register the indicators to it
        self.consolidator = self.ResolveConsolidator(self.symbol, Resolution.Minute)
        self.RegisterIndicator(self.symbol, self.fast, self.consolidator)
        self.RegisterIndicator(self.symbol, self.slow, self.consolidator)

        #  Warm up the indicators
        self.WarmUpIndicator(self.symbol, self.fast, Resolution.Minute)
        self.WarmUpIndicator(self.symbol, self.slow, Resolution.Minute)

        self.PlotEma()

    def PlotEma(self):
        self.Plot('EMA Cross', 'Fast', self.fast.Current.Value)
        self.Plot('EMA Cross', 'Slow', self.slow.Current.Value)
