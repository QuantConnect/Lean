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

from System import *
from System.Collections.Generic import List
from QuantConnect import *
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm


class CoarseFineUniverseSelectionBenchmark(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2017, 1, 1)  
        self.SetEndDate(2018, 1, 1)    
        self.SetCash(50000)            

        self.UniverseSettings.Resolution = Resolution.Daily

        self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)

        self.numberOfSymbols = 30
        self.numberOfSymbolsFine = 2
        self._changes = None

    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):
        
        selected = [x for x in coarse if (x.HasFundamentalData)]
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(selected, key=lambda x: x.DollarVolume, reverse=True)

        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:self.numberOfSymbols] ]

    # sort the data by P/E ratio and take the top 'NumberOfSymbolsFine'
    def FineSelectionFunction(self, fine):
        # sort descending by P/E ratio
        sortedByPeRatio = sorted(fine, key=lambda x: x.ValuationRatios.PERatio, reverse=True)
        # take the top entries from our sorted collection
        return [ x.Symbol for x in sortedByPeRatio[:self.numberOfSymbolsFine] ]

    def OnData(self, data):
        # if we have no changes, do nothing
        if self._changes is None: return

        # liquidate removed securities
        for security in self._changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        for security in self._changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 0.5)
        self._changes = None;

    def OnSecuritiesChanged(self, changes):
        self._changes = changes