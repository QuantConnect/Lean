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

### <summary>
### Demonstration of using coarse and fine universe selection together to filter down a smaller universe of stocks.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="fine universes" />
class CoarseFundamentalTop3Algorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014,3,24)    #Set Start Date
        self.SetEndDate(2014,4,7)      #Set End Date
        self.SetCash(50000)            #Set Strategy Cash

        # what resolution should the data *added* to the universe be?
        self.UniverseSettings.Resolution = Resolution.Daily

        # this add universe method accepts a single parameter that is a function that
        # accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction)

        self.__numberOfSymbols = 3
        self._changes = None


    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:self.__numberOfSymbols] ]


    def OnData(self, data):

        self.Log(f"OnData({self.UtcTime}): Keys: {', '.join([key.Value for key in data.Keys])}")

        # if we have no changes, do nothing
        if self._changes is None: return

        # liquidate removed securities
        for security in self._changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        # we want 1/N allocation in each security in our universe
        for security in self._changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 1 / self.__numberOfSymbols)

        self._changes = None


    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self._changes = changes
        self.Log(f"OnSecuritiesChanged({self.UtcTime}):: {changes}")

    def OnOrderEvent(self, fill):
        self.Log(f"OnOrderEvent({self.UtcTime}):: {fill}")