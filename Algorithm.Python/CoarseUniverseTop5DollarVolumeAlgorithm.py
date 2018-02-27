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
AddReference("System.Collections")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from System.Collections.Generic import List
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *

### <summary>
### In this algorithm we demonstrate how to use the coarse fundamental data to
### define a universe as the top dollar volume
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
### <meta name="tag" content="regression test" />
class CoarseFundamentalTop5Algorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014, 1, 1)  #Set Start Date
        self.SetEndDate(2015, 1, 1)    #Set End Date
        self.SetCash(50000)            #Set Strategy Cash

        # what resolution should the data *added* to the universe be?
        self.UniverseSettings.Resolution = Resolution.Daily

        # this add universe method accepts two parameters:
        # - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
        self.AddUniverse(self.CoarseSelectionFunction)
        self.numberOfSymbols = 5
        # initialize our changes to nothing
        self.changes = None

    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)
        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:self.numberOfSymbols]]

    def OnData(self, data):
        keys = ", ".join(sorted(str(i) for i in data.Keys))
        self.Debug("OnData({0}): Keys: {1}".format(self.UtcTime, keys))
        # if we have no changes, do nothing
        if self.changes == None: return

        # liquidate removed securities
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        # we want 20% allocation in each security in our universe
        for security in self.changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 0.2)
        self.changes = None

    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self.changes = changes
    
    def OnOrderEvent(self, fill):
        self.Debug("OnOrderEvent({0}):: {1}".format(self.UtcTime, fill))