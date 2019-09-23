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
from QuantConnect.Logging import Log
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import InsightCollection, InsightDirection
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioConstructionModel, PortfolioTarget
from Portfolio.MaximumSharpeRatioPortfolioOptimizer import MaximumSharpeRatioPortfolioOptimizer
from datetime import datetime, timedelta
from itertools import groupby
import pandas as pd
import numpy as np
from numpy import dot, transpose
from numpy.linalg import inv
from pytz import utc
UTCMIN = datetime.min.replace(tzinfo=utc)

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
                 delta = 2.5,
                 tau = 0.05,
                 optimizer = None):
        """Initialize the model
        Args:
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
        self.optimizer = MaximumSharpeRatioPortfolioOptimizer(risk_free_rate = risk_free_rate) if optimizer is None else optimizer

        self.removedSymbols = []
        self.symbolDataBySymbol = {}

        self.insightCollection = InsightCollection()
        self.nextExpiryTime = UTCMIN
        self.rebalancingTime = UTCMIN
        self.rebalancingPeriod = Extensions.ToTimeSpan(resolution)

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

        if (algorithm.UtcTime <= self.nextExpiryTime and
            algorithm.UtcTime <= self.rebalancingTime and
            len(insights) == 0 and
            self.removedSymbols is None):
            return targets

        insights = PortfolioConstructionModel.FilterInvalidInsightMagnitude(algorithm, insights)

        self.insightCollection.AddRange(insights)

        # Create flatten target for each security that was removed from the universe
        if self.removedSymbols is not None:
            universeDeselectionTargets = [ PortfolioTarget(symbol, 0) for symbol in self.removedSymbols ]
            targets.extend(universeDeselectionTargets)
            self.removedSymbols = None

        # Get insight that haven't expired of each symbol that is still in the universe
        activeInsights = self.insightCollection.GetActiveInsights(algorithm.UtcTime)

        # Get the last generated active insight for each symbol
        lastActiveInsights = []
        for sourceModel, f in groupby(sorted(activeInsights, key = lambda ff: ff.SourceModel), lambda fff: fff.SourceModel):
            for symbol, g in groupby(sorted(list(f), key = lambda gg: gg.Symbol), lambda ggg: ggg.Symbol):
                lastActiveInsights.append(sorted(g, key = lambda x: x.GeneratedTimeUtc)[-1])

        # Get view vectors
        P, Q = self.get_views(lastActiveInsights)
        if P is not None:

            returns = dict()

            # Updates the BlackLittermanSymbolData with insights
            # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
            for insight in lastActiveInsights:
                symbol = insight.Symbol
                symbolData = self.symbolDataBySymbol.get(symbol, self.BlackLittermanSymbolData(insight.Symbol, self.lookback, self.period))
                if insight.Magnitude is None:
                    algorithm.SetRunTimeError(ArgumentNullExceptionArgumentNullException('BlackLittermanOptimizationPortfolioConstructionModel does not accept \'None\' as Insight.Magnitude. Please make sure your Alpha Model is generating Insights with the Magnitude property set.'))
                symbolData.Add(algorithm.Time, insight.Magnitude)
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
                target = PortfolioTarget.Percent(algorithm, symbol, weight)
                if target is not None:
                    targets.append(target)

        # Get expired insights and create flatten targets for each symbol
        expiredInsights = self.insightCollection.RemoveExpiredInsights(algorithm.UtcTime)

        expiredTargets = []
        for symbol, f in groupby(expiredInsights, lambda x: x.Symbol):
            if not self.insightCollection.HasActiveInsights(symbol, algorithm.UtcTime):
                expiredTargets.append(PortfolioTarget(symbol, 0))
                continue

        targets.extend(expiredTargets)

        self.nextExpiryTime = self.insightCollection.GetNextExpiryTime()
        if self.nextExpiryTime is None:
            self.nextExpiryTime = UTCMIN

        self.rebalancingTime = algorithm.UtcTime + self.rebalancingPeriod

        return targets


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # Get removed symbol and invalidate them in the insight collection
        self.removedSymbols = [x.Symbol for x in changes.RemovedSecurities]
        self.insightCollection.Clear(self.removedSymbols)

        for symbol in self.removedSymbols:
            symbolData = self.symbolDataBySymbol.pop(symbol, None)
            if symbolData is not None:
                symbolData.Reset()

        # initialize data for added securities
        addedSymbols = [ x.Symbol for x in changes.AddedSecurities ]
        history = algorithm.History(addedSymbols, self.lookback * self.period, self.resolution)

        for symbol in addedSymbols:
            symbolData = self.BlackLittermanSymbolData(symbol, self.lookback, self.period)

            if not history.empty:
                ticker = SymbolCache.GetTicker(symbol)

                if ticker not in history.index.levels[0]:
                    Log.Trace(f'BlackLittermanOptimizationPortfolioConstructionModel.OnSecuritiesChanged: {ticker} not found in history data frame.')
                    continue

                symbolData.WarmUpIndicators(history.loc[ticker])

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
                data = [float(x.Value) for x in self.window],
                index = [x.EndTime for x in self.window])

        @property
        def IsReady(self):
            return self.window.IsReady

        def __str__(self, **kwargs):
            return '{}: {:.2%}'.format(self.roc.Name, (1 + self.window[0])**252 - 1)