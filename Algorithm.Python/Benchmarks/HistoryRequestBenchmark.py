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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *

class HistoryRequestBenchmark(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2010, 1, 1)
        self.SetEndDate(2018, 1, 1)
        self.SetCash(10000)
        self.symbol = self.AddEquity("SPY").Symbol

    def OnEndOfDay(self):
        minuteHistory = self.History([self.symbol], 60, Resolution.Minute)
        lastHourHigh = 0
        for index, row in minuteHistory.loc["SPY"].iterrows():
            if lastHourHigh < row["high"]:
                lastHourHigh = row["high"]

        dailyHistory = self.History([self.symbol], 1, Resolution.Daily).loc["SPY"].head()
        dailyHistoryHigh = dailyHistory["high"]
        dailyHistoryLow = dailyHistory["low"]
        dailyHistoryOpen = dailyHistory["open"]