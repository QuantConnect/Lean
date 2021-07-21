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
from QuantConnect.Data.Custom.IconicTypes import *

class CustomDataLinkedIconicTypeAddDataCoarseSelectionRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 3, 24)
        self.SetEndDate(2014, 4, 7)
        self.SetCash(100000)

        self.UniverseSettings.Resolution = Resolution.Daily

        self.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelector))

    def CoarseSelector(self, coarse):
        symbols = [
            Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            Symbol.Create("BAC", SecurityType.Equity, Market.USA),
            Symbol.Create("FB", SecurityType.Equity, Market.USA),
            Symbol.Create("GOOGL", SecurityType.Equity, Market.USA),
            Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
            Symbol.Create("IBM", SecurityType.Equity, Market.USA),
        ]

        self.customSymbols = []

        for symbol in symbols:
            self.customSymbols.append(self.AddData(LinkedData, symbol, Resolution.Daily).Symbol)

        return symbols

    def OnData(self, data):
        if not self.Portfolio.Invested and len(self.Transactions.GetOpenOrders()) == 0:
            aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
            self.SetHoldings(aapl, 0.5)

        for customSymbol in self.customSymbols:
            if not self.ActiveSecurities.ContainsKey(customSymbol.Underlying):
                raise Exception(f"Custom data undelrying ({customSymbol.Underlying}) Symbol was not found in active securities")
