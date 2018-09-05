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
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Selection import *
from Alphas.RsiAlphaModel import RsiAlphaModel
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Execution.StandardDeviationExecutionModel import StandardDeviationExecutionModel
from datetime import timedelta

### <summary>
### Regression algorithm for the StandardDeviationExecutionModel.
### This algorithm shows how the execution model works to split up orders and submit them
### only when the price is 2 standard deviations from the 60min mean (default model settings).
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class StandardDeviationExecutionModelRegressionAlgorithm(QCAlgorithmFramework):
    '''Regression algorithm for the StandardDeviationExecutionModel.
    This algorithm shows how the execution model works to split up orders and submit them
    only when the price is 2 standard deviations from the 60min mean (default model settings).'''

    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetCash(1000000)

        self.SetUniverseSelection(ManualUniverseSelectionModel([
            Symbol.Create('AIG', SecurityType.Equity, Market.USA),
            Symbol.Create('BAC', SecurityType.Equity, Market.USA),
            Symbol.Create('IBM', SecurityType.Equity, Market.USA),
            Symbol.Create('SPY', SecurityType.Equity, Market.USA)
        ]))

        self.SetAlpha(RsiAlphaModel(14, Resolution.Hour))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(StandardDeviationExecutionModel())

    def OnOrderEvent(self, orderEvent):
        self.Log(f"{self.Time}: {orderEvent}")