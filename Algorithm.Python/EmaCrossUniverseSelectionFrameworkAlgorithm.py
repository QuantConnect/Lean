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
from Alphas.ConstantAlphaModel import ConstantAlphaModel
from Selection.EmaCrossUniverseSelectionModel import EmaCrossUniverseSelectionModel
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from datetime import timedelta

### <summary>
### Framework algorithm that uses the EmaCrossUniverseSelectionModel to
### select the universe based on a moving average cross.
### </summary>
class EmaCrossUniverseSelectionFrameworkAlgorithm(QCAlgorithm):
    '''Framework algorithm that uses the EmaCrossUniverseSelectionModel to select the universe based on a moving average cross.'''

    def Initialize(self):

        self.SetStartDate(2013,1,1)
        self.SetEndDate(2015,1,1)
        self.SetCash(100000)

        fastPeriod = 100
        slowPeriod = 300
        count = 10

        self.UniverseSettings.Leverage = 2.0
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetUniverseSelection(EmaCrossUniverseSelectionModel(fastPeriod, slowPeriod, count))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1), None, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())