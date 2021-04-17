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
# limitations under the License

from datetime import datetime, timedelta
from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Securities import *
from QuantConnect.Securities.Option import *
from QuantConnect.Securities.Volatility import *
from QuantConnect import *

### <summary>
### This regression algorithm tests In The Money (ITM) index option expiry for calls.
### We test to make sure that index options have greeks enabled, same as equity options.
### </summary>
class IndexOptionCallITMGreeksExpiryRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.onDataCalls = 0
        self.invested = False

        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 1, 31)

        spx = self.AddIndex("SPX", Resolution.Minute)
        spx.VolatilityModel = StandardDeviationOfReturnsVolatilityModel(60, Resolution.Minute, timedelta(minutes=1))
        self.spx = spx.Symbol

        # Select a index option call expiring ITM, and adds it to the algorithm.
        self.spxOption = list(self.OptionChainProvider.GetOptionContractList(self.spx, self.Time))
        self.spxOption = [i for i in self.spxOption if i.ID.StrikePrice <= 3200 and i.ID.OptionRight == OptionRight.Call and i.ID.Date.year == 2021 and i.ID.Date.month == 1]
        self.spxOption = list(sorted(self.spxOption, key=lambda x: x.ID.StrikePrice, reverse=True))[0]
        self.spxOption = self.AddIndexOptionContract(self.spxOption, Resolution.Minute)

        self.spxOption.PriceModel = OptionPriceModels.BlackScholes()

        self.expectedOptionContract = Symbol.CreateOption(self.spx, Market.USA, OptionStyle.European, OptionRight.Call, 3200, datetime(2021, 1, 15))
        if self.spxOption.Symbol != self.expectedOptionContract:
            raise Exception(f"Contract {self.expectedOptionContract} was not found in the chain")

    def OnData(self, data: Slice):
        # Let the algo warmup, but without using SetWarmup. Otherwise, we get
        # no contracts in the option chain
        if self.invested or self.onDataCalls < 40:
            self.onDataCalls += 1
            return

        self.onDataCalls += 1

        if data.OptionChains.Count == 0:
            return

        if all([any([c.Symbol not in data for c in o.Contracts.Values]) for o in data.OptionChains.Values]):
            return

        if len(list(list(data.OptionChains.Values)[0].Contracts.Values)) == 0:
            raise Exception(f"No contracts found in the option {list(data.OptionChains.Keys)[0]}")

        deltas = [i.Greeks.Delta for i in self.SortByMaxVolume(data)]
        gammas = [i.Greeks.Gamma for i in self.SortByMaxVolume(data)] #data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Gamma).ToList()
        lambda_ = [i.Greeks.Lambda for i in self.SortByMaxVolume(data)] #data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Lambda).ToList()
        rho = [i.Greeks.Rho for i in self.SortByMaxVolume(data)] #data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Rho).ToList()
        theta = [i.Greeks.Theta for i in self.SortByMaxVolume(data)] #data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Theta).ToList()
        vega = [i.Greeks.Vega for i in self.SortByMaxVolume(data)] #data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Vega).ToList()

        # The commented out test cases all return zero.
        # This is because of failure to evaluate the greeks in the option pricing model, most likely
        # due to us not clearing the default 30 day requirement for the volatility model to start being updated.
        if any([i for i in deltas if i == 0]):
            raise Exception("Option contract Delta was equal to zero")

        # Delta is 1, therefore we expect a gamma of 0
        if any([i for i in gammas if i == 0]):
            raise AggregateException("Option contract Gamma was equal to zero")

        if any([i for i in lambda_ if lambda_ == 0]):
            raise AggregateException("Option contract Lambda was equal to zero")

        if any([i for i in rho if i == 0]):
            raise Exception("Option contract Rho was equal to zero")
        
        if any([i for i in theta if i == 0]):
            raise Exception("Option contract Theta was equal to zero")

        # The strike is far away from the underlying asset's price, and we're very close to expiry.
        # Zero is an expected value here.
        if any([i for i in vega if vega == 0]):
            raise AggregateException("Option contract Vega was equal to zero")

        if not self.invested:
            self.SetHoldings(list(list(data.OptionChains.Values)[0].Contracts.Values)[0].Symbol, 1)
            self.invested = True

    ### <summary>
    ### Ran at the end of the algorithm to ensure the algorithm has no holdings
    ### </summary>
    ### <exception cref="Exception">The algorithm has holdings</exception>
    def OnEndOfAlgorithm(self):
        if self.Portfolio.Invested:
            raise Exception(f"Expected no holdings at end of algorithm, but are invested in: {', '.join(self.Portfolio.Keys)}")

        if not self.invested:
            raise Exception(f"Never checked greeks, maybe we have no option data?")

    def SortByMaxVolume(self, data: Slice):
        chain = [i for i in sorted(list(data.OptionChains.Values), key=lambda x: sum([j.Volume for j in x.Contracts.Values]), reverse=True)][0]
        return chain.Contracts.Values
