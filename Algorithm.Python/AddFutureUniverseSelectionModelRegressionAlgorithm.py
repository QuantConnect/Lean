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
### This example demonstrates how to use the FutureUniverseSelectionModel to select futures contracts for a given underlying asset.
### The model is set to update daily, and the algorithm ensures that the selected contracts meet specific criteria.
### This also includes a check to ensure that only future contracts are added to the algorithm's universe.
### </summary>
class AddFutureUniverseSelectionModelRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 8)
        self.set_end_date(2013, 10, 10)
        
        self.set_universe_selection(FutureUniverseSelectionModel(
            timedelta(days=1),
            lambda time: [ Symbol.create(Futures.Indices.SP_500_E_MINI, SecurityType.FUTURE, Market.CME) ]
        ))
    
    def on_securities_changed(self, changes):
        if len(changes.added_securities) > 0:
            for security in changes.added_securities:
                if security.symbol.security_type != SecurityType.FUTURE:
                    raise RegressionTestException(f"Expected future security, but found '{security.symbol.security_type}'")
                if security.symbol.id.symbol != "ES":
                    raise RegressionTestException(f"Expected future symbol 'ES', but found '{security.symbol.id.symbol}'")

    def on_end_of_algorithm(self):
        if len(self.active_securities) == 0:
            raise RegressionTestException("No active securities found. Expected at least one active security")