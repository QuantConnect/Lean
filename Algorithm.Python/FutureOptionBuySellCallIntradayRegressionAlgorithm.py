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

import clr
from System import *
from System.Reflection import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Future import *
from QuantConnect import Market


### <summary>
### This regression algorithm tests In The Money (ITM) future option expiry for calls.
### We expect 3 orders from the algorithm, which are:
### 
###   * Initial entry, buy ES Call Option (expiring ITM)
###   * Option exercise, receiving ES future contracts
###   * Future contract liquidation, due to impending expiry
### 
### Additionally, we test delistings for future options and assert that our
### portfolio holdings reflect the orders the algorithm has submitted.
### </summary>
class FutureOptionBuySellCallIntradayRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 3, 1)
        clr.GetClrType(QCAlgorithm).GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, DateTime(2021, 3, 30))

        start = datetime(2020, 9, 22)

        # We add AAPL as a temporary workaround for https://github.com/QuantConnect/Lean/issues/4872
        # which causes delisting events to never be processed, thus leading to options that might never
        # be exercised until the next data point arrives.
        self.AddEquity("AAPL", Resolution.Daily)

        self.es18z20 = self.AddFutureContract(
            Symbol.CreateFuture(
                Futures.Indices.SP500EMini,
                Market.CME,
                datetime(2020, 12, 18)
            ),
            Resolution.Minute).Symbol

        self.es19h21 = self.AddFutureContract(
            Symbol.CreateFuture(
                Futures.Indices.SP500EMini,
                Market.CME,
                datetime(2021, 3, 19)
            ),
            Resolution.Minute).Symbol

        # Select a future option expiring ITM, and adds it to the algorithm.
        self.esOptions = [
            self.AddFutureOptionContract(i, Resolution.Minute).Symbol for i in (self.OptionChainProvider.GetOptionContractList(self.es19h21, start) + self.OptionChainProvider.GetOptionContractList(self.es18z20, start)) if i.ID.StrikePrice == 3250.0 and i.ID.OptionRight == OptionRight.Call
        ]

        self.expectedContracts = [
            Symbol.CreateOption(self.es19h21, Market.CME, OptionStyle.American, OptionRight.Call, 3250.0, datetime(2021, 3, 19)),
            Symbol.CreateOption(self.es18z20, Market.CME, OptionStyle.American, OptionRight.Call, 3250.0, datetime(2020, 12, 18))
        ]

        for esOption in self.esOptions:
            if esOption not in self.expectedContracts:
                raise AssertionError(f"Contract {esOption} was not found in the chain")

        self.Schedule.On(self.DateRules.On(start.year, start.month, start.day), self.TimeRules.AfterMarketOpen(self.es19h21, 1), self.ScheduleCallbackBuy)
        self.Schedule.On(self.DateRules.On(start.year, start.month, start.day), self.TimeRules.Noon, self.ScheduleCallbackLiquidate)

    def ScheduleCallbackBuy(self):
        self.MarketOrder(self.esOptions[0], 1)
        self.MarketOrder(self.esOptions[1], -1)

    def ScheduleCallbackLiquidate(self):
        self.Liquidate()

    def OnEndOfAlgorithm(self):
        if self.Portfolio.Invested:
            raise AssertionError(f"Expected no holdings at end of algorithm, but are invested in: {', '.join([str(i.ID) for i in self.Portfolio.Keys])}")
    