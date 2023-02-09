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

#region imports
from AlgorithmImports import *
#endregion

class IndexOptionBullCallSpreadAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 1, 1)
        self.SetEndDate(2021, 1, 1)
        self.SetCash(100000)

        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol

        index = self.AddIndex("SPX", Resolution.Minute).Symbol
        option = self.AddIndexOption(index, "SPXW", Resolution.Minute)
        option.SetFilter(lambda x: x.WeeklysOnly().Strikes(-5, 5).Expiration(40, 60))
        
        self.spxw = option.Symbol
        self.tickets = []

    def OnData(self, slice: Slice) -> None:
        if not self.Portfolio[self.spy].Invested:
            self.MarketOrder(self.spy, 100)
        
        # Return if hedge position presents
        if any([self.Portfolio[x.Symbol].Invested for x in self.tickets]):
            return

        # Return if hedge position presents
        chain = slice.OptionChains.get(self.spxw)
        if not chain: return

        # Get the nearest expiry date of the contracts
        expiry = min([x.Expiry for x in chain])
        
        # Select the call Option contracts with the nearest expiry and sort by strike price
        calls = sorted([i for i in chain if i.Expiry == expiry and i.Right == OptionRight.Call], 
                        key=lambda x: x.Strike)
        if len(calls) < 2: return

        # Buy the bull call spread
        bull_call_spread = OptionStrategies.BullCallSpread(self.spxw, calls[0].Strike, calls[-1].Strike, expiry)
        self.tickets = self.Buy(bull_call_spread, 1)