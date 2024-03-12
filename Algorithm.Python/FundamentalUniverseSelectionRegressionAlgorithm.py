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
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

### <summary>
### Demonstration of how to define a universe using the fundamental data
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="regression test" />
class FundamentalUniverseSelectionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 3, 26)
        self.SetEndDate(2014, 4, 7)

        self.UniverseSettings.Resolution = Resolution.Daily

        self.AddEquity("SPY")
        self.AddEquity("AAPL")

        self.SetUniverseSelection(FundamentalUniverseSelectionModelTest())

        self.changes = None

    # return a list of three fixed symbol objects
    def SelectionFunction(self, fundamental):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted([x for x in fundamental if x.Price > 1],
            key=lambda x: x.DollarVolume, reverse=True)

        # sort descending by P/E ratio
        sortedByPeRatio = sorted(sortedByDollarVolume, key=lambda x: x.ValuationRatios.PERatio, reverse=True)

        # take the top entries from our sorted collection
        return [ x.Symbol for x in sortedByPeRatio[:self.numberOfSymbolsFundamental] ]

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
            self.SetHoldings(security.Symbol, 0.02)

        self.changes = None

    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self.changes = changes

class FundamentalUniverseSelectionModelTest(FundamentalUniverseSelectionModel):
    def Select(self, algorithm, fundamental):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted([x for x in fundamental if x.HasFundamentalData and x.Price > 1],
            key=lambda x: x.DollarVolume, reverse=True)

        # sort descending by P/E ratio
        sortedByPeRatio = sorted(sortedByDollarVolume, key=lambda x: x.ValuationRatios.PERatio, reverse=True)

        # take the top entries from our sorted collection
        return [ x.Symbol for x in sortedByPeRatio[:2] ]
