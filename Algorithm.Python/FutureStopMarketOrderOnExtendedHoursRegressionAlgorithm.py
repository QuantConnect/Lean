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
from QuantConnect import Orders

# <summary>
# This example demonstrates how to create future 'stopMarketOrder' in extended Market Hours time
# </summary>


class FutureStopMarketOrderOnExtendedHoursRegressionAlgorithm(QCAlgorithm):
    # Keep new created instance of stopMarketOrder
    stopMarketTicket = None
    SP500EMini = None

    # Initialize the Algorithm and Prepare Required Data
    def Initialize(self):
        self.SetStartDate(2013, 10, 6)
        self.SetEndDate(2013, 10, 12)

        # Add mini SP500 future with extended Market hours flag
        self.SP500EMini = self.AddFuture(
            Futures.Indices.SP500EMini, Resolution.Minute, extendedMarketHours=True
        )

        # Init new schedule event with params: everyDay, 19:00:00 PM, what should to do
        self.Schedule.On(
            self.DateRules.EveryDay(),
            self.TimeRules.At(19, 0),
            self.MakeMarketAndStopMarketOrder,
        )

    # This method is opened 2 new orders by scheduler
    def MakeMarketAndStopMarketOrder(self):
        self.MarketOrder(self.SP500EMini.Mapped, 1)
        self.stopMarketTicket = self.StopMarketOrder(
            self.SP500EMini.Mapped, -1, self.SP500EMini.Price * 0.999
        )

    # New Data Event handler receiving all subscription data in a single event
    def OnData(self, slice):
        if (
            self.stopMarketTicket == None
            or self.stopMarketTicket.Status != OrderStatus.Submitted
        ):
            return None

        self.stopPrice = self.stopMarketTicket.Get(OrderField.StopPrice)
        self.bar = self.Securities[self.stopMarketTicket.Symbol].Cache.GetData()

        if self.stopPrice > self.bar.Low:
            self.Log(f"{self.stopPrice} -> {self.bar.Low}")

    # An order fill update the resulting information is passed to this method.
    def OnOrderEvent(self, orderEvent):
        if orderEvent is not None and orderEvent.Status == OrderStatus.Filled:
            # Get Exchange Hours for specific security
            exchangeHours = self.MarketHoursDatabase.GetExchangeHours(
                self.SP500EMini.SubscriptionDataConfig
            )

            # Validate, Exchange is opened explicitly
            if (
                exchangeHours.IsOpen(
                    orderEvent.UtcTime, self.SP500EMini.IsExtendedMarketHours
                )
                == False
            ):
                raise Exception(
                    "The Exchange hours was closed, checko 'extendedMarketHours' flag in Initialize() when added new security(ies)"
                )
