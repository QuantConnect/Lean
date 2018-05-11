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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Brokerages import *
from datetime import timedelta

### <summary>
### Regression algorithm for guaranteed Volume Weighted Average Price orders.
### This algorithm shows how to submit VWAP orders.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class GuaranteedVwapRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage)
        self.AddEquity("SPY", Resolution.Minute)

        # The guaranteed VWAP order will be submitted in pre-market,
        # so we warmup one day to ensure we have a price for the security
        # when the first order is submitted.
        self.SetWarmUp(timedelta(days=1))

        # IB will only accept Guaranteed VWAP orders at or before 9.29 AM,
        # so LEAN requires them to be submitted at least one minute earlier.

        # Guaranteed VWAP orders must be submitted at or before 9:29 AM
        self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.At(9, 15), self.EveryDayBeforeMarketOpen)

    def EveryDayBeforeMarketOpen(self):
        self.Debug(f"Submitting VWAP order at: {self.Time}")

        # Submit the VWAP order
        self.VwapOrder("SPY", 100)

    def OnOrderEvent(self, orderEvent):
        self.Debug(f"{self.Time} {orderEvent}")
