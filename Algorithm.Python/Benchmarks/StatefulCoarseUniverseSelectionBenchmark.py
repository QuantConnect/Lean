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
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *


class StatefulCoarseUniverseSelectionBenchmark(QCAlgorithm):

    def Initialize(self):
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2017, 11, 1)
        self.SetEndDate(2018, 1, 1)
        self.SetCash(50000)

        self.AddUniverse(self.CoarseSelectionFunction)
        self.numberOfSymbols = 250
        self._blackList = []

    # sort the data by daily dollar volume and take the top 'NumberOfSymbols'
    def CoarseSelectionFunction(self, coarse):

        selected = [x for x in coarse if (x.HasFundamentalData)]
        # sort descending by daily dollar volume
        sortedByDollarVolume = sorted(selected, key=lambda x: x.DollarVolume, reverse=True)

        # return the symbol objects of the top entries from our sorted collection
        return [ x.Symbol for x in sortedByDollarVolume[:self.numberOfSymbols] if not (x.Symbol in self._blackList) ]

    def OnData(self, slice):
        if slice.HasData:
            symbol = slice.Keys[0]
            if symbol:
                if len(self._blackList) > 50:
                    self._blackList.pop(0)
                self._blackList.append(symbol)

    def OnSecuritiesChanged(self, changes):
        # if we have no changes, do nothing
        if changes is None: return

        # liquidate removed securities
        for security in changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        for security in changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 0.001)