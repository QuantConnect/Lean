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
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Selection import *

# we load the python version of these models:
from Alphas.ConstantAlphaModel import *
from Execution.ImmediateExecutionModel import *
from Portfolio.EqualWeightingPortfolioConstructionModel import *

from datetime import timedelta

### <summary>
### Basic template framework algorithm uses framework components to define the algorithm.
### </summary>
class PortfolioRebalanceOnCustomFuncRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2015, 1, 5)
        self.SetEndDate(2017, 1, 1)

        self.Settings.RebalancePortfolioOnInsightChanges = False;

        self.SetUniverseSelection(CustomUniverseSelectionModel("CustomUniverseSelectionModel", lambda time: [ "AAPL", "IBM", "FB", "SPY" ]))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, None));
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel(self.RebalanceFunction))
        self.SetExecution(ImmediateExecutionModel())
        self.lastRebalanceTime = self.StartDate

    def RebalanceFunction(self, time):
        self.lastRebalanceTime = time
        if self.Portfolio.MarginRemaining > 60000 or self.Portfolio.MarginRemaining < 40000:
            return time
        return None

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Submitted:
            if self.UtcTime != self.lastRebalanceTime:
                raise ValueError(f"{self.UtcTime} {orderEvent.Symbol}")
