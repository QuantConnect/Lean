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
from QuantConnect.Data.UniverseSelection import ConstituentsUniverse

### <summary>
### Test algorithm using a 'ConstituentsUniverse' with test data
### </summary>
class ConstituentsUniverseRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)  #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash

        self._appl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
        self._spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        self._qqq = Symbol.Create("QQQ", SecurityType.Equity, Market.USA)
        self._fb = Symbol.Create("FB", SecurityType.Equity, Market.USA)
        self._step = 0

        self.UniverseSettings.Resolution = Resolution.Daily

        self.AddUniverse(ConstituentsUniverse(
            Symbol.Create("constituents-universe-qctest", SecurityType.Equity, Market.USA),
            self.UniverseSettings))

    def OnData(self, data):
        self._step = self._step + 1
        if self._step == 1:
            if not data.ContainsKey(self._qqq) or not data.ContainsKey(self._appl):
                raise ValueError("Unexpected symbols found, step: " + str(self._step))
            if data.Count != 2:
                raise ValueError("Unexpected data count, step: " + str(self._step))
            # AAPL will be deselected by the ConstituentsUniverse
            # but it shouldn't be removed since we hold it
            self.SetHoldings(self._appl, 0.5)
        elif self._step == 2:
            if not data.ContainsKey(self._appl):
                raise ValueError("Unexpected symbols found, step: " + str(self._step))
            if data.Count != 1:
                raise ValueError("Unexpected data count, step: " + str(self._step))
            # AAPL should now be released
            # note: takes one extra loop because the order is executed on market open
            self.Liquidate()
        elif self._step == 3:
            if not data.ContainsKey(self._fb) or not data.ContainsKey(self._spy) or not data.ContainsKey(self._appl):
                raise ValueError("Unexpected symbols found, step: " + str(self._step))
            if data.Count != 3:
                raise ValueError("Unexpected data count, step: " + str(self._step))
        elif self._step == 4:
            if not data.ContainsKey(self._fb) or not data.ContainsKey(self._spy):
                raise ValueError("Unexpected symbols found, step: " + str(self._step))
            if data.Count != 2:
                raise ValueError("Unexpected data count, step: " + str(self._step))
        elif self._step == 5:
            if not data.ContainsKey(self._fb) or not data.ContainsKey(self._spy):
                raise ValueError("Unexpected symbols found, step: " + str(self._step))
            if data.Count != 2:
                raise ValueError("Unexpected data count, step: " + str(self._step))

    def OnEndOfAlgorithm(self):
        if self._step != 5:
            raise ValueError("Unexpected step count: " + str(self._step))

    def  OnSecuritiesChanged(self, changes):
        for added in changes.AddedSecurities:
            self.Log("AddedSecurities " + str(added))

        for removed in changes.RemovedSecurities:
            self.Log("RemovedSecurities " + str(removed) + str(self._step))
            # we are currently notifying the removal of AAPl twice,
            # when deselected and when finally removed (since it stayed pending)
            if removed.Symbol == self._appl and self._step != 1 and self._step != 2 or removed.Symbol == self._qqq and self._step != 1:
                raise ValueError("Unexpected removal step count: " + str(self._step))