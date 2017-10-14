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


### <summary>
### Regression test for consistency of hour data over a reverse split event in US equities.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="regression test" />
class HourSplitRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2005, 2, 25)
        self. SetEndDate(2005, 2, 28)
        self.SetCash(100000)
        self.SetBenchmark(lambda x: 0)

        self.symbol = self.AddEquity("AAPL", Resolution.Hour).Symbol
    
    def OnData(self, slice):
        if slice.Bars.Count == 0: return
        if (not self.Portfolio.Invested) and self.Time.date() == self.EndDate.date():
            self.Buy(self.symbol, 1)