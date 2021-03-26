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

from datetime import datetime, timedelta
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *
from QuantConnect import Market
from QuantConnect import *


### <summary>
### This regression algorithm tests In The Money (ITM) index option expiry for puts.
### We expect 2 orders from the algorithm, which are:
###
###   * Initial entry, buy ES Put Option (expiring ITM) (buy, qty 1)
###   * Option exercise, receiving cash (sell, qty -1)
###
### Additionally, we test delistings for index options and assert that our
### portfolio holdings reflect the orders the algorithm has submitted.
### </summary>
class IndexOptionPutITMExpiryRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 1, 31)

        self.spx = self.AddIndex("SPX", Resolution.Minute).Symbol

        # Select a index option expiring ITM, and adds it to the algorithm.
        self.spxOption = list(self.OptionChainProvider.GetOptionContractList(self.spx, self.Time))
        self.spxOption = [i for i in self.spxOption if i.ID.StrikePrice >= 4200 and i.ID.OptionRight == OptionRight.Put and i.ID.Date.year == 2021 and i.ID.Date.month == 1]
        self.spxOption = list(sorted(self.spxOption, key=lambda x: x.ID.StrikePrice))[0]
        self.spxOption = self.AddIndexOptionContract(self.spxOption, Resolution.Minute).Symbol

        self.expectedContract = Symbol.CreateOption(self.spx, Market.USA, OptionStyle.European, OptionRight.Put, 4200, datetime(2021, 1, 15))
        if self.spxOption != self.expectedContract:
            raise Exception(f"Contract {self.expectedContract} was not found in the chain")

        self.Schedule.On(self.DateRules.Tomorrow, self.TimeRules.AfterMarketOpen(self.spx, 1), lambda: self.MarketOrder(self.spxOption, 1))

    def OnData(self, data: Slice):
        # Assert delistings, so that we can make sure that we receive the delisting warnings at
        # the expected time. These assertions detect bug #4872
        for delisting in data.Delistings.Values:
            if delisting.Type == DelistingType.Warning:
                if delisting.Time != datetime(2021, 1, 15):
                    raise Exception(f"Delisting warning issued at unexpected date: {delisting.Time}")
            if delisting.Type == DelistingType.Delisted:
                if delisting.Time != datetime(2021, 1, 16):
                    raise Exception(f"Delisting happened at unexpected date: {delisting.Time}")

    def OnOrderEvent(self, orderEvent: OrderEvent):
        if orderEvent.Status != OrderStatus.Filled:
            # There's lots of noise with OnOrderEvent, but we're only interested in fills.
            return

        if orderEvent.Symbol not in self.Securities:
            raise Exception(f"Order event Symbol not found in Securities collection: {orderEvent.Symbol}")

        security = self.Securities[orderEvent.Symbol]
        if security.Symbol == self.spx:
            self.AssertIndexOptionOrderExercise(orderEvent, security, self.Securities[self.expectedContract])
        elif security.Symbol == self.expectedContract:
            self.AssertIndexOptionContractOrder(orderEvent, security)
        else:
            raise Exception(f"Received order event for unknown Symbol: {orderEvent.Symbol}")

    def AssertIndexOptionOrderExercise(self, orderEvent: OrderEvent, index: Security, optionContract: Security):
        expectedLiquidationTimeUtc = datetime(2021, 1, 15)

        if orderEvent.Direction == OrderDirection.Buy and orderEvent.UtcTime != expectedLiquidationTimeUtc:
            raise Exception(f"Liquidated index option contract, but not at the expected time. Expected: {expectedLiquidationTimeUtc} - found {orderEvent.UtcTime}")

        # No way to detect option exercise orders or any other kind of special orders
        # other than matching strings, for now.
        if "Option Exercise" in orderEvent.Message:
            if orderEvent.FillPrice != 3300:
                raise Exception("Option did not exercise at expected strike price (3300)")

            if optionContract.Holdings.Quantity != 0:
                raise Exception(f"Exercised option contract, but we have holdings for Option contract {optionContract.Symbol}")

    def AssertIndexOptionContractOrder(self, orderEvent: OrderEvent, option: Security):
        if orderEvent.Direction == OrderDirection.Buy and option.Holdings.Quantity != 1:
            raise Exception(f"No holdings were created for option contract {option.Symbol}")
        if orderEvent.Direction == OrderDirection.Sell and option.Holdings.Quantity != 0:
            raise Exception(f"Holdings were found after a filled option exercise")
        if "Exercise" in orderEvent.Message and option.Holdings.Quantity != 0:
            raise Exception(f"Holdings were found after exercising option contract {option.Symbol}")

    ### <summary>
    ### Ran at the end of the algorithm to ensure the algorithm has no holdings
    ### </summary>
    ### <exception cref="Exception">The algorithm has holdings</exception>
    def OnEndOfAlgorithm(self):
        if self.Portfolio.Invested:
            raise Exception(f"Expected no holdings at end of algorithm, but are invested in: {', '.join(self.Portfolio.Keys)}")