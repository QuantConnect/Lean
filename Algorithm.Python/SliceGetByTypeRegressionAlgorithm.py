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
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Algorithm.Framework.Alphas import *
from datetime import timedelta

TradeFlag = False

### <summary>
### Regression algorithm asserting slice.Get() works for both the alpha and the algorithm
### </summary>
class SliceGetByTypeRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)

        self.AddEquity("SPY", Resolution.Minute)
        self.SetAlpha(TestAlphaModel())

    def OnData(self, data):
        if "SPY" in data:
            tb = data.Get(TradeBar)["SPY"]
            global TradeFlag
            if not self.Portfolio.Invested and TradeFlag:
                self.SetHoldings("SPY", 1)

class TestAlphaModel(AlphaModel):
    def Update(self, algorithm, data):
        insights = []

        if "SPY" in data:
            tb = data.Get(TradeBar)["SPY"]
            global TradeFlag
            TradeFlag = True

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        return