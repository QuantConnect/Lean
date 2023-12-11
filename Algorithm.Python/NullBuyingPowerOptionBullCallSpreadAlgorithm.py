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
### Shows how setting to use the SecurityMarginModel.Null (or BuyingPowerModel.Null)
### to disable the sufficient margin call verification.
### See also: <see cref="OptionEquityBullCallSpreadRegressionAlgorithm"/>
### </summary>
### <meta name="tag" content="reality model" />
class NullBuyingPowerOptionBullCallSpreadAlgorithm(QCAlgorithm):
    def Initialize(self):

        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(200000)

        self.SetSecurityInitializer(lambda security: security.SetMarginModel(SecurityMarginModel.Null))
        self.Portfolio.SetPositions(SecurityPositionGroupModel.Null);

        equity = self.AddEquity("GOOG")
        option = self.AddOption(equity.Symbol)
        self.optionSymbol = option.Symbol

        option.SetFilter(-2, 2, 0, 180, True)

    def OnData(self, slice):
        if self.Portfolio.Invested or not self.IsMarketOpen(self.optionSymbol):
            return

        chain = slice.OptionChains.get(self.optionSymbol)
        if chain:
            call_contracts = [x for x in chain if x.Right == OptionRight.Call]

            expiry = min(x.Expiry for x in call_contracts)

            call_contracts = sorted([x for x in call_contracts if x.Expiry == expiry],
                key = lambda x: x.Strike)

            long_call = call_contracts[0]
            short_call = [x for x in call_contracts if x.Strike > long_call.Strike][0]

            quantity = 1000

            tickets = [
                self.MarketOrder(short_call.Symbol, -quantity),
                self.MarketOrder(long_call.Symbol, quantity)
            ]

            for ticket in tickets:
                if ticket.Status != OrderStatus.Filled:
                    raise Exception(f"There should be no restriction on buying {ticket.Quantity} of {ticket.Symbol} with BuyingPowerModel.Null")


    def OnEndOfAlgorithm(self) -> None:
        if self.Portfolio.TotalMarginUsed != 0:
            raise Exception("The TotalMarginUsed should be zero to avoid margin calls.")
