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
### Example of custom fill model for security to only fill bars of data obtained after the order was placed. This is to encourage more
### pessimistic fill models and eliminate the possibility to fill on old market data that may not be relevant.
### </summary>
class ForwardDataOnlyFillModelAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,10,31)

        self.security = self.AddEquity("SPY", Resolution.Hour)
        self.security.SetFillModel(ForwardDataOnlyFillModel())

        self.Schedule.On(self.DateRules.WeekStart(), self.TimeRules.AfterMarketOpen(self.security.Symbol), self.Trade)

    def Trade(self):
        if not self.Portfolio.Invested:
            if self.Time.hour != 9 or self.Time.minute != 30:
                raise Exception(f"Unexpected event time {self.Time}")

            ticket = self.Buy("SPY", 1)
            if ticket.Status != OrderStatus.Submitted:
                raise Exception(f"Unexpected order status {ticket.Status}")

    def OnOrderEvent(self, orderEvent: OrderEvent):
        self.Debug(f"OnOrderEvent:: {orderEvent}")
        if orderEvent.Status == OrderStatus.Filled and (self.Time.hour != 10 or self.Time.minute != 0):
            raise Exception(f"Unexpected fill time {self.Time}")

class ForwardDataOnlyFillModel(EquityFillModel):
    def Fill(self, parameters: FillModelParameters):
        orderLocalTime = Extensions.ConvertFromUtc(parameters.Order.Time, parameters.Security.Exchange.TimeZone)
        for dataType in [ QuoteBar, TradeBar, Tick ]:
            data = parameters.Security.Cache.GetData[dataType]()
            if not data is None and orderLocalTime <= data.EndTime:
                return super().Fill(parameters)
        return Fill([])
