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
### This example demonstrates how to use the OptionUniverseSelectionModel to select options contracts based on specified conditions.
### The model is updated daily and selects different options based on the current date.
### The algorithm ensures that only valid option contracts are selected for the universe.
### </summary>
class AddOptionUniverseSelectionModelRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2014, 6, 5)
        self.set_end_date(2014, 6, 6)
        self.option_count = 0
        
        self.universe_settings.resolution = Resolution.MINUTE
        self.set_universe_selection(OptionUniverseSelectionModel(
            timedelta(days=1),
            self.select_option_chain_symbols
        ))
    
    def select_option_chain_symbols(self, utc_time):
        new_york_time = Extensions.convert_from_utc(utc_time, TimeZones.NEW_YORK)
        if new_york_time.date() < datetime(2014, 6, 6).date():
            return [ Symbol.create("TWX", SecurityType.OPTION, Market.USA) ]
        
        if new_york_time.date() >= datetime(2014, 6, 6).date():
            return [ Symbol.create("AAPL", SecurityType.OPTION, Market.USA) ]

    def on_securities_changed(self, changes):
        if len(changes.added_securities) > 0:
            for security in changes.added_securities:
                symbol = security.symbol.underlying if security.symbol.underlying else security.Symbol
                if symbol.value != "AAPL" and symbol.value != "TWX":
                    raise RegressionTestException(f"Unexpected security {security.Symbol}")
                
                if security.symbol.security_type == SecurityType.OPTION:
                    self.option_count += 1

    def on_end_of_algorithm(self):
        if len(self.active_securities) == 0:
            raise RegressionTestException("No active securities found. Expected at least one active security")
        if self.option_count == 0:
            raise RegressionTestException("The option count should be greater than 0")