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

class IndexOptionPutButterflyAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 1, 1)
        self.SetEndDate(2021, 1, 1)
        self.SetCash(1000000)

        self.vxz = self.AddEquity("VXZ", Resolution.Minute).Symbol

        index = self.AddIndex("SPX", Resolution.Minute).Symbol
        option = self.AddIndexOption(index, "SPXW", Resolution.Minute)
        option.SetFilter(lambda x: x.IncludeWeeklys().Strikes(-3, 3).Expiration(15, 45))

        self.option = option.Symbol
        self.multiplier = option.SymbolProperties.ContractMultiplier

    def OnData(self, slice: Slice) -> None:
        # The order of magnitude per SPXW order's value is 10000 times of VXZ
        if not self.Portfolio[self.vxz].Invested:
            self.MarketOrder(self.vxz, 10000)
        
        # Return if there is any opening index option position
        if any([x.Type == SecurityType.IndexOption and x.Invested for x in self.Portfolio.Values]): return

        # Get the OptionChain
        chain = slice.OptionChains.get(self.option)
        if not chain: return

        # Get nearest expiry date
        expiry = min([x.Expiry for x in chain])
        
        # Select the put Option contracts with nearest expiry and sort by strike price
        puts = [x for x in chain if x.Expiry == expiry and x.Right == OptionRight.Put]
        if len(puts) < 3: return
        sorted_puts = sorted(puts, key=lambda x: x.Strike)

        # Select ATM put
        atm_put = sorted(puts, key=lambda x: abs(x.Strike - chain.Underlying.Value))[0]

        # Create combo order legs
        legs = [
            Leg.Create(sorted_puts[0].Symbol, -1),
            Leg.Create(sorted_puts[-1].Symbol, -1),
            Leg.Create(atm_put.Symbol, 2)
        ]
        price = sum([abs(self.Securities[x.Symbol].Price * x.Quantity) * self.multiplier for x in legs])
        if price > 0:
            quantity = self.Portfolio.TotalPortfolioValue // price
            self.ComboMarketOrder(legs, -quantity, asynchronous=True)