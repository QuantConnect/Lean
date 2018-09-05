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
AddReference("System.Core")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm
from datetime import date

### <summary>
### Demonstration of how to define a universe as a combination of use the coarse fundamental data and fine fundamental data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="regression test" />
class CoarseFineFundamentalRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014,3,24)   #Set Start Date
        self.SetEndDate(2014,4,7)      #Set End Date
        self.SetCash(50000)            #Set Strategy Cash

        self.UniverseSettings.Resolution = Resolution.Daily

        # this add universe method accepts two parameters:
        # - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
        # - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)

        self.changes = None
        self.numberOfSymbolsFine = 2

    # return a list of three fixed symbol objects
    def CoarseSelectionFunction(self, coarse):
        tickers = [ "GOOG", "BAC", "SPY" ]

        if self.Time.date() < date(2014, 4, 1):
            tickers = [ "AAPL", "AIG", "IBM" ]

        return [ Symbol.Create(x, SecurityType.Equity, Market.USA) for x in tickers ]


    # sort the data by P/E ratio and take the top 'NumberOfSymbolsFine'
    def FineSelectionFunction(self, fine):
        # sort descending by P/E ratio
        sortedByPeRatio = sorted(fine, key=lambda x: x.ValuationRatios.PERatio, reverse=True)

        # take the top entries from our sorted collection
        return [ x.Symbol for x in sortedByPeRatio[:self.numberOfSymbolsFine] ]

    def OnData(self, data):
        # if we have no changes, do nothing
        if self.changes is None: return

        # liquidate removed securities
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)
                self.Debug("Liquidated Stock: " + str(security.Symbol.Value))

        # we want 50% allocation in each security in our universe
        for security in self.changes.AddedSecurities:
            if (security.Fundamentals.EarningRatios.EquityPerShareGrowth.OneYear > 0.25):
                self.SetHoldings(security.Symbol, 0.5)
                self.Debug("Purchased Stock: " + str(security.Symbol.Value))

        self.changes = None

    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self.changes = changes