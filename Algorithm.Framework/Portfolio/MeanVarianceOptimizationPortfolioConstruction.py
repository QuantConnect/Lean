# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
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
from QuantConnect.Util import PythonUtil
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Data.UniverseSelection import *
from datetime import timedelta
import numpy as np
import pandas as pd
from scipy.optimize import minimize

### <summary>
### Provides an implementation of Mean-Variance portfolio optimization based on modern portfolio theory.
### The target percent holdings of each security is calculated by maximizing the Sharpe ratio. 
### The interval of weights in optimization method can be changed based on the long-short algorithm.
### The default model uses the last three months daily price to calculate the optimal weight 
### with the weight range from -1 to 1  
### </summary>
class MeanVarianceOptimizationPortfolioConstructionModel:
    def __init__(self, resolution=Resolution.Daily, interval=63, min_weight=-1, max_weight=1):
        """ 
        Initialize the model
        Args:
            resolution:The resolution of the history price 
            interval(int): The time interval of history price to calculate the weight 
            min_weight(float): The lower bounds on portfolio weights
            max_weight(float): The upper bounds on portfolio weights
        """
        self.securities = [ ]
        self.resolution = resolution
        self.interval = interval
        self.min_weight = min_weight
        self.max_weight = max_weight

    def CreateTargets(self, algorithm, insights):
        """ 
        Create portfolio targets from the specified insights
        Args:
            algorithm:The algorithm instance
            insights: The insights to create portoflio targets from
        Returns: 
            An enumerable of portoflio targets to be sent to the execution model
        """
        
        symbols = [insight.Symbol for insight in insights]
        # request the daily history price of last year
        hist = algorithm.History(symbols, self.interval, self.resolution)
        df_price = hist['close'].unstack(level=0)
        # calculate the daily return 
        daily_return = (df_price / df_price.shift(1)).dropna()
        weights = PortfolioOptimization(daily_return, self.min_weight, self.max_weight).opt_portfolio() 
        # Create portfolio targets from the specified insights
        for insight in insights:
            yield PortfolioTarget.Percent(algorithm, insight.Symbol, weights[str(insight.Symbol)])
        
    def OnSecuritiesChanged(self, algorithm, changes):
        """ 
        Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm 
        """
        for added in changes.AddedSecurities:
            self.securities.append(added)
        for removed in changes.RemovedSecurities:
            if removed in self.securities:
                self.securities.remove(removed) 
 
class PortfolioOptimization(object):
    """ 
    Construct the mean variance optimization model with the portfolio return
    Args:
       df_return(dataframe): Dataframe of porfolio return indexed by the timestamp
       min_weight(float): The lower bounds on portfolio weights
       max_weight(float): The upper bounds on portfolio weights
       n(int): The number of the risk assets in the portfolio
    """
    
    def __init__(self, df_return, min_weight, max_weight):
        self.df_return = df_return 
        self.min_weight = min_weight
        self.max_weight = max_weight
        self.n = df_return.columns.size 

    def annual_portfolio_return(self, weights):
        # calculate the annual return of the portfolio
        return np.sum(self.df_return.mean() * weights) * 252

    def annual_portfolio_vol(self, weights):
        # calculate the annual volatility of the portfolio
        return np.sqrt(np.dot(weights.T, np.dot(self.df_return.cov() * 252, weights)))

    def min_func(self, weights):
        # object function of Sharpe ratio
        return - self.annual_portfolio_return(weights) / self.annual_portfolio_vol(weights)
        
    def opt_portfolio(self):
        # maximize the Sharpe ratio to find the optimal weights
        cons = ({'type': 'eq', 'fun': lambda x: np.sum(x) - 1})
        # Set the range for weights(long-only algorithm)
        # The range can be changed in interval (-1, 1) based on your long-short algorithms 
        bnds = tuple((self.min_weight, self.max_weight) for x in range(self.n))
        
        opt = minimize(self.min_func,                       # object function
                       np.array(self.n * [1. / self.n]),    # initial value
                       method='SLSQP',                      # optimization method
                       bounds=bnds,                         # bounds for variables 
                       constraints=cons)                    # constraint conditions
                      
        opt_weights = opt['x']
        return pd.Series(opt_weights, index = self.df_return.columns)