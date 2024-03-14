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

    MostTradedSecurityStatistic = "Most Traded Security"
    MostTradedSecurityTradeCountStatistic = "Most Traded Security Trade Count"

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

        self.trade_counts = {self.spy: 0, self.ibm: 0}

    def OnData(self, data: Slice):
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
            self.Log(f"Total trades so far: {statistics[PerformanceMetrics.TotalOrders]}")
            self.Log(f"Sharpe Ratio: {statistics[PerformanceMetrics.SharpeRatio]}")

            # --------

            # We can also set custom summary statistics:

            if all(count == 0 for count in self.trade_counts.values()):
                if StatisticsResultsAlgorithm.MostTradedSecurityStatistic in statistics:
                    raise Exception(f"Statistic {StatisticsResultsAlgorithm.MostTradedSecurityStatistic} should not be set yet")
                if StatisticsResultsAlgorithm.MostTradedSecurityTradeCountStatistic in statistics:
                    raise Exception(f"Statistic {StatisticsResultsAlgorithm.MostTradedSecurityTradeCountStatistic} should not be set yet")
            else:
                # The current most traded security should be set in the summary
                most_trade_security, most_trade_security_trade_count = self.GetMostTradeSecurity()
                self.CheckMostTradedSecurityStatistic(statistics, most_trade_security, most_trade_security_trade_count)

            # Update the trade count
            self.trade_counts[orderEvent.Symbol] += 1

            # Set the most traded security
            most_trade_security, most_trade_security_trade_count = self.GetMostTradeSecurity()
            self.SetSummaryStatistic(StatisticsResultsAlgorithm.MostTradedSecurityStatistic, most_trade_security)
            self.SetSummaryStatistic(StatisticsResultsAlgorithm.MostTradedSecurityTradeCountStatistic, most_trade_security_trade_count)

            # Re-calculate statistics:
            statistics = self.Statistics.Summary

            # Let's keep track of our custom summary statistics after the update
            self.CheckMostTradedSecurityStatistic(statistics, most_trade_security, most_trade_security_trade_count)

    def OnEndOfAlgorithm(self):
        statistics = self.Statistics.Summary
        if StatisticsResultsAlgorithm.MostTradedSecurityStatistic not in statistics:
            raise Exception(f"Statistic {StatisticsResultsAlgorithm.MostTradedSecurityStatistic} should be in the summary statistics")
        if StatisticsResultsAlgorithm.MostTradedSecurityTradeCountStatistic not in statistics:
            raise Exception(f"Statistic {StatisticsResultsAlgorithm.MostTradedSecurityTradeCountStatistic} should be in the summary statistics")

        most_trade_security, most_trade_security_trade_count = self.GetMostTradeSecurity()
        self.CheckMostTradedSecurityStatistic(statistics, most_trade_security, most_trade_security_trade_count)

    def CheckMostTradedSecurityStatistic(self, statistics: Dict[str, str], mostTradedSecurity: Symbol, tradeCount: int):
        mostTradedSecurityStatistic = statistics[StatisticsResultsAlgorithm.MostTradedSecurityStatistic]
        mostTradedSecurityTradeCountStatistic = statistics[StatisticsResultsAlgorithm.MostTradedSecurityTradeCountStatistic]
        self.Log(f"Most traded security: {mostTradedSecurityStatistic}")
        self.Log(f"Most traded security trade count: {mostTradedSecurityTradeCountStatistic}")

        if mostTradedSecurityStatistic != mostTradedSecurity:
            raise Exception(f"Most traded security should be {mostTradedSecurity} but it is {mostTradedSecurityStatistic}")

        if mostTradedSecurityTradeCountStatistic != str(tradeCount):
            raise Exception(f"Most traded security trade count should be {tradeCount} but it is {mostTradedSecurityTradeCountStatistic}")

    def GetMostTradeSecurity(self) -> Tuple[Symbol, int]:
        most_trade_security = max(self.trade_counts, key=lambda symbol: self.trade_counts[symbol])
        most_trade_security_trade_count = self.trade_counts[most_trade_security]
        return most_trade_security, most_trade_security_trade_count

