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

from AlgorithmImports import *
from System.Collections.Generic import List
from QuantConnect.Data.Custom.IconicTypes import *

### <summary>
### Provides an example algorithm showcasing the Security.Data features
### </summary>
class DynamicSecurityDataRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 10, 22)
        self.SetEndDate(2015, 10, 30)

        self.Ticker = "GOOGL"
        self.Equity = self.AddEquity(self.Ticker, Resolution.Daily)

        customLinkedEquity = self.AddData(LinkedData, self.Ticker, Resolution.Daily)
        
        firstLinkedData = LinkedData()
        firstLinkedData.Count = 100
        firstLinkedData.Symbol = customLinkedEquity.Symbol
        firstLinkedData.EndTime = self.StartDate
        
        secondLinkedData = LinkedData()
        secondLinkedData.Count = 100
        secondLinkedData.Symbol = customLinkedEquity.Symbol
        secondLinkedData.EndTime = self.StartDate
        
        # Adding linked data manually to cache for example purposes, since
        # LinkedData is a type used for testing and doesn't point to any real data.
        customLinkedEquityType = list(customLinkedEquity.Subscriptions)[0].Type
        customLinkedData = List[LinkedData]()
        customLinkedData.Add(firstLinkedData)
        customLinkedData.Add(secondLinkedData)
        self.Equity.Cache.AddDataList(customLinkedData, customLinkedEquityType, False) 

    def OnData(self, data):
        # The Security object's Data property provides convenient access
        # to the various types of data related to that security. You can
        # access not only the security's price data, but also any custom
        # data that is mapped to the security, such as our SEC reports.

        # 1. Get the most recent data point of a particular type:
        # 1.a Using the generic method, Get(T): => T
        customLinkedData = self.Equity.Data.Get(LinkedData)
        self.Log("{}: LinkedData: {}".format(self.Time, str(customLinkedData)))

        # 2. Get the list of data points of a particular type for the most recent time step:
        # 2.a Using the generic method, GetAll(T): => IReadOnlyList<T>
        customLinkedDataList = self.Equity.Data.GetAll(LinkedData)
        self.Log("{}: LinkedData: {}".format(self.Time, len(customLinkedDataList)))

        if not self.Portfolio.Invested:
            self.Buy(self.Equity.Symbol, 10)
