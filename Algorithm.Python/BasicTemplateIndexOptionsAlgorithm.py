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

from QuantConnect.Algorithm import *
from QuantConnect.Data import *
from QuantConnect.Indicators import *
from QuantConnect import *


class BasicTemplateIndexOptionsAlgorithm(QCAlgorithm):
    def Initialize(self) -> None:
        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 2, 1)
        self.SetCash(1000000)

        self.spx = self.AddIndex("SPX", Resolution.Minute).Symbol
        spxOptions = self.AddIndexOption(self.spx, Resolution.Minute)
        spxOptions.SetFilter(lambda x: x.CallsOnly())

        self.emaSlow = self.EMA(self.spx, 80)
        self.emaFast = self.EMA(self.spx, 200)

    def OnData(self, data: Slice) -> None:
        if self.spx not in data.Bars or not self.emaSlow.IsReady:
            return

        for chain in data.OptionChains.Values:
            for contract in chain.Contracts.Values:
                if self.Portfolio.Invested:
                    continue

                if (self.emaFast > self.emaSlow and contract.Right == OptionRight.Call) or \
                    (self.emaFast < self.emaSlow and contract.Right == OptionRight.Put):

                    self.Liquidate(self.InvertOption(contract.Symbol))
                    self.MarketOrder(contract.Symbol, 1)

    def OnEndOfAlgorithm(self) -> None:
        if self.Portfolio[self.spx].TotalSaleVolume > 0:
            raise Exception("Index is not tradable.")

        if self.Portfolio.TotalSaleVolume == 0:
            raise Exception("Trade volume should be greater than zero by the end of this algorithm")

    def InvertOption(self, symbol: Symbol) -> Symbol:
        return Symbol.CreateOption(
            symbol.Underlying,
            symbol.ID.Market,
            symbol.ID.OptionStyle,
            OptionRight.Put if symbol.ID.OptionRight == OptionRight.Call else OptionRight.Call,
            symbol.ID.StrikePrice,
            symbol.ID.Date
        )