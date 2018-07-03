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
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Alphas.PairsTradingAlphaModel import PairsTradingAlphaModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from Risk.NullRiskManagementModel import NullRiskManagementModel

### <summary>
### Framework algorithm that uses the PairsTradingAlphaModel to detect
### divergences between correllated assets. Detection of asset correlation is not
### performed and is expected to be handled outside of the alpha model.
### </summary>
class PairsTradingAlphaModelFrameworkAlgorithm(QCAlgorithmFramework):
    '''Framework algorithm that uses the PairsTradingAlphaModel to detect
    divergences between correllated assets. Detection of asset correlation is not
    performed and is expected to be handled outside of the alpha model.'''

    def Initialize(self):

        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)

        bac = self.AddEquity("BAC")
        aig = self.AddEquity("AIG")

        self.SetUniverseSelection(ManualUniverseSelectionModel(self.Securities.Keys))
        self.SetAlpha(PairsTradingAlphaModel(bac.Symbol, aig.Symbol))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())