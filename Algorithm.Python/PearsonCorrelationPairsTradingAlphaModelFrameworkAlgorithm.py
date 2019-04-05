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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *


### <summary>
### Framework algorithm that uses the PearsonCorrelationPairsTradingAlphaModel.
### This model extendes BasePairsTradingAlphaModel and uses Pearson correlation
### to rank the pairs trading candidates and use the best candidate to trade.
### </summary>
class PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm(QCAlgorithm):
    '''Framework algorithm that uses the PearsonCorrelationPairsTradingAlphaModel.
    This model extendes BasePairsTradingAlphaModel and uses Pearson correlation
    to rank the pairs trading candidates and use the best candidate to trade.'''

    def Initialize(self):

        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)

        self.SetUniverseSelection(ManualUniverseSelectionModel(
            Symbol.Create('AIG', SecurityType.Equity, Market.USA),
            Symbol.Create('BAC', SecurityType.Equity, Market.USA),
            Symbol.Create('IBM', SecurityType.Equity, Market.USA),
            Symbol.Create('SPY', SecurityType.Equity, Market.USA)))

        self.SetAlpha(PearsonCorrelationPairsTradingAlphaModel(252, Resolution.Daily))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())