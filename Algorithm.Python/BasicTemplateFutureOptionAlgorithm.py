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
### The demonstration algorithm shows some of the most common order methods when working with FutureOption assets.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />

class BasicTemplateFutureOptionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.SetStartDate(2022, 1, 1)
        self.SetEndDate(2022, 2, 1)
        self.SetCash(100000)

        gold_futures = self.AddFuture(Futures.Metals.Gold, Resolution.Minute)
        gold_futures.SetFilter(0, 180)
        self.symbol = gold_futures.Symbol
        self.AddFutureOption(self.symbol, lambda universe: universe.Strikes(-5, +5)
                                                                    .CallsOnly()
                                                                    .BackMonth()
                                                                    .OnlyApplyFilterAtMarketOpen())

        # Historical Data
        history = self.History(self.symbol, 60, Resolution.Daily)
        self.Log(f"Received {len(history)} bars from {self.symbol} FutureOption historical data call.")

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        Arguments:
            slice: Slice object keyed by symbol containing the stock data
        '''
        # Access Data
        for kvp in data.OptionChains:
            underlying_future_contract = kvp.Key.Underlying
            chain = kvp.Value

            if not chain: continue

            for contract in chain:
                self.Log(f"""Canonical Symbol: {kvp.Key}; 
                    Contract: {contract}; 
                    Right: {contract.Right}; 
                    Expiry: {contract.Expiry}; 
                    Bid price: {contract.BidPrice}; 
                    Ask price: {contract.AskPrice}; 
                    Implied Volatility: {contract.ImpliedVolatility}""")

            if not self.Portfolio.Invested:
                atm_strike = sorted(chain, key = lambda x: abs(chain.Underlying.Price - x.Strike))[0].Strike
                selected_contract = sorted([contract for contract in chain if contract.Strike == atm_strike], \
                           key = lambda x: x.Expiry, reverse=True)[0]
                self.MarketOrder(selected_contract.Symbol, 1)

    def OnOrderEvent(self, orderEvent):
        self.Debug("{} {}".format(self.Time, orderEvent.ToString()))