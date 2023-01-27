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

class IndexOptionIronCondorAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 9, 1)
        self.SetEndDate(2019, 11, 1)
        self.SetCash(100000)

        index = self.AddIndex("SPX", Resolution.Minute).Symbol
        option = self.AddIndexOption(index, "SPXW", Resolution.Minute)
        option.SetFilter(lambda x: x.WeeklysOnly().Strikes(-5, 5).Expiration(0, 14))
        self.symbol = option.Symbol

        self.bb = self.BB(index, 10, 2, resolution=Resolution.Daily)
        self.WarmUpIndicator(index, self.bb)
        
    def OnData(self, slice: Slice) -> None:
        if self.Portfolio.Invested: return

        # Get the OptionChain
        chain = slice.OptionChains.get(self.symbol)
        if not chain: return

        # Get the closest expiry date
        expiry = min([x.Expiry for x in chain])
        chain = [x for x in chain if x.Expiry == expiry]

        # Separate the call and put contracts and sort by Strike to find OTM contracts
        calls = sorted([x for x in chain if x.Right == OptionRight.Call], key=lambda x: x.Strike, reverse=True)
        puts = sorted([x for x in chain if x.Right == OptionRight.Put], key=lambda x: x.Strike)
        if len(calls) < 3 or len(puts) < 3: return

        # Create combo order legs
        price = self.bb.Price.Current.Value
        quantity = 1
        if price > self.bb.UpperBand.Current.Value or price < self.bb.LowerBand.Current.Value:
            quantity = -1

        legs = [
            Leg.Create(calls[0].Symbol, quantity),
            Leg.Create(puts[0].Symbol, quantity),
            Leg.Create(calls[2].Symbol, -quantity),
            Leg.Create(puts[2].Symbol, -quantity)
        ]

        self.ComboMarketOrder(legs, 10, asynchronous=True)