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
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from datetime import timedelta

### <summary>
### Example demonstrating how to access to options history for a given underlying equity security.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
### <meta name="tag" content="history" />
class BasicTemplateOptionsHistoryAlgorithm(QCAlgorithm):

    def Initialize(self):
        # this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
        self.SetStartDate(2015, 11, 24)
        self.SetEndDate(2016, 12, 24)
        self.SetCash(1000000)

        equity = self.AddEquity("GOOG")
        option = self.AddOption("GOOG")
        self.underlying = option.Symbol

        equity.SetDataNormalizationMode(DataNormalizationMode.Raw)

        option.SetFilter(-2,2, timedelta(0), timedelta(180))
        self.SetBenchmark(equity.Symbol)

    def OnData(self,slice):
        if not self.Portfolio.Invested:
            for chain in slice.OptionChains:
                for contract in chain.Value:
                    self.Log("{0},Bid={1} Ask={2} Last={3} OI={4} OI={4} sigma={5:0.000} NPV={6:0.000} delta={7:0.000} gamma={8:0.000} vega={9:0.000} beta={10:0.00} theta={11:0.00} IV={12:0.000}".format(
                    contract.Symbol.Value,
                    contract.BidPrice,
                    contract.AskPrice,
                    contract.LastPrice,
                    contract.OpenInterest,
                    self.underlying.VolatilityModel.Volatility,
                    contract.TheoreticalPrice,
                    contract.Greeks.Delta,
                    contract.Greeks.Gamma,
                    contract.Greeks.Vega,
                    contract.Greeks.Rho,
                    contract.Greeks.Theta / 365.0,
                    contract.ImpliedVolatility))

    def OnOrderEvent(self, orderEvent):
        # Order fill event handler. On an order fill update the resulting information is passed to this method.
        # Order event details containing details of the events
        self.Log(str(orderEvent))

    def OnSecuritiesChanged(self, changes):
        if changes == SecurityChanges.None: return
        for change in changes.AddedSecurities:
            history = self.History(change.Symbol, 10, Resolution.Minute)
            history = history.sortlevel(['time'], ascending=False)[:3]

            self.Log("History: " + str(history.index.get_level_values('symbol').values[0])
                        + ": " + str(history.index.get_level_values('time').values[0])
                        + " > " + str(history['close'].values))