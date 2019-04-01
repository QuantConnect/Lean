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
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Selection import *
from Risk.TrailingStopRiskManagementModel import TrailingStopRiskManagementModel
from datetime import timedelta


class TrailingStopRiskFrameworkAlgorithm(QCAlgorithm):
    '''Show example of how to use the TrailingStopRiskManagementModel'''

    def Initialize(self):

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        # set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel([ Symbol.Create("SPY", SecurityType.Equity, Market.USA) ]))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(minutes = 20), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(TrailingStopRiskManagementModel(0.01))

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug(f'Processed Order: {orderEvent.Symbol}, Quantity: {orderEvent.FillQuantity}')