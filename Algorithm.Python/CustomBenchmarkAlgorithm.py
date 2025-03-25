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
### Shows how to set a custom benchmark for you algorithms
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="benchmarks" />
class CustomBenchmarkAlgorithm(QCAlgorithm):

    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2013,10,7)   #Set Start Date
        self.set_end_date(2013,10,11)    #Set End Date
        self.set_cash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.add_equity("SPY", Resolution.SECOND)
        
        # Disabling the benchmark / setting to a fixed value 
        # self.set_benchmark(lambda x: 0)
        
        # Set the benchmark to AAPL US Equity
        self.set_benchmark(Symbol.create("AAPL", SecurityType.EQUITY, Market.USA))

    def on_data(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if not self.portfolio.invested:
            self.set_holdings("SPY", 1)
            self.debug("Purchased Stock")

        tuple_result = SymbolCache.try_get_symbol("AAPL", None)
        if tuple_result[0]:
            raise AssertionError("Benchmark Symbol is not expected to be added to the Symbol cache")
