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
from csv import reader
from urllib import urlopen

from clr import AddReference
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.UniverseSelection import *


class DropboxUniverseSelectionAlgorithm(QCAlgorithm):
    '''In this algortihm we show how you can easily use the universe selection feature to fetch symbols
    to be traded using the AddUniverse method. This method accepts a function that will return the
    desired current set of symbols. Return Universe.Unchanged if no universe changes should be made'''

    def __init__(self):
        # the changes from the previous universe selection      
        self.__changes = SecurityChanges.None
        # only used in backtest for caching the file results
        self.__backtestSymbolsPerDay = {}
        

    def CoarseSelectionFunction(self, dateTime):
        url = "https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1" \
            if self.LiveMode else \
              "https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1"

        # handle live mode file format
        if self.LiveMode:
            # fetch the file from dropbox  
            file = urlopen(url).read()
            # if we have a file for today, break apart by commas and return symbols
            if len(file) > 0: return file.ToCsv()
            # no symbol today, leave universe unchanged
            return self.Universe.Unchanged

        # backtest - first cache the entire file
        if len(self.__backtestSymbolsPerDay) == 0:
            # fetch the file from dropbox only if we haven't cached the result already
            file = reader(urlopen(url))
            for line in file:
                date = datetime.strptime(line[0], '%Y%m%d')
                self.__backtestSymbolsPerDay[date] = line[1:]

        # if we have symbols for this date return them, else specify Universe.Unchanged
        return self.__backtestSymbolsPerDay.get(datetime(dateTime), self.Universe.Unchanged)


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013,01,01)  #Set Start Date
        self.SetEndDate(2013,12,31)    #Set End Date
        # this sets the resolution for data subscriptions added by our universe
        self.UniverseSettings.Resolution = Resolution.Daily        
        
        self.AddUniverse("my-dropbox-universe", Resolution.Daily, self.CoarseSelectionFunction)

        
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if len(data.Bars) == 0: return
        if self.__changes == SecurityChanges.None: return

        # start fresh
        self.Liquidate()

        percentage = 1./len(data.Bars)
        for tradeBar in data.Bars.Values:
            self.SetHoldings(tradeBar.Symbol, percentage)

        # reset changes
        self.__changes = SecurityChanges.None


    def OnSecuritiesChanged(self, changes):
        '''Event fired each time the we add/remove securities from the data feed'''
        # each time our securities change we'll be notified here
        self.__changes = changes