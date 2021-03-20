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
AddReference("System.Core")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData
from QuantConnect.Securities import *

from datetime import datetime
import json

### <summary>
### Regression test to demonstrate setting custom Symbol Properties and Market Hours for a custom data import
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="importing data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="crypto" />
### <meta name="tag" content="regression test" />
class CustomDataPropertiesRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2011,9,13)   # Set Start Date
        self.SetEndDate(2015,12,1)     # Set End Date
        self.SetCash(100000)           # Set Strategy Cash
        
        # Define our custom data properties and exchange hours
        self.ticker = 'BTC'
        properties = SymbolProperties("Bitcoin", "USD", 1, 0.01, 0.01, self.ticker)
        exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork)

        # Add the custom data to our algorithm with our custom properties and exchange hours
        self.bitcoin = self.AddData(Bitcoin, self.ticker, properties, exchangeHours)

        # Verify our symbol properties were changed and loaded into this security
        if self.bitcoin.SymbolProperties != properties :
            raise Exception("Failed to set and retrieve custom SymbolProperties for BTC")

        # Verify our exchange hours were changed and loaded into this security
        if self.bitcoin.Exchange.Hours != exchangeHours :
            raise Exception("Failed to set and retrieve custom ExchangeHours for BTC")

        # For regression purposes on AddData overloads, this call is simply to ensure Lean can accept this
        # with default params and is not routed to a breaking function.
        self.AddData(Bitcoin, "BTCUSD");


    def OnData(self, data):
        if not self.Portfolio.Invested:
            if data['BTC'].Close != 0 :
                self.Order('BTC', self.Portfolio.MarginRemaining/abs(data['BTC'].Close + 1))

    def OnEndOfAlgorithm(self):
        #Reset our Symbol property value, for testing purposes.
        self.SymbolPropertiesDatabase.SetEntry(Market.USA, self.MarketHoursDatabase.GetDatabaseSymbolKey(self.bitcoin.Symbol), SecurityType.Base,
            SymbolProperties.GetDefault("USD"));



class Bitcoin(PythonData):
    '''Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data'''

    def GetSource(self, config, date, isLiveMode):
        if isLiveMode:
            return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest)

        #return "http://my-ftp-server.com/futures-data-" + date.ToString("Ymd") + ".zip"
        # OR simply return a fixed small data file. Large files will slow down your backtest
        return SubscriptionDataSource("https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm", SubscriptionTransportMedium.RemoteFile)


    def Reader(self, config, line, date, isLiveMode):
        coin = Bitcoin()
        coin.Symbol = config.Symbol

        if isLiveMode:
            # Example Line Format:
            # {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
            try:
                liveBTC = json.loads(line)

                # If value is zero, return None
                value = liveBTC["last"]
                if value == 0: return None

                coin.Time = datetime.now()
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
            coin.Time = datetime.strptime(data[0], "%Y-%m-%d")
            coin.Value = float(data[4])
            coin["Open"] = float(data[1])
            coin["High"] = float(data[2])
            coin["Low"] = float(data[3])
            coin["Close"] = float(data[4])
            coin["VolumeBTC"] = float(data[5])
            coin["VolumeUSD"] = float(data[6])
            coin["WeightedPrice"] = float(data[7])
            return coin

        except ValueError:
            # Do nothing, possible error in json decoding
            return None