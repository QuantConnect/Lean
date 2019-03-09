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
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *
import base64

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

        self.backtestSymbolsPerDay = {}
        self.current_universe = []

        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverse("my-dropbox-universe", self.selector)

    def selector(self, date):
        # handle live mode file format
        if self.LiveMode:
            # fetch the file from dropbox
            str = self.Download("https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1")
            # if we have a file for today, return symbols, else leave universe unchanged
            self.current_universe = str.split(',') if len(str) > 0 else self.current_universe
            return self.current_universe

        # backtest - first cache the entire file
        if len(self.backtestSymbolsPerDay) == 0:

            # No need for headers for authorization with dropbox, these two lines are for example purposes 
            byteKey = base64.b64encode("UserName:Password".encode('ASCII'))
            # The headers must be passed to the Download method as dictionary
            headers = { 'Authorization' : f'Basic ({byteKey.decode("ASCII")})' }

            str = self.Download("https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1", headers)
            for line in str.splitlines():
                data = line.split(',')
                self.backtestSymbolsPerDay[data[0]] = data[1:]

        index = date.strftime("%Y%m%d")
        self.current_universe = self.backtestSymbolsPerDay.get(index, self.current_universe)

        return self.current_universe

    def OnData(self, slice):

        if slice.Bars.Count == 0: return
        if self.changes is None: return

        # start fresh
        self.Liquidate()

        percentage = 1 / slice.Bars.Count
        for tradeBar in slice.Bars.Values:
            self.SetHoldings(tradeBar.Symbol, percentage)

        # reset changes
        self.changes = None

    def OnSecuritiesChanged(self, changes):
        self.changes = changes