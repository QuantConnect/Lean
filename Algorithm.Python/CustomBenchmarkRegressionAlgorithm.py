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
    def initialize(self):
        self.set_start_date(2013,10,7)
        self.set_end_date(2013,10,11)
        self.set_brokerage_model(CustomBrokerageModelWithCustomBenchmark())
        self.add_equity("SPY", Resolution.DAILY)
        self.update_request_submitted = False

    def on_data(self, slice):
        benchmark = self.benchmark.evaluate(self.time)
        if (self.time.day % 2 == 0) and (benchmark != 1):
            raise AssertionError(f"Benchmark should be 1, but was {benchmark}")

        if (self.time.day % 2 == 1) and (benchmark != 2):
            raise AssertionError(f"Benchmark should be 2, but was {benchmark}")

class CustomBenchmark:
    def evaluate(self, time):
        if time.day % 2 == 0:
            return 1
        else:
            return 2

class CustomBrokerageModelWithCustomBenchmark(CustomBrokerageModel):
    def get_benchmark(self, securities):
        return CustomBenchmark()
