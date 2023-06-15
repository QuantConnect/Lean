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
### Demonstration of how to access the statistics results from within an algorithm through the `Statistics` property.
### </summary>
class StatisticsResultsAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 11)
        self.SetCash(100000)

        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol
        self.ibm = self.AddEquity("IBM", Resolution.Minute).Symbol

        self.fastSpyEma = self.EMA(self.spy, 30, Resolution.Minute)
        self.slowSpyEma = self.EMA(self.spy, 60, Resolution.Minute)

        self.fastIbmEma = self.EMA(self.spy, 10, Resolution.Minute)
        self.slowIbmEma = self.EMA(self.spy, 30, Resolution.Minute)

    def OnData(self, data):
        if not self.slowSpyEma.IsReady: return

        if self.fastSpyEma > self.slowSpyEma:
            self.SetHoldings(self.spy, 0.5)
        elif self.Securities[self.spy].Invested:
            self.Liquidate(self.spy)

        if self.fastIbmEma > self.slowIbmEma:
            self.SetHoldings(self.ibm, 0.2)
        elif self.Securities[self.ibm].Invested:
            self.Liquidate(self.ibm)

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            # We can access the statistics summary at runtime
            statistics = self.Statistics.Summary
            statisticsStr = "\n\t".join([f"{kvp.Key}: {kvp.Value}" for kvp in statistics])
            self.Debug(f"\nStatistics after fill:\n\t{statisticsStr}")

            # Access a single statistic
            self.Log(f"Total trades so far: {statistics[PerformanceMetrics.TotalTrades]}")
            self.Log(f"Sharpe Ratio: {statistics[PerformanceMetrics.SharpeRatio]}")
