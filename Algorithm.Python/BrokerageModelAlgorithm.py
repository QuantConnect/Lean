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
from QuantConnect.Brokerages import *
from QuantConnect.Orders import *
from QCAlgorithm import QCAlgorithm

### <summary>
### Demonstrate the usage of the BrokerageModel property to help improve backtesting
### accuracy through simulation of a specific brokerage's rules around restrictions
### on submitting orders as well as fee structure.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="brokerage models" />
class BrokerageModelAlgorithm(QCAlgorithm):
    def Initialize(self):

        self.SetCash(100000)            # Set Strategy Cash
        self.SetStartDate(2013,10,7)    # Set Start Date
        self.SetEndDate(2013,10,11)     # Set End Date
        self.AddEquity("SPY", Resolution.Second)
        
        # there's two ways to set your brokerage model. The easiest would be to call
        # SetBrokerageModel( BrokerageName ); // BrokerageName is an enum
        # SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
        # SetBrokerageModel(BrokerageName.Default);

        # the other way is to call SetBrokerageModel( IBrokerageModel ) with your
        # own custom model. I've defined a simple extension to the default brokerage
        # model to take into account a requirement to maintain 500 cash in the account at all times
        self.SetBrokerageModel(MinimumAccountBalanceBrokerageModel(self,500.00))
        self.last = 1   
        
    def OnData(self, slice):
        # Simple buy and hold template
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", self.last)
            if self.Portfolio["SPY"].Quantity == 0:
                # each time we fail to purchase we'll decrease our set holdings percentage
                self.Debug(str(self.Time) + " - Failed to purchase stock")
                self.last *= 0.95
            else:
                self.Debug("{} - Purchased Stock @ SetHoldings( {} )".format(self.Time, self.last))


class MinimumAccountBalanceBrokerageModel(DefaultBrokerageModel):
    '''Custom brokerage model that requires clients to maintain a minimum cash balance'''
    def __init__(self, algorithm, minimumAccountBalance):
        self.algorithm = algorithm
        self.minimumAccountBalance = minimumAccountBalance
    
    def CanSubmitOrder(self,security, order, message):
        '''Prevent orders which would bring the account below a minimum cash balance'''
        message = None
        # we want to model brokerage requirement of minimumAccountBalance cash value in account
        orderCost = order.GetValue(security)
        cash = self.algorithm.Portfolio.Cash
        cashAfterOrder = cash - orderCost
        if cashAfterOrder < self.minimumAccountBalance:
            # return a message describing why we're not allowing this order
            message = BrokerageMessageEvent(BrokerageMessageType.Warning, "InsufficientRemainingCapital", "Account must maintain a minimum of ${0} USD at all times. Order ID: {1}".format(self.minimumAccountBalance, order.Id))
            self.algorithm.Error(str(message))
            return False
        return True