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
from Selection.ManualUniverseSelectionModel import ManualUniverseSelectionModel
from datetime import timedelta

### <summary>
### Framework algorithm that uses the G10CurrencySelectionModel,
### a Universe Selection Model that inherits from ManualUniverseSelectionModel
### </summary>
class G10CurrencySelectionModelFrameworkAlgorithm(QCAlgorithm):
    '''Framework algorithm that uses the G10CurrencySelectionModel,
    a Universe Selection Model that inherits from ManualUniverseSelectionMode'''

    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        # set algorithm framework models
        self.SetUniverseSelection(self.G10CurrencySelectionModel())
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(minutes = 20), 0.025, None))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(MaximumDrawdownPercentPerSecurity(0.01))

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug("Purchased Stock: {0}".format(orderEvent.Symbol))

    class G10CurrencySelectionModel(ManualUniverseSelectionModel):
        '''Provides an implementation of IUniverseSelectionModel that simply subscribes to G10 currencies'''
        def __init__(self):
            '''Initializes a new instance of the G10CurrencySelectionModel class
            using the algorithm's security initializer and universe settings'''
            super().__init__([Symbol.Create(x, SecurityType.Forex, Market.Oanda)
                             for x in [ "EURUSD",
                                        "GBPUSD",
                                        "USDJPY",
                                        "AUDUSD",
                                        "NZDUSD",
                                        "USDCAD",
                                        "USDCHF",
                                        "USDNOK",
                                        "USDSEK" ]])