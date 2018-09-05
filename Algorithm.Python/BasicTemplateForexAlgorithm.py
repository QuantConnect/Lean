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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Indicators import *
from QCAlgorithm import QCAlgorithm
from datetime import timedelta
import numpy as np

### <summary>
### Algorithm demonstrating FOREX asset types and requesting history on them in bulk. As FOREX uses
### QuoteBars you should request slices or
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="history and warm up" />
### <meta name="tag" content="history" />
### <meta name="tag" content="forex" />
class BasicTemplateForexAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Set the cash we'd like to use for our backtest
        self.SetCash(100000)

        # Start and end dates for the backtest.
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)

        # Add FOREX contract you want to trade
        # find available contracts here https://www.quantconnect.com/data#forex/oanda/cfd
        self.AddForex("EURUSD", Resolution.Minute)
        self.AddForex("GBPUSD", Resolution.Minute)
        self.AddForex("EURGBP", Resolution.Minute)

        self.History(5, Resolution.Daily)
        self.History(5, Resolution.Hour)
        self.History(5, Resolution.Minute)

        history = self.History(TimeSpan.FromSeconds(5), Resolution.Second)

        for data in sorted(history, key=lambda x: x.Time):
            for key in data.Keys:
                self.Log(str(key.Value) + ": " + str(data.Time) + " > " + str(data[key].Value))

    def OnData(self, data):
        # Print to console to verify that data is coming in
        for key in data.Keys:
            self.Log(str(key.Value) + ": " + str(data.Time) + " > " + str(data[key].Value))