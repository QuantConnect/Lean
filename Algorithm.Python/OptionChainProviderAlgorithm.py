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
from QuantConnect.Data import *
from QCAlgorithm import QCAlgorithm
import numpy as np
from datetime import timedelta

### <summary>
### Demonstration of the Option Chain Provider -- a much faster mechanism for manually specifying the option contracts you'd like to recieve
### data for and manually subscribing to them.
### </summary>
### <meta name="tag" content="strategy example" />
### <meta name="tag" content="options" />
### <meta name="tag" content="using data" />
### <meta name="tag" content="selecting options" />
### <meta name="tag" content="manual selection" />

class OptionChainProviderAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(100000)
        # add the underlying asset
        self.equity = self.AddEquity("GOOG", Resolution.Minute)
        self.equity.SetDataNormalizationMode(DataNormalizationMode.Raw)
        # initialize the option contract with empty string
        self.contract = str()
        self.contractsAdded = set()

    def OnData(self, data):

        if not self.Portfolio[self.equity.Symbol].Invested:
            self.MarketOrder(self.equity.Symbol, 100)

        if not (self.Securities.ContainsKey(self.contract) and self.Portfolio[self.contract].Invested):
            self.contract = self.OptionsFilter(data)

        if self.Securities.ContainsKey(self.contract) and not self.Portfolio[self.contract].Invested:
            self.MarketOrder(self.contract, -1)

    def OptionsFilter(self, data):
        ''' OptionChainProvider gets a list of option contracts for an underlying symbol at requested date.
            Then you can manually filter the contract list returned by GetOptionContractList.
            The manual filtering will be limited to the information included in the Symbol
            (strike, expiration, type, style) and/or prices from a History call '''

        contracts = self.OptionChainProvider.GetOptionContractList(self.equity.Symbol, data.Time)
        self.underlyingPrice = self.Securities[self.equity.Symbol].Price
        # filter the out-of-money call options from the contract list which expire in 10 to 30 days from now on
        otm_calls = [i for i in contracts if i.ID.OptionRight == OptionRight.Call and
                                            i.ID.StrikePrice - self.underlyingPrice > 0 and
                                            10 < (i.ID.Date - data.Time).days < 30]
        if len(otm_calls) > 0:
            contract = sorted(sorted(otm_calls, key = lambda x: x.ID.Date),
                                                     key = lambda x: x.ID.StrikePrice - self.underlyingPrice)[0]
            if contract not in self.contractsAdded:
                self.contractsAdded.add(contract)
                # use AddOptionContract() to subscribe the data for specified contract
                self.AddOptionContract(contract, Resolution.Minute)
            return contract
        else:
            return str()