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

from AlgorithmImports import *

### <summary>
### This regression algorithm verifies automatic option contract assignment behavior.
### </summary>
class OptionAssignmentRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.cnt = 0
        self.SetStartDate(2015, 12, 23)
        self.SetEndDate(2015, 12, 28)
        self.SetCash(100000)
        self.stock = self.AddEquity("GOOG", Resolution.Minute)
        
        contracts = list(self.OptionChain(self.stock.Symbol))
        
        self.put_option_symbol = sorted(
            [c for c in contracts if c.ID.OptionRight == OptionRight.Put and c.ID.StrikePrice == 800],
            key=lambda c: c.ID.Date
        )[0]

        self.call_option_symbol = sorted(
            [c for c in contracts if c.ID.OptionRight == OptionRight.Call and c.ID.StrikePrice == 600],
            key=lambda c: c.ID.Date
        )[0]
        
        self.put_option = self.AddOptionContract(self.put_option_symbol)
        self.call_option = self.AddOptionContract(self.call_option_symbol)

    def on_data(self, data):
        if not self.Portfolio.Invested and self.stock.Price != 0 and self.put_option.Price != 0 and self.call_option.Price != 0:
            #this gets executed on start and after each auto-assignment, finally ending with expiration assignment
            if self.time < self.put_option_symbol.ID.Date:
                self.MarketOrder(self.put_option_symbol, -1)
            
            if self.time < self.call_option_symbol.ID.Date:
                self.MarketOrder(self.call_option_symbol, -1)