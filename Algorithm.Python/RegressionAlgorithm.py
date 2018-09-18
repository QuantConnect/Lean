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
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data.Market import *
from QCAlgorithm import QCAlgorithm
from datetime import datetime, timedelta

### <summary>
### Algorithm used for regression tests purposes
### </summary>
### <meta name="tag" content="regression test" />
class RegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(10000000)         #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Tick)
        self.AddEquity("BAC", Resolution.Minute)
        self.AddEquity("AIG", Resolution.Hour)
        self.AddEquity("IBM", Resolution.Daily)

        self.__lastTradeTicks = self.StartDate
        self.__lastTradeTradeBars = self.__lastTradeTicks
        self.__tradeEvery = timedelta(minutes=1)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if self.Time - self.__lastTradeTradeBars < self.__tradeEvery:
            return
        self.__lastTradeTradeBars = self.Time

        for kvp in data.Bars:
            period = kvp.Value.Period.total_seconds()

            if self.roundTime(self.Time, period) != self.Time:
                pass

            symbol = kvp.Key
            holdings = self.Portfolio[symbol]

            if not holdings.Invested:
                self.MarketOrder(symbol, 10)
            else:
                self.MarketOrder(symbol, -holdings.Quantity)


    def roundTime(self, dt=None, roundTo=60):
        """Round a datetime object to any time laps in seconds
        dt : datetime object, default now.
        roundTo : Closest number of seconds to round to, default 1 minute.
        """
        if dt is None : dt = datetime.now()
        seconds = (dt - dt.min).seconds
        # // is a floor division, not a comment on following line:
        rounding = (seconds+roundTo/2) // roundTo * roundTo
        return dt + timedelta(0,rounding-seconds,-dt.microsecond)