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
from datetime import datetime, timedelta

### <summary>
### Options Open Interest data regression test.
### </summary>
### <meta name="tag" content="options" />
### <meta name="tag" content="regression test" />
class OptionOpenInterestRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(1000000)
        self.SetStartDate(2014,6,5)
        self.SetEndDate(2014,6,6)

        option = self.AddOption("TWX")

        # set our strike/expiry filter for this option chain
        option.SetFilter(-10, 10, timedelta(0), timedelta(365*2))

        # use the underlying equity as the benchmark
        self.SetBenchmark("TWX")

    def OnData(self, slice):
        if not self.Portfolio.Invested: 
            for chain in slice.OptionChains:
                for contract in chain.Value:
                    if float(contract.Symbol.ID.StrikePrice) == 72.5 and \
                       contract.Symbol.ID.OptionRight == OptionRight.Call and \
                       contract.Symbol.ID.Date == datetime(2016, 1, 15):
                        if slice.Time.date() == datetime(2014, 6, 5).date() and contract.OpenInterest != 50:
                            raise ValueError("Regression test failed: current open interest was not correctly loaded and is not equal to 50")  
                        if slice.Time.date() == datetime(2014, 6, 6).date() and contract.OpenInterest != 70:
                            raise ValueError("Regression test failed: current open interest was not correctly loaded and is not equal to 70")  
                        if slice.Time.date() == datetime(2014, 6, 6).date():
                            self.MarketOrder(contract.Symbol, 1)
                            self.MarketOnCloseOrder(contract.Symbol, -1)

    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))