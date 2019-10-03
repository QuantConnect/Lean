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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Algorithm.Framework.Selection import *
from QuantConnect.Data.UniverseSelection import *
from datetime import timedelta

### <summary>
### Regression algorithm to test universe additions and removals with open positions
### </summary>
### <meta name="tag" content="regression test" />
class InceptionDateSelectionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,10,31)
        self.SetCash(100000)

        self.changes = None
        self.UniverseSettings.Resolution = Resolution.Hour

        # select IBM once a week, empty universe the other days
        self.AddUniverseSelection(CustomUniverseSelectionModel("my-custom-universe", lambda dt: ["IBM"] if dt.day % 7 == 0 else []))
        # Adds SPY 5 days after StartDate and keep it in Universe
        self.AddUniverseSelection(InceptionDateUniverseSelectionModel("spy-inception", {"SPY": self.StartDate + timedelta(5)}));


    def OnData(self, slice):
        if self.changes is None:
           return

        # we'll simply go long each security we added to the universe
        for security in self.changes.AddedSecurities:
            self.SetHoldings(security.Symbol, .5)

        self.changes = None

    def OnSecuritiesChanged(self, changes):
        # liquidate removed securities
        for security in changes.RemovedSecurities:
            self.Liquidate(security.Symbol, "Removed from Universe")

        self.changes = changes