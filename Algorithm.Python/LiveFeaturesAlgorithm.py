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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")


from System import *
from System.Globalization import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Python import PythonData

import numpy as np
from datetime import datetime
import json


### <summary>
### Live Trading Functionality Demonstration algorithm including SMS, Email and Web hook notifications.
### </summary>
### <meta name="tag" content="live trading" />
### <meta name="tag" content="alerts" />
### <meta name="tag" content="sms alerts" />
### <meta name="tag" content="web hooks" />
### <meta name="tag" content="email alerts" />
### <meta name="tag" content="runtime statistics" />

class LiveTradingFeaturesAlgorithm(QCAlgorithm):

    ### Initialize the Algorithm and Prepare Required Data
    def Initialize(self):

        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(25000)

        ##Equity Data for US Markets
        self.AddSecurity(SecurityType.Equity, 'IBM', Resolution.Second)
        
        ##FOREX Data for Weekends: 24/6
        self.AddSecurity(SecurityType.Forex, 'EURUSD', Resolution.Minute)

        ##Custom/Bitcoin Live Data: 24/7
        self.AddData(Bitcoin, 'BTC', Resolution.Second, TimeZones.Utc)


    ### New Bitcoin Data Event
    def OnData(Bitcoin, data):
        if self.LiveMode:
            self.SetRuntimeStatistic('BTC', str(data.Close))

        if not self.Portfolio.HoldStock:
            self.MarketOrder('BTC', 100)

            ##Send a notification email/SMS/web request on events:
            self.Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment")
            self.Notify.Sms("+11233456789", str(data.Time) + ">> Test message from live BTC server.")
            self.Notify.Web("http://api.quantconnect.com", str(data.Time) + ">> Test data packet posted from live BTC server.")


    ### Raises the data event
    def OnData(self, data):
        if (not self.Portfolio['IBM'].HoldStock) and data.ContainsKey('IBM'):
            quantity = int(np.floor(self.Portfolio.MarginRemaining / data['IBM'].Close))
            self.MarketOrder('IBM',quantity)
            self.Debug('Purchased IBM on ' + str(self.Time.strftime("%m/%d/%Y")))
            self.Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment")

###Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data
class Bitcoin(PythonData):

    def GetSource(self, config, date, isLiveMode):
        if isLiveMode:
            return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest)
        
        return  SubscriptionDataSource("https://www.quandl.com/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc", SubscriptionTransportMedium.RemoteFile)


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
