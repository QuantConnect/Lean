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
###
### </summary>
class ForwardDataOnlyFillModelAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,1)
        self.SetEndDate(2013,10,31)

        self.security = self.AddEquity("SPY", Resolution.Hour)
        self.security.SetFillModel(ForwardDataOnlyFillModel())

    def OnData(self, data: Slice):
        if not self.Portfolio.Invested:
            ticket = self.Buy("SPY", 1)
            if ticket.Status != OrderStatus.Submitted:
                raise Exception(f"Unexpected order status {ticket.Status}");

    def OnOrderEvent(self, orderEvent: OrderEvent):
        self.Debug(f"OnOrderEvent:: {orderEvent}")

class ForwardDataOnlyFillModel(EquityFillModel):
    def Fill(self, parameters: FillModelParameters):
        orderLocalTime = Extensions.ConvertFromUtc(parameters.Order.Time, parameters.Security.Exchange.TimeZone)
        for dataType in [ QuoteBar, TradeBar, Tick ]:
            data = parameters.Security.Cache.GetData[dataType]()
            if not data is None and orderLocalTime < data.EndTime:
                return super().Fill(parameters)
        return Fill([])
