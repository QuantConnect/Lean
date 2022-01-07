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

from AlgorithmImports import *

class BasicTemplateIndexAlgorithm(QCAlgorithm):
    def Initialize(self) -> None:
        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 1, 18)
        self.SetCash(1000000)

        # Use indicator for signal; but it cannot be traded
        self.spx = self.AddIndex("SPX", Resolution.Minute).Symbol

        # Trade on SPX ITM calls
        self.spxOption = Symbol.CreateOption(
            self.spx,
            Market.USA,
            OptionStyle.European,
            OptionRight.Call,
            3200,
            datetime(2021, 1, 15)
        )

        self.AddIndexOptionContract(self.spxOption, Resolution.Minute)

        self.emaSlow = self.EMA(self.spx, 80)
        self.emaFast = self.EMA(self.spx, 200)

    def OnData(self, data: Slice):
        if self.spx not in data.Bars or self.spxOption not in data.Bars:
            return

        if not self.emaSlow.IsReady:
            return

        if self.emaFast > self.emaSlow:
            self.SetHoldings(self.spxOption, 1)
        else:
            self.Liquidate()

    def OnEndOfAlgorithm(self) -> None:
        if self.Portfolio[self.spx].TotalSaleVolume > 0:
            raise Exception("Index is not tradable.")
