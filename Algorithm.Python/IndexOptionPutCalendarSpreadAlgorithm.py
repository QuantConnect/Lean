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

class IndexOptionPutCalendarSpreadAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 1, 1)
        self.SetEndDate(2023, 1, 1)
        self.SetCash(50000)

        self.AddEquity("VXZ", Resolution.Minute)

        index = self.AddIndex("VIX", Resolution.Minute).Symbol
        option = self.AddIndexOption(index, "VIXW", Resolution.Minute)
        option.SetFilter(lambda x: x.Strikes(-2, 2).Expiration(15, 45))
        self.symbol = option.Symbol

        self.expiry = datetime.max

    def OnData(self, slice: Slice) -> None:
        if not self.Portfolio["VXZ"].Invested:
            self.MarketOrder("VXZ", 100)
        
        index_options_invested = [x for x in self.Portfolio.Values
                                  if x.Type == SecurityType.IndexOption and x.Invested]
        # Liquidate if the shorter term option is about to expire
        if self.expiry < self.Time + timedelta(2) and all([slice.ContainsKey(x.Symbol) for x in self.legs]):
            for holding in index_options_invested:
                self.Liquidate(holding.Symbol)
        # Return if there is any opening index option position
        elif index_options_invested:
            return

        # Get the OptionChain
        chain = slice.OptionChains.get(self.symbol)
        if not chain: return

        # Get ATM strike price
        strike = sorted(chain, key = lambda x: abs(x.Strike - chain.Underlying.Value))[0].Strike
        
        # Select the ATM put Option contracts and sort by expiration date
        puts = sorted([i for i in chain if i.Strike == strike and i.Right == OptionRight.Put], 
                        key=lambda x: x.Expiry)
        if len(puts) < 2: return
        self.expiry = puts[0].Expiry

        # Create combo order legs
        self.legs = [
            Leg.Create(puts[0].Symbol, -1),
            Leg.Create(puts[-1].Symbol, 1)
        ]
        self.ComboMarketOrder(self.legs, -1, asynchronous=True)