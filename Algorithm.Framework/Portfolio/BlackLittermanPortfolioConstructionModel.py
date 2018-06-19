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
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioConstructionModel, PortfolioTarget
from datetime import timedelta
import numpy as np
import pandas as pd
from scipy.optimize import minimize
from itertools import groupby
from numpy import dot, transpose
from numpy.linalg import inv

### <summary>
### Provides an implementation of Black-Litterman portfolio optimization. The model adjusts equilibrium market 
### returns by incorporating views from multiple alpha models and therefore to get the optimal risky portfolio 
### reflecting those views. If insights of all alpha models have None magnitude or there are linearly dependent 
### vectors in link matrix of views, the expected return would be the implied excess equilibrium return. 
### The interval of weights in optimization method can be changed based on the long-short algorithm.
### The default model uses the 0.0025 as weight-on-views scalar parameter tau. The optimization method 
### maximizes the Sharpe ratio with the weight range from -1 to 1.
### </summary>
class BlackLittermanPortfolioConstructionModel(PortfolioConstructionModel):
    def __init__(self, *args, **kwargs):
        """Initialize the model
        Args:
            lookback(int): Historical return lookback period
            resolution: The resolution of the history price 
            minimum_weight(float): The lower bounds on portfolio weights
            maximum_weight(float): The upper bounds on portfolio weights
            risk_free_rate(float): The risk free rate
            self.tau(float): The model parameter indicating the uncertainty of the CAPM prior """
            
        self.lookback = kwargs['lookback'] if 'lookback' in kwargs else 5
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.minimum_weight = kwargs['minimum_weight'] if 'minimum_weight' in kwargs else -1
        self.maximum_weight = kwargs['maximum_weight'] if 'maximum_weight' in kwargs else 1
        self.risk_free_rate = kwargs['risk_free_rate'] if 'risk_free_rate' in kwargs else 0
        self.tau = kwargs['tau'] if 'tau' in kwargs else 0.025
        self.symbolDataDict = {}
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
        
        # construct the dataframe of historical return
        price = {}
        for symbol, data in self.symbolDataDict.items():
            price[str(symbol)] = data.PriceSeries()
        df_price = pd.DataFrame(price)[::-1]

        returns = df_price.pct_change().dropna()

        # get view vectors
        P, Q = self.get_views(insights, returns)
        
        # get the implied excess equilibrium return vector 
        equilibrium_return, cov = self.get_equilibrium_return(returns)
        
        # if view is empty, use equilibrium return as the expected return instead
        if P.size == 0:
            expected_return = equilibrium_return
        else:
            # create the diagonal covariance matrix of error terms from the expressed views
            omega = dot(dot(dot(self.tau, P), cov), transpose(P))
    
            if np.linalg.det(omega) == 0:
                expected_return = equilibrium_return
            else:
                A = inv(dot(self.tau, cov)) + dot(dot(np.transpose(P), inv(omega)), P)
                B = dot(inv(dot(self.tau, cov)), equilibrium_return) + dot(dot(np.transpose(P), inv(omega)), Q)
                # the new combined expected return vector
                expected_return = dot(inv(A), B)

        # the optimization method processes the data frame
        opt, weights = self.maximum_sharpe_ratio(self.risk_free_rate, expected_return, returns)

        # create portfolio targets from the specified insights
        for insight in insights:
            weight = weights[str(insight.Symbol)]
            targets.append(PortfolioTarget.Percent(algorithm, insight.Symbol, weight))

        return targets


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        for removed in changes.RemovedSecurities:
            self.pendingRemoval.append(removed.Symbol)
            symbolData = self.symbolDataDict.pop(removed.Symbol, None)
        # update the price in history rolling window for unchanged securities
        for symbol in self.symbolDataDict.keys():
            self.symbolDataDict[symbol].Add(algorithm.Time, algorithm.Securities[symbol].Close)
        # initialize data for newly added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities]
        history = algorithm.History(symbols, self.lookback, self.resolution)
        for symbol in symbols:
            if symbol not in self.symbolDataDict.keys():
                symbolData = SymbolData(symbol, self.lookback)
                self.symbolDataDict[symbol] = symbolData
                symbolData.WarmUpHisotryWindow(history.loc[str(symbol)])


    def maximum_sharpe_ratio(self, risk_free_rate, expected_return, returns):
        '''Maximum Sharpe Ratio optimization method'''

        # Objective function
        fun = lambda weights: -self.sharpe_ratio(risk_free_rate, expected_return, returns, weights)

        # Constraint: The sum of weights equal to 1
        constraints = [{'type': 'eq', 'fun': lambda w: np.sum(w) - 1}]

        size = returns.columns.size
        x0 = np.array(size * [1. / size])
        bounds = tuple((self.minimum_weight, self.maximum_weight) for x in range(size))

        opt = minimize(fun,                         # Objective function
                       x0,                          # Initial guess
                       method='SLSQP',              # Optimization method:  Sequential Least SQuares Programming
                       bounds = bounds,             # Bounds for variables 
                       constraints = constraints)   # Constraints definition

        weights = pd.Series(opt['x'], index = returns.columns)

        return opt, weights

    def sharpe_ratio(self, risk_free_rate, expected_return, returns, weights):
        annual_return = np.dot(np.matrix(expected_return), np.matrix(weights).T).item()
        annual_volatility = np.sqrt(np.dot(weights.T, np.dot(returns.cov(), weights)))
        return (annual_return - risk_free_rate)/annual_volatility
        
    def get_equilibrium_return(self, returns):
        ''' Calculate the implied excess equilibrium return '''
        
        symbols = list(returns.columns)
        # equal weighting scheme
        W = np.array([1/len(symbols)]*len(symbols))
        # annualized return
        annual_return = np.sum(((1 + returns.mean())**252 - 1) * W)
        # annualized variance of return
        annual_variance = dot(W.T, dot(returns.cov()*252, W))
        # the risk aversion coefficient
        risk_aversion = (annual_return - self.risk_free_rate ) / annual_variance
        # the covariance matrix of excess returns (N x N matrix)
        cov = returns.cov()*252
        # the implied excess equilibrium return Vector (N x 1 column vector)
        equilibrium_return = dot(dot(risk_aversion, cov), W)
        
        return equilibrium_return, cov 

    def get_views(self, insights, returns):
        ''' Generate the view variables P and Q from insights in different alpha models  '''
        
        # generate the link matrix of views: P
        view = {}
        insights = sorted(insights, key = lambda x: x.SourceModel)
        for model, group in groupby(insights, lambda x: x.SourceModel):
            view[model] = {str(symbol): 0 for symbol in list(returns.columns)}
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


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    
    def __init__(self, symbol, lookback):
        self.Symbol = symbol
        self.Window = RollingWindow[IndicatorDataPoint](lookback)

    def WarmUpHisotryWindow(self, history):
        # warm up the history window with the history request
        for tuple in history.itertuples():
            item = IndicatorDataPoint(self.Symbol, tuple.Index, float(tuple.close))
            self.Window.Add(item)
    
    def Add(self, time, value):
        # add the value to update the rolling window
        item = IndicatorDataPoint(self.Symbol, time, value)
        self.Window.Add(item)

    def PriceSeries(self):
        # convert the rolling window to the price series indexed by datetime
        data = [float(x.Value) for x in self.Window]
        index = [x.EndTime for x in self.Window]
        return pd.Series(data, index=index)