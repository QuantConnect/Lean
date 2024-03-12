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

from datetime import datetime
from AlgorithmImports import *

### <summary>
### Custom data universe selection regression algorithm asserting it's behavior. See GH issue #6396
### </summary>
class NoUniverseSelectorRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2014, 3, 24)
        self.SetEndDate(2014, 3, 31)

        self.UniverseSettings.Resolution = Resolution.Daily;
        self.AddUniverse(CoarseFundamental)
        self.changes = None

    def OnData(self, data):
        # if we have no changes, do nothing
        if not self.changes: return

        # liquidate removed securities
        for security in self.changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol)

        activeAndWithDataSecurities = sum(x.Value.HasData for x in self.ActiveSecurities)
        # we want 1/N allocation in each security in our universe
        for security in self.changes.AddedSecurities:
            if security.HasData:
                self.SetHoldings(security.Symbol, 1 / activeAndWithDataSecurities)
        self.changes = None

    def OnSecuritiesChanged(self, changes):
        self.changes = changes
