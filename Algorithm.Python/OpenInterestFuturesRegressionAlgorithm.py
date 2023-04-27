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
### Futures framework algorithm that uses open interest to select the active contract.
### </summary>
### <meta name="tag" content="regression test" />
### <meta name="tag" content="futures" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="filter selection" />
class OpenInterestFuturesRegressionAlgorithm(QCAlgorithm):
    expected_expiry_dates = {datetime(2013, 12, 27), datetime(2014,2,26)}
    def Initialize(self):
        self.UniverseSettings.Resolution = Resolution.Tick
        self.SetStartDate(2013,10,8)
        self.SetEndDate(2013,10,11)
        self.SetCash(10000000)

        # set framework models
        universe = OpenInterestFutureUniverseSelectionModel(self, lambda date_time: [Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX)], None, len(self.expected_expiry_dates))
        self.SetUniverseSelection(universe)
    
    def OnData(self,data):
        if self.Transactions.OrdersCount == 0 and data.HasData:
            matched = list(filter(lambda s: not (s.ID.Date in self.expected_expiry_dates), data.Keys))
            if len(matched) != 0:
                raise Exception(f"{len(matched)}/{len(slice.Keys)} were unexpected expiry date(s): " + ", ".join(list(map(lambda x: x.ID.Date, matched))))

            for symbol in data.Keys:
                self.MarketOrder(symbol, 1)
        elif any(p.Value.Invested for p in self.Portfolio):
            self.Liquidate()
