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
from QuantConnect.Data.Market import *
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm
from datetime import timedelta

### <summary>
### This algorithm shows some of the various helper methods available when defining universes
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="coarse universes" />
class UniverseSelectionDefinitionsAlgorithm(QCAlgorithm):

    def Initialize(self):
        # subscriptions added via universe selection will have this resolution
        self.UniverseSettings.Resolution = Resolution.Hour
        # force securities to remain in the universe for a minimm of 30 minutes
        self.UniverseSettings.MinimumTimeInUniverse = timedelta(minutes=30)

        self.SetStartDate(2013,10,7)    # Set Start Date
        self.SetEndDate(2013,10,11)     # Set End Date
        self.SetCash(100000)            # Set Strategy Cash

        # add universe for the top 50 stocks by dollar volume
        self.AddUniverse(self.Universe.DollarVolume.Top(50))

        # add universe for the bottom 50 stocks by dollar volume
        self.AddUniverse(self.Universe.DollarVolume.Bottom(50))

        # add universe for the 90th dollar volume percentile
        self.AddUniverse(self.Universe.DollarVolume.Percentile(90.0))

        # add universe for stocks between the 70th and 80th dollar volume percentile
        self.AddUniverse(self.Universe.DollarVolume.Percentile(70.0, 80.0))

        self.changes = None

    def OnData(self, data):
        if self.changes is None: return

        # liquidate securities that fell out of our universe
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        # invest in securities just added to our universe
        for security in self.changes.AddedSecurities:
            if not security.Invested:
                self.MarketOrder(security.Symbol, 10)

        self.changes = None


    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        self.changes = changes