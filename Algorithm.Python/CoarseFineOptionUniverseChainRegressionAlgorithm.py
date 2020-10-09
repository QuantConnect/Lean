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
from QuantConnect.Algorithm import *
from QuantConnect.Data.UniverseSelection import *
from datetime import *

### <summary>
### Demonstration of how to chain a coarse and fine universe selection with an option chain universe selection model
### that will add and remove an'OptionChainUniverse' for each symbol selected on fine
### </summary>
class CoarseFineOptionUniverseChainRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014,6,5)  #Set Start Date
        self.SetEndDate(2014,6,6)    #Set End Date

        self.UniverseSettings.Resolution = Resolution.Minute
        self._twx = Symbol.Create("TWX", SecurityType.Equity, Market.USA)
        self._aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
        self._lastEquityAdded = None
        self._changes = None
        self._optionCount = 0

        universe = self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)
        
        self.AddUniverseOptions(universe, self.OptionFilterFunction)

    def OptionFilterFunction(self, universe):
        universe.IncludeWeeklys().FrontMonth()

        contracts = list()
        for symbol in universe:
            if len(contracts) == 5:
                break
            contracts.append(symbol)
        return universe.Contracts(contracts)

    def CoarseSelectionFunction(self, coarse):
        if self.Time <= datetime(2014,6,5):
            return [ self._twx ]
        return [ self._aapl ]

    def FineSelectionFunction(self, fine):
        if self.Time <= datetime(2014,6,5):
            return [ self._twx ]
        return [ self._aapl ]

    def OnData(self, data):
        if self._changes == None or any(security.Price == 0 for security in self._changes.AddedSecurities):
            return

        # liquidate removed securities
        for security in self._changes.RemovedSecurities:
            if security.Invested:
                self.Liquidate(security.Symbol);

        for security in self._changes.AddedSecurities:
            if not security.Symbol.HasUnderlying:
                self._lastEquityAdded = security.Symbol;
            else:
                # options added should all match prev added security
                if security.Symbol.Underlying != self._lastEquityAdded:
                    raise ValueError(f"Unexpected symbol added {security.Symbol}")
                self._optionCount += 1

            self.SetHoldings(security.Symbol, 0.05)
        self._changes = None

    # this event fires whenever we have changes to our universe
    def OnSecuritiesChanged(self, changes):
        if self._changes == None:
            self._changes = changes
            return
        self._changes = self._changes.op_Addition(self._changes, changes)

    def OnEndOfAlgorithm(self):
        if self._optionCount == 0:
            raise ValueError("Option universe chain did not add any option!")
