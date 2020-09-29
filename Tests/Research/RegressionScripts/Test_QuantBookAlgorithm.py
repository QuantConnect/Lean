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
AddReference("QuantConnect.Research")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Configuration")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Custom import *
from QuantConnect.Data.Market import TradeBar, QuoteBar
from QuantConnect.Research import *
from QuantConnect.Indicators import *
from QuantConnect.Configuration import *
from PythonQuantBook import *

class PythonQB(PythonQuantBook):

    #If you overwrite the __init__ you must call super!!
    def __init__(self):
        super().__init__()
        self.InitializeCalled = False
        self.OnDataCalled = False

    def Initialize(self):
        self.InitializeCalled = True
        self.SetStartDate(2013,10, 7)  #Set Start Date
        self.SetEndDate(2013,10,8)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        self.AddEquity("SPY", Resolution.Minute)

    def OnData(self, data):
        self.OnDataCalled = True;
        for val in data.Values:
            self.Debug(val.ToString());
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

    def OnOrderEvent(self, orderEvent):
        self.Debug("{} {}".format(self.Time, orderEvent.ToString()))

    def OnEndOfAlgorithm(self):
        self.Debug("End of Algorithm in Python CustomQuantBook")
        self.Debug("{} - TotalPortfolioValue: {}".format(self.Time, self.Portfolio.TotalPortfolioValue))
        self.Debug("{} - CashBook: {}".format(self.Time, self.Portfolio.CashBook))
        
    def OnEndOfDay(symbol):
        self.Debug("End of day for {} in Python CustomQuantBook")
