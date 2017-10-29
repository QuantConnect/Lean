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
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *
from datetime import datetime
import decimal as d
import pandas as pd

### <summary>
### In this algortihm we show how you can easily use the universe selection feature to fetch symbols
### to be traded using the BaseData custom data system in combination with the AddUniverse{T} method.
### AddUniverse{T} requires a function that will return the symbols to be traded.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="custom universes" />
class DropboxUniverseSelectionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,1,1)
        self.SetEndDate(2013,12,31)
        
        self.backtestSymbolsPerDay = None
        self.current_universe = []

        self.UniverseSettings.Resolution = Resolution.Daily;
        self.AddUniverse("my-dropbox-universe", self.selector)
    
    def selector(self, data):
        # handle live mode file format
        if self.LiveMode:
            # fetch the file from dropbox
            url = "https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1"
            df = pd.read_csv(url, header = None)
            # if we have a file for today, return symbols
            if not df.empty: 
                self.current_universe = df.iloc[0,:].tolist()
            # no symbol today, leave universe unchanged
            return self.current_universe

        # backtest - first cache the entire file
        if self.backtestSymbolsPerDay is None:
            url = "https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1"
            self.backtestSymbolsPerDay = pd.read_csv(url, header = None, index_col = 0)
        
        index = int(data.strftime("%Y%m%d"))
        if index in self.backtestSymbolsPerDay.index:
            self.current_universe = self.backtestSymbolsPerDay.loc[index,:].dropna().tolist()

        return self.current_universe


    def OnData(self, slice):

        if slice.Bars.Count == 0: return
        if self.changes == None: return
        
        # start fresh
        self.Liquidate()

        percentage = 1 / d.Decimal(slice.Bars.Count)
        for tradeBar in slice.Bars.Values:
            self.SetHoldings(tradeBar.Symbol, percentage)
        
        # reset changes
        self.changes = None
    
    def OnSecuritiesChanged(self, changes):
        self.changes = changes