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
AddReference("System.Collections")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from System.Collections.Generic import List
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *
from datetime import datetime


class CustomDataRegressionAlgorithm(QCAlgorithm):
    
    ''' Regression algorithm for custom data '''

    def Initialize(self):
        
        self.SetStartDate(2014,04,01)  #Set Start Date
        self.SetEndDate(2015,04,30)    #Set End Date
        self.SetCash(50000)            #Set Strategy Cash
        
        self.AddData[Bitcoin]("BTC", Resolution.Daily)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            if data['BTC'].Close != 0 :
                self.Order('BTC', self.Portfolio.MarginRemaining/abs(data['BTC'].Close + 1))