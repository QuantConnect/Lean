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
from QuantConnect.Data.Custom.TradingEconomics import *

### <summary>
### This example algorithm shows how to import and use Trading Economics data.
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="tradingeconomics" />
class InterestRateForexAlgorithm(QCAlgorithm):
    def Initialize(self):

        self.SetStartDate(2017, 1, 1)  # Set Start Date
        self.SetEndDate(2019, 4, 30)    # Set End Date
        self.SetCash(100000)           # Set Strategy Cash
        self.holdingDays = 31          # Set holding days for portfolio

        self.forex = self.AddForex("EURUSD", Resolution.Hour, Market.FXCM).Symbol
        self.calendar = self.AddData(TradingEconomicsCalendar, TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol

    def OnData(self, slice):
        if slice.ContainsKey(self.calendar):
            #self.Debug(f"{self.Time} Forecast: {slice[self.calendar].Forecast}")
            fore_IR = slice[self.calendar].Forecast # forecast Interest Rate
            prev_IR = slice[self.calendar].Previous # previous released actual Interest Rate

            # IR increase, USD becomes more valuable EURUSD down
            if fore_IR > prev_IR:
                # enter short position
                self.SetHoldings(self.forex, -1)
                self.OrderOpenTime = self.Time
                self.Log(f"{self.OrderOpenTime} Enter Short Position at {slice[self.forex].Close}")

            # IR not increase, USD becomes less valuable, EURUSD up
            else:
                # enter long position
                self.SetHoldings(self.forex, 1)
                self.OrderOpenTime = self.Time
                self.Log(f"{self.OrderOpenTime} Enter Long Position at {slice[self.forex].Close}")

        if self.Portfolio.Invested and (self.Time - self.OrderOpenTime).days > self.holdingDays:
            self.Liquidate(self.forex)
            self.Log(f"{self.Time} Liquidate at {slice[self.forex].Close}")