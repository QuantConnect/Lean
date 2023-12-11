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
### Regression algorithm asserting the behavior of specifying a null position group allowing us to fill orders which would be invalid if not
### </summary>
class NullMarginMultipleOrdersRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(10000)

        # override security position group model
        self.Portfolio.SetPositions(SecurityPositionGroupModel.Null)
        # override margin requirements
        self.SetSecurityInitializer(lambda security: security.SetBuyingPowerModel(ConstantBuyingPowerModel(1)))

        equity = self.AddEquity("GOOG", leverage=4, fillForward=True)
        option = self.AddOption(equity.Symbol, fillForward=True)
        self._optionSymbol = option.Symbol

        option.SetFilter(lambda u: u.Dynamic().Strikes(-2, +2).Expiration(0, 180))

    def OnData(self, data: Slice):
        if not self.Portfolio.Invested:
            if self.IsMarketOpen(self._optionSymbol):
                chain = data.OptionChains.GetValue(self._optionSymbol)
                if chain is not None:
                    callContracts = [contract for contract in chain if contract.Right == OptionRight.Call]
                    callContracts.sort(key=lambda x: (x.Expiry, 1/ x.Strike), reverse=True)

                    optionContract = callContracts[0]
                    self.MarketOrder(optionContract.Symbol.Underlying, 1000)
                    self.MarketOrder(optionContract.Symbol, -10)

                    if self.Portfolio.TotalMarginUsed != 1010:
                        raise ValueError(f"Unexpected margin used {self.Portfolio.TotalMarginUsed}")
