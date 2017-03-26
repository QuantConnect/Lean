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


class CustomDataBitcoinAlgorithm(QCAlgorithm):
    '''3.0 CUSTOM DATA SOURCE: USE YOUR OWN MARKET DATA (OPTIONS, FOREX, FUTURES, DERIVATIVES etc).
    The new QuantConnect Lean Backtesting Engine is incredibly flexible and allows you to define your own data source. 
    This includes any data source which has a TIME and VALUE. These are the *only* requirements.
    To demonstrate this we're loading in "Bitcoin" data.'''

    def Initialize(self):
        self.SetStartDate(2011, 9, 13)
        self.SetEndDate(DateTime.Now.Date.AddDays(-1))
        self.SetCash(100000)

        # Define the symbol and "type" of our generic data:
        self.AddData(Bitcoin, "BTC")
        self.btc = self.Securities["BTC"].Symbol


    def OnData(self, data):
        if self.btc not in data: return

        close = data[self.btc].Close
        
        # If we don't have any weather "SHARES" -- invest"
        if not self.Portfolio.Invested:
            # Weather used as a tradable asset, like stocks, futures etc. 
            self.SetHoldings(self.btc, 1)
            self.Debug("Buying BTC 'Shares': BTC: {0}".format(close))
        
        self.Debug("Time: {0} {1} {2}".format(DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), close))


class Bitcoin(PythonData):
    '''Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data'''
 
    def GetSource(self, config, date, isLiveMode):
        if isLiveMode:
            return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest);
            
        #return "http://my-ftp-server.com/futures-data-" + date.ToString("Ymd") + ".zip";
        # OR simply return a fixed small data file. Large files will slow down your backtest
        return SubscriptionDataSource("http://www.quandl.com/api/v1/datasets/BCHARTS/BITSTAMPUSD.csv?sort_order=asc", SubscriptionTransportMedium.RemoteFile);


    def Reader(self, config, line, date, isLiveMode):
        coin = Bitcoin()
        coin.Symbol = config.Symbol
            
        if isLiveMode:
            # Example Line Format:
            # {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
            try:
                liveBTC = json.loads(line)
                
                # If value is zero, return None
                value = decimal.Decimal(liveBTC["last"])
                if value == 0: return None

                coin.Time = DateTime.Now
                coin.Value = value                
                coin["Open"] = float(liveBTC["open"])
                coin["High"] = float(liveBTC["high"])
                coin["Low"] = float(liveBTC["low"])
                coin["Close"] = float(liveBTC["last"])
                coin["Ask"] = float(liveBTC["ask"])
                coin["Bid"] = float(liveBTC["bid"])
                coin["VolumeBTC"] = float(liveBTC["volume"])
                coin["WeightedPrice"] = float(liveBTC["vwap"])
                return coin
            except ValueError:
                # Do nothing, possible error in json decoding
                return None

        # Example Line Format:
        # Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
        # 2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
        if not (line.strip() and line[0].isdigit()): return None
        
        try:
            data = line.split(',')
            
            # If value is zero, return None
            value = decimal.Decimal(data[4])
            if value == 0: return None

            coin.Time = DateTime.Now #DateTime.Parse(data[0], None)
            coin.Value = value
            coin["Open"] = float(data[1])
            coin["High"] = float(data[2])
            coin["Low"] = float(data[3])
            coin["Close"] = float(data[4])
            coin["VolumeBTC"] = float(data[5])
            coin["VolumeUSD"] = float(data[6])
            coin["WeightedPrice"] = float(data[7])
            return coin;
            
        except ValueError:
            # Do nothing, possible error in json decoding
            return None