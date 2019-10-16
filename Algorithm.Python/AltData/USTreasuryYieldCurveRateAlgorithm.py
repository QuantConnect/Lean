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
from QuantConnect.Data import *
from QuantConnect.Data.Custom.USTreasury import *

from datetime import datetime, timedelta

class USTreasuryYieldCurveRateAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.SetStartDate(2000, 3, 1)
        self.SetEndDate(2019, 9, 15)
        self.SetCash(100000)

        self.spy = self.AddEquity("SPY", Resolution.Hour).Symbol
        self.yieldCurve = self.AddData(USTreasuryYieldCurveRate, "USTYCR").Symbol
        self.lastInversion = datetime(1, 1, 1)

        # Request 60 days of history with the USTreasuryYieldCurveRate custom data Symbol.
        history = self.History(USTreasuryYieldCurveRate, self.yieldCurve, 60, Resolution.Daily)

        # Count the number of items we get from our history request
        self.Debug(f"We got {len(history)} items from our history request")

    def OnData(self, data):

        if not data.ContainsKey(self.yieldCurve):
            return

        rates = data[self.yieldCurve]

        # Check for None before using the values
        if rates.TenYear is None or rates.TwoYear is None:
            return

        # Only advance if a year has gone by
        if (self.Time - self.lastInversion) < timedelta(days=365):
            return

        # if there is a yield curve inversion after not having one for a year, short SPY for two years
        if not self.Portfolio.Invested and rates.TwoYear > rates.TenYear:
            self.Debug(f"{self.Time} - Yield curve inversion! Shorting the market for two years")
            self.SetHoldings(self.spy, -0.5)

            self.lastInversion = self.Time

            return

        # If two years have passed, liquidate our position in SPY
        if self.Time - self.lastInversion >= timedelta(days=365 * 2):
            self.Liquidate(self.spy)
