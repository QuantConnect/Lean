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

class IndexOptionCallCalendarSpreadAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 1, 1)
        self.SetEndDate(2023, 1, 1)
        self.SetCash(50000)
        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin)

        self.vxz = self.AddEquity("VXZ", Resolution.Minute).Symbol
        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol

        index = self.AddIndex("VIX", Resolution.Minute).Symbol
        option = self.AddIndexOption(index, "VIXW", Resolution.Minute)
        option.SetFilter(lambda x: x.Strikes(-2, 2).Expiration(15, 45))
        
        self.symbol = option.Symbol
        self.multiplier = option.SymbolProperties.ContractMultiplier
        self.expiry = datetime.max

    def OnData(self, slice: Slice) -> None:
        # Liquidate if the shorter term option is about to expire
        if self.expiry < self.Time + timedelta(2):
            self.Liquidate()
        # Return if there is any opening index option position
        elif [x for x in self.Portfolio.Values if x.Type == SecurityType.IndexOption and x.Invested]:
            return

        # Get the OptionChain
        chain = slice.OptionChains.get(self.symbol)
        if not chain: return

        # Get ATM strike price
        strike = sorted(chain, key = lambda x: abs(x.Strike - chain.Underlying.Value))[0].Strike
        
        # Select the ATM call Option contracts and sort by expiration date
        calls = sorted([i for i in chain if i.Strike == strike and i.Right == OptionRight.Call], 
                        key=lambda x: x.Expiry)
        if len(calls) < 2: return
        self.expiry = calls[0].Expiry

        # Create combo order legs
        legs = [
            Leg.Create(calls[0].Symbol, -1),
            Leg.Create(calls[-1].Symbol, 1),
            Leg.Create(self.vxz, -100),
            Leg.Create(self.spy, -10)
        ]
        qty = self.Portfolio.TotalPortfolioValue // \
            sum([abs(self.Securities[x.Symbol].Price * x.Quantity) * self.multiplier if x.Symbol.ID.SecurityType == SecurityType.IndexOption
                 else abs(self.Securities[x.Symbol].Price * x.Quantity)
                 for x in legs])
        self.ComboMarketOrder(legs, -qty, asynchronous=True)