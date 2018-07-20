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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioConstructionModel, PortfolioTarget
from Portfolio.MaximumSharpeRatioPortfolioOptimizer import MaximumSharpeRatioPortfolioOptimizer
from datetime import timedelta
from itertools import groupby
import pandas as pd
import numpy as np
from numpy import dot, transpose
from numpy.linalg import inv

### <summary>
### Provides an implementation of Black-Litterman portfolio optimization. The model adjusts equilibrium market 
### returns by incorporating views from multiple alpha models and therefore to get the optimal risky portfolio 
### reflecting those views. If insights of all alpha models have None magnitude or there are linearly dependent 
### vectors in link matrix of views, the expected return would be the implied excess equilibrium return. 
### The interval of weights in optimization method can be changed based on the long-short algorithm.
### The default model uses the 0.0025 as weight-on-views scalar parameter tau and
### MaximumSharpeRatioPortfolioOptimizer that accepts a 63-row matrix of 1-day returns.
### </summary>
class BlackLittermanOptimizationPortfolioConstructionModel(PortfolioConstructionModel):
    def __init__(self,
                 lookback = 1,
                 period = 63,
                 resolution = Resolution.Daily,
                 risk_free_rate = 0,
                 tau = 0.025,
                 optimizer = None):
        """Initialize the model
        Args:
            lookback(int): Historical return lookback period
            period(int): The time interval of history price to calculate the weight
            resolution: The resolution of the history price
            risk_free_rate(float): The risk free rate
            tau(float): The model parameter indicating the uncertainty of the CAPM prior"""
        self.lookback = lookback
        self.period = period
        self.resolution = resolution
        self.risk_free_rate = risk_free_rate
        self.tau = tau
        self.optimizer = MaximumSharpeRatioPortfolioOptimizer(risk_free_rate = risk_free_rate) if optimizer is None else optimizer

        self.symbolDataBySymbol = {}
        self.pendingRemoval = []

    def CreateTargets(self, algorithm, insights):
        """ 
        Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portoflio targets from
        Returns: 
            An enumerable of portoflio targets to be sent to the execution model
        """
        targets = []

        for symbol in self.pendingRemoval:
            targets.append(PortfolioTarget.Percent(algorithm, symbol, 0))
        self.pendingRemoval.clear()

        symbols = [insight.Symbol for insight in insights]
        if len(symbols) == 0 or all([insight.Magnitude == 0 for insight in insights]):
            return targets

        for insight in insights:
            symbolData = self.symbolDataBySymbol.get(insight.Symbol)
            if insight.Magnitude is None:
                algorithm.SetRunTimeError(ArgumentNullException('BlackLittermanOptimizationPortfolioConstructionModel does not accept \'None\' as Insight.Magnitude. Please make sure your Alpha Model is generating Insights with the Magnitude property set.'))
            symbolData.Add(algorithm.Time, insight.Magnitude)

        # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
        historical_returns = { str(symbol) : data.Return for symbol, data in self.symbolDataBySymbol.items() if symbol in symbols }
        historical_returns = pd.DataFrame(historical_returns)

        # Get view vectors
        P, Q = self.get_views(insights)

        # Get the implied excess equilibrium return vector 
        equilibrium_return, cov = self.get_equilibrium_return(historical_returns)

        # If view is empty, use equilibrium return as the expected return instead
        if P.size == 0:
            investors_views_aggregation_returns = equilibrium_return
        else:
            # Create the diagonal covariance matrix of error terms from the expressed views
            omega = dot(dot(dot(self.tau, P), cov), transpose(P))
    
            if np.linalg.det(omega) == 0:
                investors_views_aggregation_returns = equilibrium_return
            else:
                A = inv(dot(self.tau, cov)) + dot(dot(np.transpose(P), inv(omega)), P)
                B = dot(inv(dot(self.tau, cov)), equilibrium_return) + dot(dot(np.transpose(P), inv(omega)), Q)
                # the new combined expected return vector
                investors_views_aggregation_returns = dot(inv(A), B)

        # The portfolio optimizer finds the optional weights for the given data
        weights = self.optimizer.Optimize(historical_returns, investors_views_aggregation_returns)
        weights = pd.Series(weights, index = historical_returns.columns)

        # create portfolio targets from the specified insights
        for insight in insights:
            weight = weights[str(insight.Symbol)]
            targets.append(PortfolioTarget.Percent(algorithm, insight.Symbol, weight))

        return targets

    def get_equilibrium_return(self, returns):
        ''' Calculate the implied excess equilibrium return '''

        size = len(returns.columns)
        # equal weighting scheme
        W = np.array([1/size]*size)
        # annualized return
        annual_return = np.sum(returns.mean() * W)
        # annualized variance of return
        annual_variance = dot(W.T, dot(returns.cov(), W))
        # the risk aversion coefficient
        risk_aversion = (annual_return - self.risk_free_rate ) / annual_variance
        # the covariance matrix of excess returns (N x N matrix)
        cov = returns.cov()
        # the implied excess equilibrium return Vector (N x 1 column vector)
        equilibrium_return = dot(dot(risk_aversion, cov), W)
        
        return equilibrium_return, cov 

    def get_views(self, insights):
        ''' Generate the view variables P and Q from insights in different alpha models  '''
        
        # generate the link matrix of views: P
        view = {}
        insights = sorted(insights, key = lambda x: x.SourceModel)
        for model, group in groupby(insights, lambda x: x.SourceModel):
            view[model] = {str(symbol): 0 for symbol in self.symbolDataBySymbol.keys()}
            for insight in group:
                view[model][str(insight.Symbol)] = insight.Direction 
        view = pd.DataFrame(view).T
        # normalize the view matrix by row
        up_view = view[view>0].fillna(0) 
        down_view = view[view<0].fillna(0)
        normalize_up_view = up_view.apply(lambda x: x/sum(x), axis=1).fillna(0)
        normalize_down_view = down_view.apply(lambda x: -x/sum(x), axis=1).fillna(0)
        # link matrix: a matrix that identifies the assets involved in the views (K x N matrix)
        P = normalize_up_view + normalize_down_view
        # drop the rows with all zero views (flat direction)
        P = P[~(P == 0).all(axis=1)]

        # generate the estimated return vector for every different view (K x 1 column vector)
        Q = []
        for model, group in groupby(insights, lambda x: x.SourceModel):
            if model in P.index:
                Q.append(sum([P.loc[model][str(insight.Symbol)]*insight.Magnitude for insight in group if insight.Magnitude is not None]))
                
        return np.array(P), np.array(Q)

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
                symbolData = self.BlackLittermanSymbolData(symbol, self.lookback, self.period)
                symbolData.WarmUpIndicators(history.loc[ticker])
                self.symbolDataBySymbol[symbol] = symbolData

    class BlackLittermanSymbolData:
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