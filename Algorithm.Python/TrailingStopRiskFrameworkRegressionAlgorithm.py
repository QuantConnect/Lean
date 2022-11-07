### QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
### Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
###
### Licensed under the Apache License, Version 2.0 (the "License");
### you may not use this file except in compliance with the License.
### You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
###
### Unless required by applicable law or agreed to in writing, software
### distributed under the License is distributed on an "AS IS" BASIS,
### WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
### See the License for the specific language governing permissions and
### limitations under the License.

from AlgorithmImports import *

### <summary>
### Regression algorithm which tests that a trailing stop liquidates and restarts correctly
### </summary>
class TrailingStopRiskFrameworkRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2014, 6, 5) 
        self.SetEndDate(2014, 6, 9)  
        self.SetCash(100000) 
        self.AddEquity("AAPL")
        self.AddRiskManagement(TrailingStopRiskManagementModel(0.01))

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("AAPL", 1)
