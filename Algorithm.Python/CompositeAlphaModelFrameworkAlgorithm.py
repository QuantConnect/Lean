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
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import *
from Alphas.RsiAlphaModel import RsiAlphaModel
from Alphas.EmaCrossAlphaModel import EmaCrossAlphaModel
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from Risk.NullRiskManagementModel import NullRiskManagementModel
from datetime import timedelta
import numpy as np

### <summary>
### Show cases how to use the CompositeAlphaModel to define.
### </summary>
class CompositeAlphaModelFrameworkAlgorithm(QCAlgorithmFramework):
    '''Show cases how to use the CompositeAlphaModel to define.'''

    def Initialize(self):

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        
        # even though we're using a framework algorithm, we can still add our securities
        # using the AddEquity/Forex/Crypto/ect methods and then pass them into a manual
        # universe selection model using Securities.Keys
        self.AddEquity("SPY")
        self.AddEquity("IBM")
        self.AddEquity("BAC")
        self.AddEquity("AIG")

        # define a manual universe of all the securities we manually registered
        self.SetUniverseSelection(ManualUniverseSelectionModel(self.Securities.Keys))

        # define alpha model as a composite of the rsi and ema cross models
        self.SetAlpha(CompositeAlphaModel(RsiAlphaModel(), EmaCrossAlphaModel()))

        # default models for the rest
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())