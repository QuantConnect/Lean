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
from datetime import datetime, timedelta

### <summary>
### This regression algorithm tests option exercise and assignment functionality
### We open two positions and go with them into expiration. We expect to see our long position exercised and short position assigned.
### </summary>
### <meta name="tag" content="regression test" />
### <meta name="tag" content="options" />
class OptionSplitRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):

        # this test opens position in the first day of trading, lives through stock split (7 for 1),
        # and closes adjusted position on the second day

        self.SetCash(1000000)
        self.SetStartDate(2014,6,6)
        self.SetEndDate(2014,6,9)

        option = self.AddOption("AAPL")

        # set our strike/expiry filter for this option chain
        option.SetFilter(self.UniverseFunc)

        self.SetBenchmark("AAPL")
        self.contract = None

    def OnData(self, slice):

        if not self.Portfolio.Invested:
            if self.Time.hour > 9 and self.Time.minute > 0:
                for kvp in slice.OptionChains:
                    chain = kvp.Value
                    contracts = filter(lambda x: x.Strike == 650 and x.Right ==  OptionRight.Call, chain)
                    sorted_contracts = sorted(contracts, key = lambda x: x.Expiry)

                if len(sorted_contracts) > 1:
                    self.contract = sorted_contracts[1]
                    self.Buy(self.contract.Symbol, 1)

        elif self.Time.day > 6 and self.Time.hour > 14 and self.Time.minute > 0:
            self.Liquidate()

        if self.Portfolio.Invested:
            options_hold = [x for x in self.Portfolio.Securities if x.Value.Holdings.AbsoluteQuantity != 0]
            holdings = options_hold[0].Value.Holdings.AbsoluteQuantity
            if self.Time.day == 6 and holdings != 1:
                self.Log("Expected position quantity of 1 but was {0}".format(holdings))
            if self.Time.day == 9 and holdings != 7:
                self.Log("Expected position quantity of 7 but was {0}".format(holdings))

    # set our strike/expiry filter for this option chain
    def UniverseFunc(self, universe):
        return universe.IncludeWeeklys().Strikes(-2, 2).Expiration(timedelta(0), timedelta(365*2))

    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))