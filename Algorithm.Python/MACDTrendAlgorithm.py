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

from datetime import datetime

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from AlgorithmPythonUtil import to_python_datetime


class MACDTrendAlgorithm(QCAlgorithm):
    '''MACD Example Algorithm'''
        
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2004, 01, 01)  #Set Start Date
        self.SetEndDate(2015, 01, 01)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        equity = self.AddEquity("SPY", Resolution.Daily)
        self.spy = equity.Symbol

        # define our daily macd(12,26) with a 9 day signal
        self.__macd = self.MACD(self.spy, 9, 26, 9, MovingAverageType.Exponential, Resolution.Daily)
        self.__previous = datetime.min


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        # wait for our macd to fully initialize
        if not self.__macd.IsReady: return    

        pyTime = to_python_datetime(self.Time)
    
        # only once per day
        if self.__previous.date() == pyTime.date(): return

        # define a small tolerance on our checks to avoid bouncing
        tolerance = 0.0025;
        
        holdings = self.Portfolio[self.spy].Quantity

        signalDeltaPercent = (self.__macd.Current.Value - self.__macd.Signal.Current.Value)/self.__macd.Fast.Current.Value

        # if our macd is greater than our signal, then let's go long
        if holdings <= 0 and signalDeltaPercent > tolerance:  # 0.01%
            # longterm says buy as well
            self.SetHoldings(self.spy, 1.0)

        # of our macd is less than our signal, then let's go short
        elif holdings >= 0 and signalDeltaPercent < -tolerance:
            self.Liquidate(self.spy)  

        # plot both lines
        self.Plot("MACD", self.__macd.Current.Value)
        self.Plot("MACD", self.__macd.Signal.Current.Value)
        self.Plot(str(self.spy), self.__macd.Fast.Current.Value)
        self.Plot(str(self.spy), self.__macd.Slow.Current.Value)

        self.__previous = pyTime