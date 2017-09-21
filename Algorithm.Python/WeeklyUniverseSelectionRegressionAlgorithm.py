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
import numpy as np

### <summary>
### Regression algorithm to test universe additions and removals with open positions
### </summary>
### <meta name="tag" content="regression test" />
class WeeklyUniverseSelectionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(100000)
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,10,31)
        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverse(self.CoarseSelectionFunction)

    def CoarseSelectionFunction(self, coarse):
        # select IBM once a week
        list = List[Symbol]()
        if self.Time.day % 7 == 0:
            list.Add(self.AddEquity("IBM").Symbol)
        return list

    def OnData(self, slice):
        if self._changes == SecurityChanges.None: return

        # liquidate removed securities
        for security in self._changes.RemovedSecurities:
            if security.Invested:
                self.Log(str(self.Time) + " Liquidate " + str(security.Symbol.Value))
                self.Liquidate(security.Symbol)

        # we'll simply go long each security we added to the universe
        for security in self._changes.AddedSecurities:
            if not security.Invested:
                self.Log(str(Time) + " Buy " + str(security.Symbol.Value))
                self.SetHoldings(security.Symbol, 1)

        self._changes = SecurityChanges.None


    def OnSecuritiesChanged(self, changes):
        # Event fired each time the we add/remove securities from the data feed
        # <param name="changes">Object containing AddedSecurities and RemovedSecurities</param>
        self._changes = changes