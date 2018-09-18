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
from QCAlgorithm import QCAlgorithm
from datetime import datetime, timedelta

### <summary>
### This regression algorithm tests option exercise and assignment functionality
### We open two positions and go with them into expiration. We expect to see our long position exercised and short position assigned.
### </summary>
### <meta name="tag" content="regression test" />
### <meta name="tag" content="options" />
class OptionExerciseAssignRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetCash(100000)
        self.SetStartDate(2015,12,24)
        self.SetEndDate(2015,12,24)

        option = self.AddOption("GOOG")

        # set our strike/expiry filter for this option chain
        option.SetFilter(self.UniverseFunc)

        self.SetBenchmark("GOOG")
        self._assignedOption = False

    def OnData(self, slice):
        if self.Portfolio.Invested: return
        for kvp in slice.OptionChains:
            chain = kvp.Value
            # find the call options expiring today
            contracts = filter(lambda x:
                               x.Expiry.date() == self.Time.date() and
                               x.Strike < chain.Underlying.Price and
                               x.Right ==  OptionRight.Call, chain)
            
            # sorted the contracts by their strikes, find the second strike under market price 
            sorted_contracts = sorted(contracts, key = lambda x: x.Strike, reverse = True)[:2]

            if sorted_contracts:
                self.MarketOrder(sorted_contracts[0].Symbol, 1)
                self.MarketOrder(sorted_contracts[1].Symbol, -1)

    # set our strike/expiry filter for this option chain
    def UniverseFunc(self, universe):
        return universe.IncludeWeeklys().Strikes(-2, 2).Expiration(timedelta(0), timedelta(10))

    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))

    def OnAssignmentOrderEvent(self, assignmentEvent):
        self.Log(str(assignmentEvent))
        self._assignedOption = True