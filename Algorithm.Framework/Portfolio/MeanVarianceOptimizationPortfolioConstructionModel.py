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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from Portfolio.MinimumVariancePortfolioOptimizer import MinimumVariancePortfolioOptimizer
from datetime import timedelta
import numpy as np
import pandas as pd

### <summary>
### Provides an implementation of Mean-Variance portfolio optimization based on modern portfolio theory.
### The default model uses the MinimumVariancePortfolioOptimizer that accepts a 63-row matrix of 1-day returns.
### </summary>
class MeanVarianceOptimizationPortfolioConstructionModel(PortfolioConstructionModel):
    def __init__(self,
                 rebalance = Resolution.Daily,
                 portfolioBias = PortfolioBias.LongShort,
                 lookback = 1,
                 period = 63,
                 resolution = Resolution.Daily,
                 targetReturn = 0.02,
                 optimizer = None):
        """Initialize the model
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.
            portfolioBias: Specifies the bias of the portfolio (Short, Long/Short, Long)
            lookback(int): Historical return lookback period
            period(int): The time interval of history price to calculate the weight
            resolution: The resolution of the history price
            optimizer(class): Method used to compute the portfolio weights"""
        self.lookback = lookback
        self.period = period
        self.resolution = resolution
        self.portfolioBias = portfolioBias
        self.sign = lambda x: -1 if x < 0 else (1 if x > 0 else 0)

        lower = 0 if portfolioBias == PortfolioBias.Long else -1
        upper = 0 if portfolioBias == PortfolioBias.Short else 1
        self.optimizer = MinimumVariancePortfolioOptimizer(lower, upper, targetReturn) if optimizer is None else optimizer

        self.symbolDataBySymbol = {}

        # If the argument is an instance of Resolution or Timedelta
        # Redefine rebalancingFunc
        rebalancingFunc = rebalance
        if isinstance(rebalance, int):
            rebalance = Extensions.ToTimeSpan(rebalance)
        if isinstance(rebalance, timedelta):
            rebalancingFunc = lambda dt: dt + rebalance
        if rebalancingFunc:
            self.SetRebalancingFunc(rebalancingFunc)

    def ShouldCreateTargetForInsight(self, insight):
        if len(PortfolioConstructionModel.FilterInvalidInsightMagnitude(self.Algorithm, [insight])) == 0:
            return False

        symbolData = self.symbolDataBySymbol.get(insight.Symbol)
        if insight.Magnitude is None:
            self.algorithm.SetRunTimeError(ArgumentNullException('MeanVarianceOptimizationPortfolioConstructionModel does not accept \'None\' as Insight.Magnitude. Please checkout the selected Alpha Model specifications.'))
            return False
        symbolData.Add(self.Algorithm.Time, insight.Magnitude)

        return True

    def DetermineTargetPercent(self, activeInsights):
        """
         Will determine the target percent for each insight
        Args:
        Returns:
        """
        targets = {}
        symbols = [insight.Symbol for insight in activeInsights]

        # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
        returns = { str(symbol) : data.Return for symbol, data in self.symbolDataBySymbol.items() if symbol in symbols }
        returns = pd.DataFrame(returns)

        # The portfolio optimizer finds the optional weights for the given data
        weights = self.optimizer.Optimize(returns)
        weights = pd.Series(weights, index = returns.columns)

        # Create portfolio targets from the specified insights
        for insight in activeInsights:
            weight = weights[str(insight.Symbol)]

            # don't trust the optimizer
            if self.portfolioBias != PortfolioBias.LongShort and self.sign(weight) != self.portfolioBias:
                weight = 0
            targets[insight] = weight

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        super().OnSecuritiesChanged(algorithm, changes)
        for removed in changes.RemovedSecurities:
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            symbolData.Reset()

        # initialize data for added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities ]
        history = algorithm.History(symbols, self.lookback * self.period, self.resolution)
        if history.empty: return

        tickers = history.index.levels[0]
        for ticker in tickers:
            symbol = SymbolCache.GetSymbol(ticker)

            if symbol not in self.symbolDataBySymbol:
                symbolData = self.MeanVarianceSymbolData(symbol, self.lookback, self.period)
                symbolData.WarmUpIndicators(history.loc[ticker])
                self.symbolDataBySymbol[symbol] = symbolData

    class MeanVarianceSymbolData:
        '''Contains data specific to a symbol required by this model'''
        def __init__(self, symbol, lookback, period):
            self.symbol = symbol
            self.roc = RateOfChange(f'{symbol}.ROC({lookback})', lookback)
            self.roc.Updated += self.OnRateOfChangeUpdated
            self.window = RollingWindow[IndicatorDataPoint](period)

        def Reset(self):
            self.roc.Updated -= self.OnRateOfChangeUpdated
            self.roc.Reset()
            self.window.Reset()

        def WarmUpIndicators(self, history):
            for tuple in history.itertuples():
                self.roc.Update(tuple.Index, tuple.close)

        def OnRateOfChangeUpdated(self, roc, value):
            if roc.IsReady:
                self.window.Add(value)

        def Add(self, time, value):
            item = IndicatorDataPoint(self.symbol, time, value)
            self.window.Add(item)

        @property
        def Return(self):
            return pd.Series(
                data = [(1 + float(x.Value))**252 - 1 for x in self.window],
                index = [x.EndTime for x in self.window])

        @property
        def IsReady(self):
            return self.window.IsReady

        def __str__(self, **kwargs):
            return '{}: {:.2%}'.format(self.roc.Name, (1 + self.window[0])**252 - 1)