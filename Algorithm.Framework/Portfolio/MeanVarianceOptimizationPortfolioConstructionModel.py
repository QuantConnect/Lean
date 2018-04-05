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
from QuantConnect.Algorithm.Framework.Portfolio import PortfolioTarget
from datetime import timedelta
import numpy as np
import pandas as pd
from scipy.optimize import minimize

### <summary>
### Provides an implementation of Mean-Variance portfolio optimization based on modern portfolio theory.
### The interval of weights in optimization method can be changed based on the long-short algorithm.
### The default model uses the last three months daily price to calculate the optimal weight 
### with the weight range from -1 to 1 and minimize the portfolio variance with a target return of 2%
### </summary>
class MeanVarianceOptimizationPortfolioConstructionModel:
    def __init__(self, *args, **kwargs):
        """ 
        Initialize the model
        Args:
            resolution: The resolution of the history price 
            period(int): The time interval of history price to calculate the weight 
            minimum_weight(float): The lower bounds on portfolio weights
            maximum_weight(float): The upper bounds on portfolio weights
            target_return(float): The target portfolio return
            optimization_method(method): Method used to compute the portfolio weights
        """
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.period = kwargs['period'] if 'period' in kwargs else 63
        self.minimum_weight = kwargs['minimum_weight'] if 'minimum_weight' in kwargs else -1
        self.maximum_weight = kwargs['maximum_weight'] if 'maximum_weight' in kwargs else 1
        self.target_return = kwargs['target_return'] if 'target_return' is kwargs else 2
        self.optimization_method = kwargs['optimization_method'] if 'optimization_method' in kwargs else self.minimum_variance
        self.symbolDataBySymbol = {}


    def CreateTargets(self, algorithm, insights):
        """ 
        Create portfolio targets from the specified insights
        Args:
            algorithm: The algorithm instance
            insights: The insights to create portoflio targets from
        Returns: 
            An enumerable of portoflio targets to be sent to the execution model
        """
        symbols = [insight.Symbol for insight in insights]
        if len(symbols) == 0:
            return []

        for insight in insights:
            symbolData = self.symbolDataBySymbol.get(insight.Symbol)
            symbolData.Add(algorithm.Time, insight.Magnitude)

        # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
        returns = { str(symbol) : data.Return for symbol, data in self.symbolDataBySymbol.items() if symbol in symbols }
        returns = pd.DataFrame(returns)

        # The optimization method processes the data frame 
        opt, weights = self.optimization_method(returns)

        # Create portfolio targets from the specified insights
        for insight in insights:
            weight = weights[str(insight.Symbol)]
            yield PortfolioTarget.Percent(algorithm, insight.Symbol, weight)


    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        removed = [ x.Symbol for x in changes.RemovedSecurities ]
        if len(removed) > 0:
            for subscription in algorithm.SubscriptionManager.Subscriptions:
                symbol = subscription.Symbol
                if symbol in removed and symbol in self.symbolDataBySymbol:
                    self.symbolDataBySymbol.pop(symbol)

        # initialize data for added securities
        for added in changes.AddedSecurities:
            if added.Symbol not in self.symbolDataBySymbol:
                symbolData = SymbolData(added, self.period)
                self.symbolDataBySymbol[added.Symbol] = symbolData

                # seed new indicators using history request
                history = algorithm.History(added.Symbol, self.period, self.resolution)
                for row in history.itertuples():
                    symbolData.ROC.Update(row.Index[1], row.close)


    def minimum_variance(self, returns):
        '''Minimum variance optimization method'''

        # Objective function
        fun = lambda weights: np.sqrt(np.dot(weights.T, np.dot(returns.cov(), weights)))

        # Constraint #1: The weights can be negative, which means investors can short a security.
        constraint1 = {'type': 'eq', 'fun': lambda w: np.sum(w) - 1}

        # Constraint #2: 
        constraint2 = {'type': 'eq', 'fun': lambda weights: np.dot(np.matrix(returns.mean()), np.matrix(weights).T).item() - self.target_return}

        size = returns.columns.size
        x0 = np.array(size * [1. / size])
        bounds = tuple((self.minimum_weight, self.maximum_weight) for x in range(size))
        constraints = [constraint1 ,constraint2]
        
        opt = minimize(fun,                         # Objective function
                       x0,                          # Initial guess
                       method='SLSQP',              # Optimization method:  Sequential Least SQuares Programming
                       bounds = bounds,             # Bounds for variables 
                       constraints = constraints)   # Constraints definition

        return opt, pd.Series(opt['x'], index = returns.columns)


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol, period):
        self.Symbol = symbol
        self.ROC = RateOfChange(1)
        self.ROC.Updated += self.OnRateOfChangeUpdated
        self.Window = RollingWindow[IndicatorDataPoint](period)

    def OnRateOfChangeUpdated(self, roc, value):
        if roc.IsReady:
            self.Window.Add(value)

    def Add(self, time, value):
        item = IndicatorDataPoint(self.Symbol, time, value)
        self.Window.Add(item)

    @property
    def Return(self):
        data = [(1 + float(x.Value))**252 - 1 for x in self.Window]
        index = [x.EndTime for x in self.Window]
        return pd.Series(data, index=index)

    @property
    def IsReady(self):
        return self.Window.IsReady
