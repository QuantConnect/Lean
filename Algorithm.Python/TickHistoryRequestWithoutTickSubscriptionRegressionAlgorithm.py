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

from datetime import timedelta
from AlgorithmImports import *

### <summary>
### Regression algorithm asserting that historical data can be requested with tick resolution without requiring
### a tick resolution subscription
### </summary>
class TickHistoryRequestWithoutTickSubscriptionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 8)

        # Subscribing SPY and IBM with daily and hour resolution instead of tick resolution
        spy = self.AddEquity("SPY", Resolution.Daily).Symbol
        ibm = self.AddEquity("IBM", Resolution.Hour).Symbol

        # Requesting history for SPY and IBM (separately) with tick resolution
        spyHistory = self.History[Tick](spy, timedelta(days=1), Resolution.Tick)
        if len(list(spyHistory)) == 0:
            raise Exception("SPY tick history is empty")

        ibmHistory = self.History[Tick](ibm, timedelta(days=1), Resolution.Tick)
        if len(list(ibmHistory)) == 0:
            raise Exception("IBM tick history is empty")

        # Requesting history for SPY and IBM (together) with tick resolution
        spyIbmHistory = self.History[Tick]([spy, ibm], timedelta(days=1), Resolution.Tick)
        if len(list(spyIbmHistory)) == 0:
            raise Exception("Compound SPY and IBM tick history is empty")

        self.Quit()
