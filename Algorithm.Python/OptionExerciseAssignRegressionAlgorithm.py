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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from datetime import datetime

### <summary>
### This regression algorithm tests option exercise and assignment functionality
### We open two positions and go with them into expiration. We expect to see our long position exercised and short position assigned.
### </summary>
### <meta name="tag" content="regression test" />
### <meta name="tag" content="options" />
class OptionExerciseAssignRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        
        self.SetCash(25000)
        self.SetStartDate(2015,12,24)
        self.SetEndDate(2015,12,24)
        equity = self.AddEquity("GOOG")
        option = self.AddOption("GOOG")
        Underlying = equity.Symbol
        self.OptionSymbol = option.Symbol
        # set our strike/expiry filter for this option chain
        option.SetFilter(-2, 2, TimeSpan.Zero, TimeSpan.FromDays(10))
        self.SetBenchmark(Underlying)
        self._assignedOption = False
    

    ''' Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override 
        for receiving all subscription data in a single event 
        <param name="slice">The current slice of data keyed by symbol string</param> '''

    def OnData(self, slice):
        if self.Portfolio.Invested: return
        for kvp in slice.OptionChains:
            chain = kvp.Value
            # find the call options expiring today
            contracts = [i for i in chain if i.Right ==  OptionRight.Call and 
                                            i.Expiry.date() == self.Time.date()]

            # sorted the contracts by their strikes, find the second strike under market price 
            sorted_contracts = [i for i in sorted(contracts, key = lambda x:x.Strike, reverse = True) 
                                        if i.Strike < chain.Underlying.Price]

            if sorted_contracts:
                self.MarketOrder(sorted_contracts[0].Symbol, 1)
                self.MarketOrder(sorted_contracts[1].Symbol, -1)
                

    ''' Order fill event handler. On an order fill update the resulting information is passed to this method.
        <param name="orderEvent">Order event details containing details of the events</param> '''
    
    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))
    def OnAssignmentOrderEvent(self, assignmentEvent):
        self.Log(str(assignmentEvent))
        self._assignedOption = True