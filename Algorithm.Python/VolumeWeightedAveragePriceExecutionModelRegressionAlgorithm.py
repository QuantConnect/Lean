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
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Selection import *
from Alphas.RsiAlphaModel import RsiAlphaModel
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Execution.VolumeWeightedAveragePriceExecutionModel import VolumeWeightedAveragePriceExecutionModel
from datetime import timedelta

### <summary>
### Regression algorithm for the VolumeWeightedAveragePriceExecutionModel.
### This algorithm shows how the execution model works to split up orders and
### submit them only when the price is on the favorable side of the intraday VWAP.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class VolumeWeightedAveragePriceExecutionModelRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm for the VolumeWeightedAveragePriceExecutionModel.
    This algorithm shows how the execution model works to split up orders and
    submit them only when the price is on the favorable side of the intraday VWAP.'''

    def Initialize(self):

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

        # using hourly rsi to generate more insights
        self.SetAlpha(RsiAlphaModel(14, Resolution.Hour))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(VolumeWeightedAveragePriceExecutionModel())

        self.InsightsGenerated += self.OnInsightsGenerated

    def OnInsightsGenerated(self, algorithm, data):
        self.Log(f"{self.Time}: {', '.join(str(x) for x in data.Insights)}")

    def OnOrderEvent(self, orderEvent):
        self.Log(f"{self.Time}: {orderEvent}")