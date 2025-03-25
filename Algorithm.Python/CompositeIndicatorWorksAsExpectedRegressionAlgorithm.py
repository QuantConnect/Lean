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

### <summary>
### This algorithm tests the functionality of the CompositeIndicator
### using either a lambda expression or a method reference.
### </summary>
class CompositeIndicatorWorksAsExpectedRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 4)
        self.set_end_date(2013, 10, 5)
        self.add_equity("SPY", Resolution.MINUTE)
        close  = self.identity("SPY", Resolution.MINUTE, Field.CLOSE)
        low = self.min("SPY", 420, Resolution.MINUTE, Field.LOW)
        self.composite_min_direct = CompositeIndicator("CompositeMinDirect", close, low, lambda l, r: IndicatorResult(min(l.current.value, r.current.value)))
        self.composite_min_method = CompositeIndicator("CompositeMinMethod", close, low, self.composer)

        self.data_received = False

    def composer(self, l, r):
        return IndicatorResult(min(l.current.value, r.current.value))

    def on_data(self, data):
        self.data_received = True
        if self.composite_min_direct.current.value != self.composite_min_method.current.value:
            raise AssertionError(f"Values of indicators differ: {self.composite_min_direct.current.value} | {self.composite_min_method.current.value}")
        
    def on_end_of_algorithm(self):
        if not self.data_received:
            raise AssertionError("No data was processed during the algorithm execution.")
