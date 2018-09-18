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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import *
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Alphas.ConstantAlphaModel import ConstantAlphaModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from Risk.MaximumSectorExposureRiskManagementModel import MaximumSectorExposureRiskManagementModel
from datetime import date, timedelta

### <summary>
### This example algorithm defines its own custom coarse/fine fundamental selection model
### with equally weighted portfolio and a maximum sector exposure.
### </summary>
class SectorExposureRiskFrameworkAlgorithm(QCAlgorithmFramework):
    '''This example algorithm defines its own custom coarse/fine fundamental selection model
### with equally weighted portfolio and a maximum sector exposure.'''

    def Initialize(self):

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2014, 3, 24)
        self.SetEndDate(2014, 4, 7)
        self.SetCash(100000)

        # set algorithm framework models
        self.SetUniverseSelection(FineFundamentalUniverseSelectionModel(self.SelectCoarse, self.SelectFine))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1)))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetRiskManagement(MaximumSectorExposureRiskManagementModel())

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug(f"Order event: {orderEvent}. Holding value: {self.Securities[orderEvent.Symbol].Holdings.AbsoluteHoldingsValue}")

    def SelectCoarse(self, coarse):
        tickers = ["AAPL", "AIG", "IBM"] if self.Time.date() < date(2014, 4, 1) else [ "GOOG", "BAC", "SPY" ]
        return [Symbol.Create(x, SecurityType.Equity, Market.USA) for x in tickers]

    def SelectFine(self, fine):
        return [f.Symbol for f in fine]