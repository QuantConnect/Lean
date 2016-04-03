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

from datetime import date, timedelta

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Data.Custom import *


class QuandlImporterAlgorithm(QCAlgorithm):
    '''QuantConnect University: Generic Quandl Data Importer
    Using the underlying dynamic data class "Quandl" we take care of the data 
    importing and definition for you. Simply point QuantConnect to the Quandl Short Code.
     
    The Quandl object has properties which match the spreadsheet headers.
    If you have multiple quandl streams look at data.Symbol to distinguish them.'''
    def __init__(self):
        self.__sma = None
        self.__quandlCode = "YAHOO/INDEX_SPY"

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        yesterday = date.today() - timedelta(1)

        self.SetStartDate(2013,1,1)                                      #Set Start Date
        self.SetEndDate(yesterday.year, yesterday.month, yesterday.day)  #Set End Date
        self.SetCash(25000)                                              #Set Strategy Cash
        self.AddData[Quandl](self.__quandlCode, Resolution.Daily)
        self.__sma = self.SMA(self.__quandlCode, 14)

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.HoldStock:
            self.SetHoldings(self.__quandlCode, 1)
            self.Debug("Purchased {0} >> {1}".format(self.__quandlCode, self.Time))

        self.Plot("SPY", self.__sma)