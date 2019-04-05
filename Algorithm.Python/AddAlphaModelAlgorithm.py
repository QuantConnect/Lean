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

### <summary>
### Test algorithm using 'QCAlgorithm.AddAlphaModel()'
### </summary>
class AddAlphaModelAlgorithm(QCAlgorithm):
    def Initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.UniverseSettings.Resolution = Resolution.Daily;

        spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA)
        fb = Symbol.Create("FB", SecurityType.Equity, Market.USA)
        ibm = Symbol.Create("IBM", SecurityType.Equity, Market.USA)

        # set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel([ spy, fb, ibm ]))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())

        self.AddAlpha(OneTimeAlphaModel(spy))
        self.AddAlpha(OneTimeAlphaModel(fb))
        self.AddAlpha(OneTimeAlphaModel(ibm))

class OneTimeAlphaModel(AlphaModel):
    def __init__(self, symbol):
        self.symbol = symbol
        self.triggered = False

    def Update(self, algorithm, data):
        insights = []
        if not self.triggered:
            self.triggered = True;
            insights.append(Insight.Price(
                self.symbol,
                Resolution.Daily,
                1,
                InsightDirection.Down))
        return insights