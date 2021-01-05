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
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *
from datetime import timedelta

### <summary>
### Basic template algorithm which showcases ConstituentsUniverse simple use case
### </summary>
class BasicTemplateConstituentUniverseAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        
        # by default will use algorithms UniverseSettings
        self.AddUniverse(self.Universe.Constituent.Steel())

        # we specify the UniverseSettings it should use
        self.AddUniverse(self.Universe.Constituent.AggressiveGrowth(
            UniverseSettings(Resolution.Hour,
                2,
                False,
                False,
                self.UniverseSettings.MinimumTimeInUniverse)))

        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)))
        self.SetExecution(ImmediateExecutionModel())
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())