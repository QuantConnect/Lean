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
### Regression algorithm to test universe additions and removals with open positions
### </summary>
### <meta name="tag" content="regression test" />
class WeeklyUniverseSelectionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(100000)
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,10,31)

        self.UniverseSettings.Resolution = Resolution.Hour

        # select IBM once a week, empty universe the other days
        self.AddUniverse("my-custom-universe", lambda dt: ["IBM"] if dt.day % 7 == 0 else [])

    def OnData(self, slice):
        if self.changes is None: return

        # liquidate removed securities
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                self.Log("{} Liquidate {}".format(self.Time, security.Symbol))
                self.Liquidate(security.Symbol)

        # we'll simply go long each security we added to the universe
        for security in self.changes.AddedSecurities:
            if not security.Invested:
                self.Log("{} Buy {}".format(self.Time, security.Symbol))
                self.SetHoldings(security.Symbol, 1)

        self.changes = None

    def OnSecuritiesChanged(self, changes):
        self.changes = changes