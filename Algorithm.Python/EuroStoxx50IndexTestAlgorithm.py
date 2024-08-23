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

class EuroStoxx50IndexTestAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2024, 7, 29)
        self.set_end_date(2024, 7, 30)
        self.set_cash(100000)

        self._symbol = self.add_index("SX5E", market=Market.EUREX).symbol

    def on_data(self, data):
        for symbol, bar in data.bars.items():
            self.log(f"[{symbol}] :: {bar}")
