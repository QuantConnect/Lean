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
from QuantConnect.Orders import OrderStatus
from QuantConnect.Orders.Fills import ImmediateFillModel
from QCAlgorithm import QCAlgorithm
import numpy as np
import decimal as d
import random

### <summary>
### Demonstration of using custom fee, slippage and fill models for modelling transactions in backtesting.
### QuantConnect allows you to model all orders as deeply and accurately as you need.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="transaction fees and slippage" />
### <meta name="tag" content="custom transaction models" />
### <meta name="tag" content="custom slippage models" />
### <meta name="tag" content="custom fee models" />
class CustomModelsAlgorithm(QCAlgorithm):
    '''Demonstration of using custom fee, slippage and fill models for modelling transactions in backtesting.
    QuantConnect allows you to model all orders as deeply and accurately as you need.'''

    def Initialize(self):
        self.SetStartDate(2013,10,1)   # Set Start Date
        self.SetEndDate(2013,10,31)    # Set End Date
        self.security = self.AddEquity("SPY", Resolution.Hour)
        self.spy = self.security.Symbol

        # set our models
        self.security.SetFeeModel(CustomFeeModel(self))
        self.security.SetFillModel(CustomFillModel(self))
        self.security.SetSlippageModel(CustomSlippageModel(self))

        
    def OnData(self, data):
        open_orders = self.Transactions.GetOpenOrders(self.spy)
        if len(open_orders) != 0: return

        if self.Time.day > 10 and self.security.Holdings.Quantity <= 0:
            quantity = self.CalculateOrderQuantity(self.spy, .5)
            self.Log("MarketOrder: " + str(quantity))
            self.MarketOrder(self.spy, quantity, True)   # async needed for partial fill market orders
        
        elif self.Time.day > 20 and self.security.Holdings.Quantity >= 0:
            quantity = self.CalculateOrderQuantity(self.spy, -.5)
            self.Log("MarketOrder: " + str(quantity))
            self.MarketOrder(self.spy, quantity, True)   # async needed for partial fill market orders

# If we want to use methods from other models, you need to inherit from one of them
class CustomFillModel(ImmediateFillModel):
    def __init__(self, algorithm):
        self.algorithm = algorithm
        self.base = ImmediateFillModel()
        self.absoluteRemainingByOrderId = {}
        random.seed(100)
    
    def MarketFill(self, asset, order):
        #if not _absoluteRemainingByOrderId.TryGetValue(order.Id, absoluteRemaining):
        absoluteRemaining = order.AbsoluteQuantity
        self.absoluteRemainingByOrderId[order.Id] = order.AbsoluteQuantity
        fill = self.base.MarketFill(asset, order)
        absoluteFillQuantity = int(min(absoluteRemaining, random.randint(0, 2*int(order.AbsoluteQuantity))))
        fill.FillQuantity = np.sign(order.Quantity) * absoluteFillQuantity
        if absoluteRemaining == absoluteFillQuantity:
            fill.Status = OrderStatus.Filled
            if self.absoluteRemainingByOrderId.get(order.Id):
                self.absoluteRemainingByOrderId.pop(order.Id)
        else:
            absoluteRemaining = absoluteRemaining - absoluteFillQuantity
            self.absoluteRemainingByOrderId[order.Id] = absoluteRemaining
            fill.Status = OrderStatus.PartiallyFilled
        self.algorithm.Log("CustomFillModel: " + str(fill))
        return fill

class CustomFeeModel:
    def __init__(self, algorithm):
        self.algorithm = algorithm

    def GetOrderFee(self, security, order):
        # custom fee math
        fee = max(1, security.Price * order.AbsoluteQuantity * d.Decimal(0.00001))
        self.algorithm.Log("CustomFeeModel: " + str(fee))
        return fee

class CustomSlippageModel:
    def __init__(self, algorithm):
        self.algorithm = algorithm
        
    def GetSlippageApproximation(self, asset, order):
        # custom slippage math
        slippage = asset.Price * d.Decimal(0.0001 * np.log10(2*float(order.AbsoluteQuantity)))
        self.algorithm.Log("CustomSlippageModel: " + str(slippage))
        return slippage