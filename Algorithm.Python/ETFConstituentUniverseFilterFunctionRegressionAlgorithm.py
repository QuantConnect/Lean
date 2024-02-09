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
### Tests a custom filter function when creating an ETF constituents universe for SPY
### </summary>
class ETFConstituentUniverseFilterFunctionRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2020, 12, 1)
        self.SetEndDate(2021, 1, 31)
        self.SetCash(100000)

        self.filtered = False
        self.securitiesChanged = False
        self.receivedData = False
        self.etfConstituentData = {}
        self.etfRebalanced = False
        self.rebalanceCount = 0
        self.rebalanceAssetCount = 0

        self.UniverseSettings.Resolution = Resolution.Hour

        self.spy = self.AddEquity("SPY", Resolution.Hour).Symbol
        self.aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)

        self.AddUniverse(self.Universe.ETF(self.spy, self.UniverseSettings, self.FilterETFs))

    def FilterETFs(self, constituents):
        constituentsData = list(constituents)
        constituentsSymbols = [i.Symbol for i in constituentsData]
        self.etfConstituentData = {i.Symbol: i for i in constituentsData}

        if len(constituentsData) == 0:
            raise Exception(f"Constituents collection is empty on {self.UtcTime.strftime('%Y-%m-%d %H:%M:%S.%f')}")
        if self.aapl not in constituentsSymbols:
            raise Exception("AAPL is not int he constituents data provided to the algorithm")

        aaplData = [i for i in constituentsData if i.Symbol == self.aapl][0]
        if aaplData.Weight == 0.0:
            raise Exception("AAPL weight is expected to be a non-zero value")

        self.filtered = True
        self.etfRebalanced = True

        return constituentsSymbols

    def OnData(self, data):
        if not self.filtered and len(data.Bars) != 0 and self.aapl in data.Bars:
            raise Exception("AAPL TradeBar data added to algorithm before constituent universe selection took place")

        if len(data.Bars) == 1 and self.spy in data.Bars:
            return

        if len(data.Bars) != 0 and self.aapl not in data.Bars:
            raise Exception(f"Expected AAPL TradeBar data on {self.UtcTime.strftime('%Y-%m-%d %H:%M:%S.%f')}")

        self.receivedData = True

        if not self.etfRebalanced:
            return

        for bar in data.Bars.Values:
            constituentData = self.etfConstituentData.get(bar.Symbol)
            if constituentData is not None and constituentData.Weight is not None and constituentData.Weight >= 0.0001:
                # If the weight of the constituent is less than 1%, then it will be set to 1%
                # If the weight of the constituent exceeds more than 5%, then it will be capped to 5%
                # Otherwise, if the weight falls in between, then we use that value.
                boundedWeight = max(0.01, min(constituentData.Weight, 0.05))

                self.SetHoldings(bar.Symbol, boundedWeight)

                if self.etfRebalanced:
                    self.rebalanceCount += 1

                self.etfRebalanced = False
                self.rebalanceAssetCount += 1


    def OnSecuritiesChanged(self, changes):
        if self.filtered and not self.securitiesChanged and len(changes.AddedSecurities) < 500:
            raise Exception(f"Added SPY S&P 500 ETF to algorithm, but less than 500 equities were loaded (added {len(changes.AddedSecurities)} securities)")

        self.securitiesChanged = True

    def OnEndOfAlgorithm(self):
        if self.rebalanceCount != 2:
            raise Exception(f"Expected 2 rebalance, instead rebalanced: {self.rebalanceCount}")

        if self.rebalanceAssetCount != 8:
            raise Exception(f"Invested in {self.rebalanceAssetCount} assets (expected 8)")

        if not self.filtered:
            raise Exception("Universe selection was never triggered")

        if not self.securitiesChanged:
            raise Exception("Security changes never propagated to the algorithm")

        if not self.receivedData:
            raise Exception("Data was never loaded for the S&P 500 constituent AAPL")
