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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Brokerages import *
from QuantConnect.Benchmarks import *
from QuantConnect.Data import *
from QuantConnect.Securities import *

### <summary>
### Regression algorithm to test zeroed benchmark through BrokerageModel override
### </summary>
### <meta name="tag" content="regression test" />
class ZeroedBenchmarkRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(100000)
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,8)

        # Add Equity
        self.AddEquity("SPY", Resolution.Hour)

        # Use our Test Brokerage Model with zerod default benchmark
        self.SetBrokerageModel(TestBrokerageModel())

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

class TestBrokerageModel(DefaultBrokerageModel):

    def GetBenchmark(self, securities):
        return FuncBenchmark(self.func)

    def func(self, datetime):
        return 0;