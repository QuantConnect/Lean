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
from datetime import timedelta

### <summary>
### This example demonstrates how to add options for a given underlying equity security.
### It also shows how you can prefilter contracts easily based on strikes and expirations.
### It also shows how you can inspect the option chain to pick a specific option contract to trade.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
class BasicTemplateOptionsFilterUniverseAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 12, 16)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(100000)

        option = self.AddOption("GOOG")
        self.option_symbol = option.Symbol

        # set our strike/expiry filter for this option chain
        option.SetFilter(-10, 10, timedelta(0), timedelta(10))

        # use the underlying equity as the benchmark
        self.SetBenchmark("GOOG")

    def OnData(self,slice):
        if self.Portfolio.Invested: return

        for kvp in slice.OptionChains:
            if kvp.Key != self.option_symbol: continue
            chain = kvp.Value
            # find the call options expiring today
            contracts = [i for i in chain if i.Right ==  OptionRight.Call and i.Expiry.date() == self.Time.date()]
            # sorted the contracts by their strike, find the second strike under market price 
            sorted_contracts = [i for i in sorted(contracts, key = lambda x:x.Strike, reverse = True) if i.Strike < chain.Underlying.Price]
            # if found, trade it
            if len(sorted_contracts) == 0: 
                self.Log("No call contracts expiring today")
                return
            self.MarketOrder(sorted_contracts[1].Symbol, 1)

    def OnOrderEvent(self, orderEvent):
        # Order fill event handler. On an order fill update the resulting information is passed to this method.
        # <param name="orderEvent">Order event details containing details of the evemts</param>
        self.Log(str(orderEvent))