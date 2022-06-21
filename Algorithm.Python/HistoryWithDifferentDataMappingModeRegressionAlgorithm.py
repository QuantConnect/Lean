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
from System import Enum

### <summary>
### Regression algorithm excersizing an equity covered European style option, using an option price model
### that supports European style options and asserting that the option price model is used.
### </summary>
class HistoryWithDifferentDataMappingModeRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 8)
        self._futureSymbol = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Daily).Symbol

    def OnEndOfAlgorithm(self):
        dataMappingModes = [DataMappingMode(x) for x in Enum.GetValues(DataMappingMode)]
        historyResults = [
            self.History([self._futureSymbol], self.StartDate, self.EndDate, Resolution.Hour, dataMappingMode=x)
                .droplevel(0, axis=0)
                .loc[self._futureSymbol
                ].close
            for x in dataMappingModes
        ]

        if any(x.size != historyResults[0].size for x in historyResults):
            raise Exception("History results bar count did not match")

        # Check that close prices at each time are different for different data mapping modes
        for time, close in historyResults[0].items():
            if any(result[time] == close for result in historyResults[-(len(historyResults) - 1):]):
                raise Exception(f"History() returned equal close prices for different data mapping modes at time {time}")
