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

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data.Custom.PsychSignal import PsychSignalSentiment

### <summary>
### This example algorithm shows how to import and use psychsignal sentiment data
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="psychsignal" />
### <meta name="tag" content="sentiment" />
class PsychSignalSentimentRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialize the algorithm with our custom data'''

        self.SetStartDate(2019, 6, 3)
        self.SetEndDate(2019, 6, 9)
        self.SetCash(100000)

        self.ticker = "AAPL"

        self.symbol = self.AddEquity(self.ticker).Symbol
        self.AddData(PsychSignalSentiment, self.ticker)

    def OnData(self, slice):
        '''Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment'''

        for message in slice.Values:
            # Price data can be lumped in with the values. We only want to work with
            # sentiment data, so we filter out any TradeBars that might make their way in here
            if not isinstance(message, PsychSignalSentiment):
                return

            if not self.Portfolio.Invested and len(self.Transactions.GetOpenOrders()) == 0 and slice.ContainsKey(self.symbol) and message.BullIntensity > 1.5 and message.BullScoredMessages > 3.0:
                self.SetHoldings(self.symbol, 0.25)

            elif self.Portfolio.Invested and message.BearIntensity > 1.5 and message.BearScoredMessages > 3.0:
                self.Liquidate(self.symbol)
