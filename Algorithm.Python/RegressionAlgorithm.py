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

from datetime import datetime, timedelta

import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Indicators")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Market import *


class RegressionAlgorithm(QCAlgorithm):
    '''Algorithm used for regression tests purposes'''

    def __init__(self):
        self.__lastTradeTicks = None
        self.__lastTradeTradeBars = None
        self.__tradeEvery = timedelta(minutes=1)

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,10,07)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(10000000)         #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Tick);
        self.AddSecurity(SecurityType.Equity, "BAC", Resolution.Minute);
        self.AddSecurity(SecurityType.Equity, "AIG", Resolution.Hour);
        self.AddSecurity(SecurityType.Equity, "IBM", Resolution.Daily);

        self.__lastTradeTicks = datetime(2013,10,07)
        self.__lastTradeTradeBars = datetime(2013,10,07)
        

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Tradebars object keyed by symbol containing the stock data
        '''
        pyTime = datetime(self.Time)

        if pyTime - self.__lastTradeTradeBars < self.__tradeEvery:
            return
        self.__lastTradeTradeBars = pyTime

        for symbol in data.Bars.Keys:            
            period = data.Bars[symbol].Period.TotalSeconds
            
            if self.roundTime(pyTime, period) != pyTime:
                pass

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
        if dt == None : dt = datetime.now()
        seconds = (dt - dt.min).seconds
        # // is a floor division, not a comment on following line:
        rounding = (seconds+roundTo/2) // roundTo * roundTo
        return dt + timedelta(0,rounding-seconds,-dt.microsecond)