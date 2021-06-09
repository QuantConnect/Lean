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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
import numpy as np

class SpreadExecutionModel(ExecutionModel):
    '''Execution model that submits orders while the current pread is tight.'''

    def __init__(self, acceptingSpreadPercent=0.005):
        '''Initializes a new instance of the SpreadExecutionModel class'''
        self.targetsCollection = PortfolioTargetCollection()
        
        # Gets or sets the maximum spread compare to current price in percentage.
        self.acceptingSpreadPercent = acceptingSpreadPercent

    def Execute(self, algorithm, targets):
        '''Executes market orders if the spread percentage to price is in desirable range.
       Args:
           algorithm: The algorithm instance
           targets: The portfolio targets'''
           
        # update the complete set of portfolio targets with the new targets
        self.targetsCollection.AddRange(targets)

        # for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
        if self.targetsCollection.Count > 0:
            for target in self.targetsCollection.OrderByMarginImpact(algorithm):
                symbol = target.Symbol
                
                # calculate remaining quantity to be ordered
                unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target)
                
                # get security information
                security = algorithm.Securities[symbol]
                
                # check order entry conditions
                if unorderedQuantity != 0 and self.SpreadIsFavorable(security):
                    algorithm.MarketOrder(symbol, unorderedQuantity)

            self.targetsCollection.ClearFulfilled(algorithm)
            
    def SpreadIsFavorable(self, security):
        '''Determines if the spread is in desirable range.'''
        # Price has to be larger than zero to avoid zero division error, or negative price causing the spread percentage < 0 by error
        # Has to be in opening hours of exchange to avoid extreme spread in OTC period
        if security.Exchange.ExchangeOpen and security.Price > 0 and (security.AskPrice - security.BidPrice) / security.Price <= self.acceptingSpreadPercent:
            return True
            
        return False
        
