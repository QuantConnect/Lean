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
### Trades on interest rate announcements from data provided by Trading Economics
### </summary>
class TradingEconomicsAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 11, 1)
        self.SetEndDate(2019, 10, 3);
        self.SetCash(100000)

        self.AddEquity("AGG", Resolution.Hour)
        self.AddEquity("SPY", Resolution.Hour)
        self.interestRate = self.AddData(TradingEconomicsCalendar, TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol

        # Request 365 days of interest rate history with the TradingEconomicsCalendar custom data Symbol.
        # We should expect no historical data because 2013-11-01 is before the absolute first point of data
        history = self.History(TradingEconomicsCalendar, self.interestRate, 365, Resolution.Daily)

        # Count the amount of items we get from our history request (should be zero)
        self.Debug(f"We got {len(history)} items from our history request")

    def OnData(self, data):
        # Make sure we have an interest rate calendar event
        if not data.ContainsKey(self.interestRate):
            return

        announcement = data[self.interestRate]

        # Confirm its a FED Rate Decision
        if announcement.Event != "Fed Interest Rate Decision":
            return

        # In the event of a rate increase, rebalance 50% to Bonds.
        interestRateDecreased = announcement.Actual <= announcement.Previous

        if interestRateDecreased:
            self.SetHoldings("SPY", 1)
            self.SetHoldings("AGG", 0)
        else:
            self.SetHoldings("SPY", 0.5)
            self.SetHoldings("AGG", 0.5)
