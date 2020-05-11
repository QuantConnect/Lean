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

from datetime import datetime, timedelta

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Custom.Robintrack import *

### <summary>
### Looks at users holding the stock AAPL at a given point in time
### and keeps track of changes in retail investor sentiment.
###
### We go long if the sentiment increases by 0.5%, and short if it decreases by -0.5%
### </summary>
class RobintrackHoldingsAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.lastValue = 0

        self.SetStartDate(2018, 5, 1)
        self.SetEndDate(2020, 5, 5)
        self.SetCash(100000)

        self.aapl = self.AddEquity("AAPL", Resolution.Daily).Symbol
        self.aaplHoldings = self.AddData(RobintrackHoldings, self.aapl).Symbol
        self.isLong = False

    def OnData(self, data):
        for kvp in data.Get(RobintrackHoldings):
            holdings = kvp.Value

            if self.lastValue != 0:
                percentChange = (holdings.UsersHolding - self.lastValue) / self.lastValue
                holdingInfo = f"There are {holdings.UsersHolding} unique users holding {kvp.Key.Underlying} - users holding % of U.S. equities universe: {holdings.UniverseHoldingPercent * 100.0}%"

                if percentChange >= 0.005 and not self.isLong:
                    self.Log(f"{self.UtcTime} - Buying AAPL - {holdingInfo}")
                    self.SetHoldings(self.aapl, 0.5)
                    self.isLong = True

                elif percentChange <= -0.005 and self.isLong:
                    self.Log(f"{self.UtcTime} - Shorting AAPL - {holdingInfo}")
                    self.SetHoldings(self.aapl, -0.5)
                    self.isLong = False

            self.lastValue = holdings.UsersHolding;
