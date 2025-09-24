# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
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
###  This algorithm sends a list of portfolio targets to vBsase API
### </summary>
class VBaseSignalExportDemonstrationAlgorithm(QCAlgorithm):

    def initialize(self):
        ''' Initialize the date'''
        self.set_start_date(2013,10, 7)
        self.set_end_date(2013,10,11)
        self.set_cash(100000)             # Set Strategy Cash

        self.vbase_apikey = "YOUR VBASE API KEY"
        self.vbase_collection_name = "YOUR VBASE COLLECTION NAME"

        self._symbols = [
            Symbol.create("SPY", SecurityType.EQUITY, Market.USA)
        ]

        for symbol in self._symbols:
            self.add_equity(symbol)

        self._sentSignal = False
        self.signal_export.add_signal_export_provider(VBaseSignalExport(self.vbase_apikey, self.vbase_collection_name))

    def on_data(self, data):
        if self._sentSignal:
            return
        self._sentSignal = True

        self.targets = []
        for symbol in self._symbols:
            self.targets.append(PortfolioTarget(symbol, 0.25))
        self.signal_export.set_target_portfolio(self.targets)
