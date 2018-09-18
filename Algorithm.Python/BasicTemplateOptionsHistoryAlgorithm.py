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
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Securities.Option import OptionPriceModels
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm
from datetime import timedelta
import decimal as d

### <summary>
### Example demonstrating how to access to options history for a given underlying equity security.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
### <meta name="tag" content="history" />
class BasicTemplateOptionsHistoryAlgorithm(QCAlgorithm):
    ''' This example demonstrates how to get access to options history for a given underlying equity security.'''

    def Initialize(self):
        # this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(1000000)

        option = self.AddOption("GOOG")
        # add the initial contract filter 
        option.SetFilter(-2,2, timedelta(0), timedelta(180))

        # set the pricing model for Greeks and volatility
        # find more pricing models https://www.quantconnect.com/lean/documentation/topic27704.html
        option.PriceModel = OptionPriceModels.CrankNicolsonFD()
        # set the warm-up period for the pricing model
        self.SetWarmUp(TimeSpan.FromDays(4))
        # set the benchmark to be the initial cash
        self.SetBenchmark(lambda x: 1000000)

    def OnData(self,slice):
        if self.IsWarmingUp: return
        if not self.Portfolio.Invested:
            for chain in slice.OptionChains:
                volatility = self.Securities[chain.Key.Underlying].VolatilityModel.Volatility
                for contract in chain.Value:
                    self.Log("{0},Bid={1} Ask={2} Last={3} OI={4} sigma={5:.3f} NPV={6:.3f} \
                              delta={7:.3f} gamma={8:.3f} vega={9:.3f} beta={10:.2f} theta={11:.2f} IV={12:.2f}".format(
                    contract.Symbol.Value,
                    contract.BidPrice,
                    contract.AskPrice,
                    contract.LastPrice,
                    contract.OpenInterest,
                    volatility,
                    contract.TheoreticalPrice,
                    contract.Greeks.Delta,
                    contract.Greeks.Gamma,
                    contract.Greeks.Vega,
                    contract.Greeks.Rho,
                    contract.Greeks.Theta / 365,
                    contract.ImpliedVolatility))

    def OnSecuritiesChanged(self, changes):
        for change in changes.AddedSecurities:
            # only print options price
            if change.Symbol.Value == "GOOG": return
            history = self.History(change.Symbol, 10, Resolution.Minute).sort_index(level='time', ascending=False)[:3]
            for index, row in history.iterrows():
                self.Log("History: " + str(index[3])
                        + ": " + index[4].strftime("%m/%d/%Y %I:%M:%S %p")
                        + " > " + str(row.close))