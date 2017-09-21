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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
import numpy as np
from datetime import timedelta
from math import floor, ceil

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
        self.SetStartDate(2017, 04, 01)
        self.SetEndDate(2017, 06, 30)
        self.SetCash(100000)
        equity = self.AddEquity("GOOG", Resolution.Minute)
        self.underlyingsymbol = equity.Symbol
        # use the underlying equity GOOG as the benchmark
        self.SetBenchmark(equity.Symbol)

    def OnData(self,slice):

        ''' OptionChainProvider gets the option chain provider,
            used to get the list of option contracts for an underlying symbol.
            Then you can manually filter the contract list returned by GetOptionContractList.
            The manual filtering will be limited to the information
            included in the Symbol (strike, expiration, type, style) and/or prices from a History call '''

        if not self.Portfolio.Invested:
            contracts = self.OptionChainProvider.GetOptionContractList(self.underlyingsymbol, self.Time.date())
            self.TradeOptions(contracts)


    def TradeOptions(self,contracts):
        # run CoarseSelection method and get a list of contracts expire within 30 to 60 days from now on
        # and the strike price between rank -5 to rank 5
        filtered_contracts = self.CoarseSelection(self.underlyingsymbol, contracts, -5, 5, 30, 60)
        expiry = sorted(filtered_contracts,key = lambda x: x.ID.Date, reverse=True)[0].ID.Date
        # filter the call options from the contracts expire on that date
        call = [i for i in filtered_contracts if i.ID.Date == expiry and i.ID.OptionRight == 0]
        # sorted the contracts according to their strike prices
        call_contracts = sorted(call,key = lambda x: x.ID.StrikePrice)
        self.call = call_contracts[0]
        for i in filtered_contracts:
            if i.ID.Date == expiry and i.ID.OptionRight == 1 and i.ID.StrikePrice ==call_contracts[0].ID.StrikePrice:
                self.put = i

        ''' Before trading the specific contract, you need to add this option contract
            AddOptionContract starts a subscription for the requested contract symbol '''

        self.AddOptionContract(self.call, Resolution.Minute)
        self.AddOptionContract(self.put, Resolution.Minute)

        self.Buy(self.call.Value ,1)
        self.Buy(self.put.Value ,1)

    def CoarseSelection(self, underlyingsymbol, symbol_list, min_strike_rank, max_strike_rank, min_expiry, max_expiry):

        ''' This method implements the coarse selection of option contracts
            according to the range of strike price and the expiration date,
            this function will help you better choose the options of different moneyness '''

        # fitler the contracts based on the expiry range
        contract_list = [i for i in symbol_list if min_expiry < (i.ID.Date.date() - self.Time.date()).days < max_expiry]
        # find the strike price of ATM option
        atm_strike = sorted(contract_list,
                            key = lambda x: abs(x.ID.StrikePrice - self.Securities[underlyingsymbol].Price))[0].ID.StrikePrice
        strike_list = sorted(set([i.ID.StrikePrice for i in contract_list]))
        # find the index of ATM strike in the sorted strike list
        atm_strike_rank = strike_list.index(atm_strike)
        min_strike = strike_list[atm_strike_rank + min_strike_rank]
        max_strike = strike_list[atm_strike_rank + max_strike_rank]
        # filter the contracts based on the range of the strike price rank
        filtered_contracts = [i for i in contract_list if i.ID.StrikePrice >= min_strike and i.ID.StrikePrice <= max_strike]

        return filtered_contracts