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
AddReference("QuantConnect.Logging")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Logging import Log
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import InsightCollection, InsightDirection
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioConstructionModel, PortfolioTarget, PortfolioBias
from Portfolio.MaximumSharpeRatioPortfolioOptimizer import MaximumSharpeRatioPortfolioOptimizer
from datetime import datetime, timedelta
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
                 rebalance = Resolution.Daily,
                 portfolioBias = PortfolioBias.LongShort,
                 lookback = 1,
                 period = 63,
                 resolution = Resolution.Daily,
                 risk_free_rate = 0,
                 delta = 2.5,
                 tau = 0.05,
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
            risk_free_rate(float): The risk free rate
            delta(float): The risk aversion coeffficient of the market portfolio
            tau(float): The model parameter indicating the uncertainty of the CAPM prior"""
        self.lookback = lookback
        self.period = period
        self.resolution = resolution
        self.risk_free_rate = risk_free_rate
        self.delta = delta
        self.tau = tau
        self.portfolioBias = portfolioBias

        lower = 0 if portfolioBias == PortfolioBias.Long else -1
        upper = 0 if portfolioBias == PortfolioBias.Short else 1
        self.optimizer = MaximumSharpeRatioPortfolioOptimizer(lower, upper, risk_free_rate) if optimizer is None else optimizer

        self.sign = lambda x: -1 if x < 0 else (1 if x > 0 else 0)
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
        return len(PortfolioConstructionModel.FilterInvalidInsightMagnitude(self.Algorithm, [ insight ])) != 0

    def DetermineTargetPercent(self, lastActiveInsights):
        targets = {}

        # Get view vectors
        P, Q = self.get_views(lastActiveInsights)
        if P is not None:
            returns = dict()
            # Updates the BlackLittermanSymbolData with insights
            # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
            for insight in lastActiveInsights:
                symbol = insight.Symbol
                symbolData = self.symbolDataBySymbol.get(symbol, self.BlackLittermanSymbolData(symbol, self.lookback, self.period))
                if insight.Magnitude is None:
                    self.Algorithm.SetRunTimeError(ArgumentNullException('BlackLittermanOptimizationPortfolioConstructionModel does not accept \'None\' as Insight.Magnitude. Please make sure your Alpha Model is generating Insights with the Magnitude property set.'))
                    return targets
                symbolData.Add(insight.GeneratedTimeUtc, insight.Magnitude)
                returns[symbol] = symbolData.Return

            returns = pd.DataFrame(returns)

            # Calculate prior estimate of the mean and covariance
            Pi, Sigma = self.get_equilibrium_return(returns)

            # Calculate posterior estimate of the mean and covariance
            Pi, Sigma = self.apply_blacklitterman_master_formula(Pi, Sigma, P, Q)

            # Create portfolio targets from the specified insights
            weights = self.optimizer.Optimize(returns, Pi, Sigma)
            weights = pd.Series(weights, index = Sigma.columns)

            for symbol, weight in weights.items():
                for insight in lastActiveInsights:
                    if str(insight.Symbol) == str(symbol):
                        # don't trust the optimizer
                        if self.portfolioBias != PortfolioBias.LongShort and self.sign(weight) != self.portfolioBias:
                            weight = 0
                        targets[insight] = weight
                        break;

        return targets

    def GetTargetInsights(self):
        # Get insight that haven't expired of each symbol that is still in the universe
        activeInsights = self.InsightCollection.GetActiveInsights(self.Algorithm.UtcTime)

        # Get the last generated active insight for each symbol
        lastActiveInsights = []
        for sourceModel, f in groupby(sorted(activeInsights, key = lambda ff: ff.SourceModel), lambda fff: fff.SourceModel):
            for symbol, g in groupby(sorted(list(f), key = lambda gg: gg.Symbol), lambda ggg: ggg.Symbol):
                lastActiveInsights.append(sorted(g, key = lambda x: x.GeneratedTimeUtc)[-1])
        return lastActiveInsights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # Get removed symbol and invalidate them in the insight collection
        super().OnSecuritiesChanged(algorithm, changes)

        for security in changes.RemovedSecurities:
            symbol = security.Symbol
            symbolData = self.symbolDataBySymbol.pop(symbol, None)
            if symbolData is not None:
                symbolData.Reset()

        # initialize data for added securities
        addedSymbols = { x.Symbol: x.Exchange.TimeZone for x in changes.AddedSecurities }
        history = algorithm.History(list(addedSymbols.keys()), self.lookback * self.period, self.resolution)

        if history.empty:
            return

        history = history.close.unstack(0)
        symbols = history.columns

        for symbol, timezone in addedSymbols.items():
            if str(symbol) not in symbols:
                continue

            symbolData = self.symbolDataBySymbol.get(symbol, self.BlackLittermanSymbolData(symbol, self.lookback, self.period))
            for time, close in history[symbol].items():
                utcTime = Extensions.ConvertToUtc(time, timezone)
                symbolData.Update(utcTime, close)

            self.symbolDataBySymbol[symbol] = symbolData

    def apply_blacklitterman_master_formula(self, Pi, Sigma, P, Q):
        '''Apply Black-Litterman master formula
        http://www.blacklitterman.org/cookbook.html
        Args:
            Pi: Prior/Posterior mean array
            Sigma: Prior/Posterior covariance matrix
            P: A matrix that identifies the assets involved in the views (size: K x N)
            Q: A view vector (size: K x 1)'''
        ts = self.tau * Sigma

        # Create the diagonal Sigma matrix of error terms from the expressed views
        omega = np.dot(np.dot(P, ts), P.T) * np.eye(Q.shape[0])
        if np.linalg.det(omega) == 0:
            return Pi, Sigma

        A = np.dot(np.dot(ts, P.T), inv(np.dot(np.dot(P, ts), P.T) + omega))

        Pi = np.squeeze(np.asarray((
            np.expand_dims(Pi, axis=0).T +
            np.dot(A, (Q - np.expand_dims(np.dot(P, Pi.T), axis=1))))
            ))

        M = ts - np.dot(np.dot(A, P), ts)
        Sigma = (Sigma + M) * self.delta

        return Pi, Sigma

    def get_equilibrium_return(self, returns):
        '''Calculate equilibrium returns and covariance
        Args:
            returns: Matrix of returns where each column represents a security and each row returns for the given date/time (size: K x N)
        Returns:
            equilibrium_return: Array of double of equilibrium returns
            cov: Multi-dimensional array of double with the portfolio covariance of returns (size: K x K)'''

        size = len(returns.columns)
        # equal weighting scheme
        W = np.array([1/size]*size)
        # the covariance matrix of excess returns (N x N matrix)
        cov = returns.cov()*252
        # annualized return
        annual_return = np.sum(((1 + returns.mean())**252 -1) * W)
        # annualized variance of return
        annual_variance = dot(W.T, dot(cov, W))
        # the risk aversion coefficient
        risk_aversion = (annual_return - self.risk_free_rate ) / annual_variance
        # the implied excess equilibrium return Vector (N x 1 column vector)
        equilibrium_return = dot(dot(risk_aversion, cov), W)

        return equilibrium_return, cov

    def get_views(self, insights):
        '''Generate views from multiple alpha models
        Args
            insights: Array of insight that represent the investors' views
        Returns
            P: A matrix that identifies the assets involved in the views (size: K x N)
            Q: A view vector (size: K x 1)'''
        try:
            P = {}
            Q = {}
            for model, group in groupby(insights, lambda x: x.SourceModel):
                group = list(group)

                up_insights_sum = 0.0
                dn_insights_sum = 0.0
                for insight in group:
                    if insight.Direction == InsightDirection.Up:
                        up_insights_sum = up_insights_sum + np.abs(insight.Magnitude)
                    if insight.Direction == InsightDirection.Down:
                        dn_insights_sum = dn_insights_sum + np.abs(insight.Magnitude)

                q = up_insights_sum if up_insights_sum > dn_insights_sum else dn_insights_sum
                if q == 0:
                    continue

                Q[model] = q

                # generate the link matrix of views: P
                P[model] = dict()
                for insight in group:
                    value = insight.Direction * np.abs(insight.Magnitude)
                    P[model][insight.Symbol] = value / q
                # Add zero for other symbols that are listed but active insight
                for symbol in self.symbolDataBySymbol.keys():
                    if symbol not in P[model]:
                        P[model][symbol] = 0

            Q = np.array([[x] for x in Q.values()])
            if len(Q) > 0:
                P = np.array([list(x.values()) for x in P.values()])
                return P, Q
        except:
            pass

        return None, None


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

        def Update(self, utcTime, close):
            self.roc.Update(utcTime, close)

        def OnRateOfChangeUpdated(self, roc, value):
            if roc.IsReady:
                self.window.Add(value)

        def Add(self, time, value):
            if self.window.Samples > 0 and self.window[0].EndTime == time:
                return;

            item = IndicatorDataPoint(self.symbol, time, value)
            self.window.Add(item)

        @property
        def Return(self):
            return pd.Series(
                data = [x.Value for x in self.window],
                index = [x.EndTime for x in self.window])

        @property
        def IsReady(self):
            return self.window.IsReady

        def __str__(self, **kwargs):
            return f'{self.roc.Name}: {(1 + self.window[0])**252 - 1:.2%}'