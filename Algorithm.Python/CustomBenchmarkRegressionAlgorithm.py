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
from CustomBrokerageModelRegressionAlgorithm import CustomBrokerageModel

### <summary>
### Regression algorithm to test we can specify a custom benchmark model, and override some of its methods
### </summary>
class CustomBenchmarkRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetBrokerageModel(CustomBrokerageModelWithCustomBenchmark())
        self.AddEquity("SPY", Resolution.Daily)
        self.updateRequestSubmitted = False

    def OnData(self, slice):
        if (not self.Portfolio.Invested) and self.Benchmark.Evaluate(self.Time) != 0:
            self.SetHoldings("SPY", 1)

class CustomBenchmark:
    def Evaluate(self, time):
        if time.day % 2 == 0:
            return 0
        else:
            return 1

class CustomBrokerageModelWithCustomBenchmark(CustomBrokerageModel):
    def GetBenchmark(self, securities):
        return CustomBenchmark()
