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

class IndexOptionCallButterflyAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 1, 1)
        self.SetEndDate(2021, 1, 1)
        self.SetCash(1000000)

        self.vxz = self.AddEquity("VXZ", Resolution.Minute).Symbol

        index = self.AddIndex("SPX", Resolution.Minute).Symbol
        option = self.AddIndexOption(index, "SPXW", Resolution.Minute)
        option.SetFilter(lambda x: x.IncludeWeeklys().Strikes(-3, 3).Expiration(15, 45))

        self.spxw = option.Symbol
        self.multiplier = option.SymbolProperties.ContractMultiplier
        self.tickets = []

    def OnData(self, slice: Slice) -> None:
        # The order of magnitude per SPXW order's value is 10000 times of VXZ
        if not self.Portfolio[self.vxz].Invested:
            self.MarketOrder(self.vxz, 10000)
        
        # Return if any opening index option position
        if any([self.Portfolio[x.Symbol].Invested for x in self.tickets]): return

        # Get the OptionChain
        chain = slice.OptionChains.get(self.spxw)
        if not chain: return

        # Get nearest expiry date
        expiry = min([x.Expiry for x in chain])
        
        # Select the call Option contracts with nearest expiry and sort by strike price
        calls = [x for x in chain if x.Expiry == expiry and x.Right == OptionRight.Call]
        if len(calls) < 3: return
        sorted_call_strikes = sorted([x.Strike for x in calls])

        # Select ATM call
        atm_strike = min([abs(x - chain.Underlying.Value) for x in sorted_call_strikes])

        # Get the strike prices for the ITM & OTM contracts, make sure they're in equidistance
        spread = min(atm_strike - sorted_call_strikes[0], sorted_call_strikes[-1] - atm_strike)
        otm_strike = atm_strike - spread
        itm_strike = atm_strike + spread
        if otm_strike not in sorted_call_strikes or itm_strike not in sorted_call_strikes: return
        
        # Buy the call butterfly
        call_butterfly = OptionStrategies.CallButterfly(self.spxw, itm_strike, atm_strike, otm_strike, expiry)
        price = sum([abs(self.Securities[x.Symbol].Price * x.Quantity) * self.multiplier for x in call_butterfly.UnderlyingLegs])
        if price > 0:
            quantity = self.Portfolio.TotalPortfolioValue // price
            self.tickets = self.Buy(call_butterfly, quantity, asynchronous=True)
        