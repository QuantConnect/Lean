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
from QuantConnect.Securities import *
import numpy as np

### <summary>
### Demonstration of using custom buying power model in backtesting.
### QuantConnect allows you to model all orders as deeply and accurately as you need.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="transaction fees and slippage" />
### <meta name="tag" content="custom buying power models" />
class CustomBuyingPowerModelAlgorithm(QCAlgorithm):
    '''Demonstration of using custom buying power model in backtesting.
    QuantConnect allows you to model all orders as deeply and accurately as you need.'''

    def Initialize(self):
        self.SetStartDate(2013,10,1)   # Set Start Date
        self.SetEndDate(2013,10,31)    # Set End Date
        security = self.AddEquity("SPY", Resolution.Hour)
        self.spy = security.Symbol

        # set the buying power model
        security.SetBuyingPowerModel(CustomBuyingPowerModel())

    def OnData(self, slice):
        if self.Portfolio.Invested:
            return

        quantity = self.CalculateOrderQuantity(self.spy, 1)
        if quantity % 100 != 0:
            raise Exception(f'CustomBuyingPowerModel only allow quantity that is multiple of 100 and {quantity} was found')

        # We normally get insufficient buying power model, but the
        # CustomBuyingPowerModel always says that there is sufficient buying power for the orders
        self.MarketOrder(self.spy, quantity * 10)


class CustomBuyingPowerModel(BuyingPowerModel):
    def GetMaximumOrderQuantityForTargetBuyingPower(self, parameters):
        quantity = super().GetMaximumOrderQuantityForTargetBuyingPower(parameters).Quantity
        quantity = np.floor(quantity / 100) * 100
        return GetMaximumOrderQuantityResult(quantity)

    def HasSufficientBuyingPowerForOrder(self, parameters):
        return HasSufficientBuyingPowerForOrderResult(True)