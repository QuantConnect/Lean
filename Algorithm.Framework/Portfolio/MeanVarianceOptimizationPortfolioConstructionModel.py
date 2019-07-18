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
                 lookback = 1,
                 period = 63,
                 resolution = Resolution.Daily,
                 optimizer = None):
        """Initialize the model
        Args:
            lookback(int): Historical return lookback period
            period(int): The time interval of history price to calculate the weight
            resolution: The resolution of the history price
            optimizer(class): Method used to compute the portfolio weights"""
        self.lookback = lookback
        self.period = period
        self.resolution = resolution
        self.optimizer = MinimumVariancePortfolioOptimizer() if optimizer is None else optimizer

        self.symbolDataBySymbol = {}
        self.pendingRemoval = []

    def CreateTargets(self, algorithm, insights):
        """
        Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portfolio targets from
        Returns:
            An enumerable of portfolio targets to be sent to the execution model
        """
        targets = []

        for symbol in self.pendingRemoval:
            targets.append(PortfolioTarget.Percent(algorithm, symbol, 0))
        self.pendingRemoval.clear()

        insights = PortfolioConstructionModel.FilterInvalidInsightMagnitude(algorithm, insights)

        symbols = [insight.Symbol for insight in insights]
        if len(symbols) == 0 or all([insight.Magnitude == 0 for insight in insights]):
            return targets

        for insight in insights:
            symbolData = self.symbolDataBySymbol.get(insight.Symbol)
            if insight.Magnitude is None:
                algorithm.SetRunTimeError(ArgumentNullException('MeanVarianceOptimizationPortfolioConstructionModel does not accept \'None\' as Insight.Magnitude. Please checkout the selected Alpha Model specifications.'))
            symbolData.Add(algorithm.Time, insight.Magnitude)

        # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
        returns = { str(symbol) : data.Return for symbol, data in self.symbolDataBySymbol.items() if symbol in symbols }
        returns = pd.DataFrame(returns)

        # The portfolio optimizer finds the optional weights for the given data
        weights = self.optimizer.Optimize(returns)
        weights = pd.Series(weights, index = returns.columns)

        # Create portfolio targets from the specified insights
        for insight in insights:
            weight = weights[str(insight.Symbol)]
            target = PortfolioTarget.Percent(algorithm, insight.Symbol, weight)
            if target is not None:
                targets.append(target)

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        for removed in changes.RemovedSecurities:
            self.pendingRemoval.append(removed.Symbol)
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