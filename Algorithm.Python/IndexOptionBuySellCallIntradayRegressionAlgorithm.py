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
# limitations under the License

from AlgorithmImports import *

### <summary>
### This regression algorithm tests In The Money (ITM) index option calls across different strike prices.
### We expect 4* orders from the algorithm, which are:
###
###   * (1) Initial entry, buy SPX Call Option (SPXF21 expiring ITM)
###   * (2) Initial entry, sell SPX Call Option at different strike (SPXF21 expiring ITM)
###   * [2] Option assignment, settle into cash
###   * [1] Option exercise, settle into cash
###
### Additionally, we test delistings for index options and assert that our
### portfolio holdings reflect the orders the algorithm has submitted.
###
### * Assignments are counted as orders
### </summary>
class IndexOptionBuySellCallIntradayRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 1, 31)

        spx = self.AddIndex("SPX", Resolution.Minute).Symbol

        # Select a index option expiring ITM, and adds it to the algorithm.
        spxOptions = list(sorted([
            self.AddIndexOptionContract(i, Resolution.Minute).Symbol \
                for i in self.OptionChainProvider.GetOptionContractList(spx, self.Time)\
                    if (i.ID.StrikePrice == 3700 or i.ID.StrikePrice == 3800) and i.ID.OptionRight == OptionRight.Call and i.ID.Date.year == 2021 and i.ID.Date.month == 1],
            key=lambda x: x.ID.StrikePrice
        ))

        expectedContract3700 = Symbol.CreateOption(
            spx,
            Market.USA,
            OptionStyle.European,
            OptionRight.Call,
            3700,
            datetime(2021, 1, 15)
        )

        expectedContract3800 = Symbol.CreateOption(
            spx,
            Market.USA,
            OptionStyle.European,
            OptionRight.Call,
            3800,
            datetime(2021, 1, 15)
        )

        if len(spxOptions) != 2:
            raise Exception(f"Expected 2 index options symbols from chain provider, found {spxOptions.Count}")

        if spxOptions[0] != expectedContract3700:
            raise Exception(f"Contract {expectedContract3700} was not found in the chain, found instead: {spxOptions[0]}")
        
        if spxOptions[1] != expectedContract3800:
            raise Exception(f"Contract {expectedContract3800} was not found in the chain, found instead: {spxOptions[1]}")

        self.Schedule.On(self.DateRules.Tomorrow, self.TimeRules.AfterMarketOpen(spx, 1), lambda: self.AfterMarketOpenTrade(spxOptions))
        self.Schedule.On(self.DateRules.Tomorrow, self.TimeRules.Noon, lambda: self.Liquidate())

    def AfterMarketOpenTrade(self, spxOptions):
        self.MarketOrder(spxOptions[0], 1)
        self.MarketOrder(spxOptions[1], -1)

    ### <summary>
    ### Ran at the end of the algorithm to ensure the algorithm has no holdings
    ### </summary>
    ### <exception cref="Exception">The algorithm has holdings</exception>
    def OnEndOfAlgorithm(self):
        if self.Portfolio.Invested:
            raise Exception(f"Expected no holdings at end of algorithm, but are invested in: {', '.join(self.Portfolio.Keys)}")

