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
from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData
from datetime import date, timedelta, datetime
from System.Collections.Generic import List
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm
import decimal as d
import numpy as np
import math
import json

### <summary>
### In this algortihm we show how you can easily use the universe selection feature to fetch symbols
### to be traded using the BaseData custom data system in combination with the AddUniverse{T} method.
### AddUniverse{T} requires a function that will return the symbols to be traded.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="custom universes" />
class DropboxBaseDataUniverseSelectionAlgorithm(QCAlgorithm):

    def Initialize(self):

        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2013,1,1)
        self.SetEndDate(2013,12,31)
        
        self.AddUniverse(StockDataSource, "my-stock-data-source", self.stockDataSource)
    
    def stockDataSource(self, data):
        list = []
        for item in data:
            for symbol in item["Symbols"]:
                list.append(symbol)
        return list

    def OnData(self, slice):

        if slice.Bars.Count == 0: return
        if self._changes is None: return
        
        # start fresh
        self.Liquidate()

        percentage = 1 / d.Decimal(slice.Bars.Count)
        for tradeBar in slice.Bars.Values:
            self.SetHoldings(tradeBar.Symbol, percentage)
        
        # reset changes
        self._changes = None
    
    def OnSecuritiesChanged(self, changes):
        self._changes = changes
        
class StockDataSource(PythonData):
    
    def GetSource(self, config, date, isLiveMode):
        url = "https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1" if isLiveMode else \
            "https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1"

        return SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile)
    
    def Reader(self, config, line, date, isLiveMode):
        if not (line.strip() and line[0].isdigit()): return None
        
        stocks = StockDataSource()
        stocks.Symbol = config.Symbol
        
        csv = line.split(',')
        if isLiveMode:
            stocks.Time = date
            stocks["Symbols"] = csv
        else:
            stocks.Time = datetime.strptime(csv[0], "%Y%m%d")
            stocks["Symbols"] = csv[1:]
        return stocks