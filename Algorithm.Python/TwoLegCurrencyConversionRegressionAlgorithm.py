### QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
### Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
###
### Licensed under the Apache License, Version 2.0 (the "License");
### you may not use this file except in compliance with the License.
### You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
###
### Unless required by applicable law or agreed to in writing, software
### distributed under the License is distributed on an "AS IS" BASIS,
### WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
### See the License for the specific language governing permissions and
### limitations under the License.

from AlgorithmImports import *

### <summary>
### Regression algorithm which tests that a two leg currency conversion happens correctly
### </summary>
class TwoLegCurrencyConversionRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2018, 4, 4)
        self.SetEndDate(2018, 4, 4)
        self.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash)
        # GDAX doesn't have LTCETH or ETHLTC, but they do have ETHUSD and LTCUSD to form a path between ETH and LTC
        self.SetAccountCurrency("ETH")
        self.SetCash("ETH", 100000)
        self.SetCash("LTC", 100000)
        self.SetCash("USD", 100000)

        self._ethUsdSymbol = self.AddCrypto("ETHUSD", Resolution.Minute).Symbol
        self._ltcUsdSymbol = self.AddCrypto("LTCUSD", Resolution.Minute).Symbol

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.MarketOrder(self._ltcUsdSymbol, 1)

    def OnEndOfAlgorithm(self):
        ltcCash = self.Portfolio.CashBook["LTC"]

        conversionSymbols = [x.Symbol for x in ltcCash.CurrencyConversion.ConversionRateSecurities]

        if len(conversionSymbols) != 2:
            raise ValueError(
                f"Expected two conversion rate securities for LTC to ETH, is {len(conversionSymbols)}")

        if conversionSymbols[0] != self._ltcUsdSymbol:
            raise ValueError(
                f"Expected first conversion rate security from LTC to ETH to be {self._ltcUsdSymbol}, is {conversionSymbols[0]}")

        if conversionSymbols[1] != self._ethUsdSymbol:
            raise ValueError(
                f"Expected second conversion rate security from LTC to ETH to be {self._ethUsdSymbol}, is {conversionSymbols[1]}")

        ltcUsdValue = self.Securities[self._ltcUsdSymbol].GetLastData().Value
        ethUsdValue = self.Securities[self._ethUsdSymbol].GetLastData().Value

        expectedConversionRate = ltcUsdValue / ethUsdValue
        actualConversionRate = ltcCash.ConversionRate

        if actualConversionRate != expectedConversionRate:
            raise ValueError(
                f"Expected conversion rate from LTC to ETH to be {expectedConversionRate}, is {actualConversionRate}")
