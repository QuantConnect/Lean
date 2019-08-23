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

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import Universe
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from datetime import date, timedelta

### <summary>
### Regression algorithm used to test a fine and coarse selection methods returning Universe.Unchanged
### </summary>
class UniverseUnchangedRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.UniverseSettings.Resolution = Resolution.Daily
        self.SetStartDate(2014,3,25)
        self.SetEndDate(2014,4,7)

        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(days = 1), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        self.AddUniverse(self.CoarseSelectionFunction, self.FineSelectionFunction)

        self.numberOfSymbolsFine = 2

    def CoarseSelectionFunction(self, coarse):
        # the first and second selection
        if self.Time.date() <= date(2014, 3, 26):
            tickers = [ "AAPL", "AIG", "IBM" ]
            return [ Symbol.Create(x, SecurityType.Equity, Market.USA) for x in tickers ]

        # will skip fine selection
        return Universe.Unchanged

    def FineSelectionFunction(self, fine):
        if self.Time.date() == date(2014, 3, 25):
            sortedByPeRatio = sorted(fine, key=lambda x: x.ValuationRatios.PERatio, reverse=True)
            return [ x.Symbol for x in sortedByPeRatio[:self.numberOfSymbolsFine] ]

        # the second selection will return unchanged, in the following fine selection will be skipped
        return Universe.Unchanged

    # assert security changes, throw if called more than once
    def OnSecuritiesChanged(self, changes):
        addedSymbols = [ x.Symbol for x in changes.AddedSecurities ]
        if (len(changes.AddedSecurities) != 2
            or self.Time.date() != date(2014, 3, 25)
            or Symbol.Create("AAPL", SecurityType.Equity, Market.USA) not in addedSymbols
            or Symbol.Create("IBM", SecurityType.Equity, Market.USA) not in addedSymbols):
            raise ValueError("Unexpected security changes")
        self.Log(f"OnSecuritiesChanged({self.Time}):: {changes}")
