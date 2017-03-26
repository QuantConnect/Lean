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
import decimal
import numpy as np
import math
import json

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData

class CustomDataNIFTYAlgorithm(QCAlgorithm):
    '''3.0 CUSTOM DATA SOURCE: USE YOUR OWN MARKET DATA (OPTIONS, FOREX, FUTURES, DERIVATIVES etc).
    The new QuantConnect Lean Backtesting Engine is incredibly flexible and allows you to define your own data source. 
    This includes any data source which has a TIME and VALUE. These are the *only* requirements. 
    To demonstrate this we're loading in "Nifty" data. This by itself isn't special, the cool part is next:
    We load the "Nifty" data as a tradable security we're calling "NIFTY".'''

    def Initialize(self):
        self.SetStartDate(2008, 1, 8)
        self.SetEndDate(2014, 7, 25)
        self.SetCash(100000)

        # Define the symbol and "type" of our generic data:
        self.AddData(DollarRupee, "USDINR")
        self.rupee = self.Securities["USDINR"].Symbol
        self.AddData(Nifty, "NIFTY")
        self.nifty = self.Securities["NIFTY"].Symbol
        
        self.minimumCorrelationHistory = 50
        self.today = CorrelationPair()
        self.prices = []
        

    def OnData(self, data):
        if self.rupee in data:
            self.today = CorrelationPair(self.Time)
            self.today.CurrencyPrice = data[self.rupee].Close

        if self.nifty not in data: return

        self.today.NiftyPrice = data[self.nifty].Close

        if self.today.Date == data[self.nifty].Time:
            self.prices.append(self.today)
            if len(self.prices) > self.minimumCorrelationHistory:
                self.prices.pop(0)

        # Strategy
        if self.Time.DayOfWeek != DayOfWeek.Wednesday: return

        cur_qnty = self.Portfolio[self.nifty].Quantity
        quantity = math.floor(self.Portfolio.TotalPortfolioValue * decimal.Decimal(0.9) / data[self.nifty].Close)
        hi_nifty = max(price.NiftyPrice for price in self.prices)
        lo_nifty = min(price.NiftyPrice for price in self.prices)

        if data[self.nifty].Open >= hi_nifty:
            code = self.Order(self.nifty,  quantity - cur_qnty)
            self.Debug("LONG  {0} Time: {1} Quantity: {2} Portfolio: {3} Nifty: {4} Buying Power: {5}".format(code, self.Time.ToShortDateString(), quantity, self.Portfolio[self.nifty].Quantity, data[self.nifty].Close, self.Portfolio.TotalPortfolioValue))
        elif data[self.nifty].Open <= lo_nifty:
            code = self.Order(self.nifty, -quantity - cur_qnty)
            self.Debug("SHORT {0} Time: {1} Quantity: {2} Portfolio: {3} Nifty: {4} Buying Power: {5}".format(code, self.Time.ToShortDateString(), quantity, self.Portfolio[self.nifty].Quantity, data[self.nifty].Close, self.Portfolio.TotalPortfolioValue))


class Nifty(PythonData):
    '''NIFTY Custom Data Class'''
    def GetSource(self, config, date, isLiveMode):
        return SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        

    def Reader(self, config, line, date, isLiveMode):
        if not (line.strip() and line[0].isdigit()): return None

        # New Nifty object
        index = Nifty();
        index.Symbol = config.Symbol
        
        try:
            # Example File Format:
            # Date,       Open       High        Low       Close     Volume      Turnover
            # 2011-09-13  7792.9    7799.9     7722.65    7748.7    116534670    6107.78
            data = line.split(',')
            index.Time = DateTime.ParseExact(data[0], "yyyy-MM-dd", None)            
            index.Value = decimal.Decimal(data[4])
            index["Open"] = float(data[1])
            index["High"] = float(data[2])
            index["Low"] = float(data[3])
            index["Close"] = float(data[4])
                
        
        except ValueError:
                # Do nothing
                return None

        return index


class DollarRupee(PythonData):
    '''Dollar Rupe is a custom data type we create for this algorithm'''
    def GetSource(self, config, date, isLiveMode):
        return SubscriptionDataSource("https://www.dropbox.com/s/m6ecmkg9aijwzy2/USDINR.csv?dl=1", SubscriptionTransportMedium.RemoteFile)

    def Reader(self, config, line, date, isLiveMode):
        if not (line.strip() and line[0].isdigit()): return None

        # New USDINR object
        currency = DollarRupee();
        currency.Symbol = config.Symbol
        
        try:
            data = line.split(',')
            currency.Time = DateTime.Parse(data[0])
            currency.Value = decimal.Decimal(data[1])
            currency["Close"] = float(data[1])
            
        except ValueError:
            # Do nothing
            return None

        return currency;


class CorrelationPair:
    '''Correlation Pair is a helper class to combine two data points which we'll use to perform the correlation.'''
    def __init__(self, *args):
        self.NiftyPrice = 0       # Nifty price for this correlation pair
        self.CurrencyPrice = 0    # Currency price for this correlation pair
        self.Date = DateTime()    # Date of the correlation pair
        if len(args) > 0: self.Date = args[0]