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
from scipy.optimize import *

### <summary>
### Provides an implementation of a risk parity portfolio optimizer that calculate the optimal weights 
### with the weight range from 0 to 1 and equalize the risk carried by each asset
### </summary>
class RiskParityPortfolioOptimizer:
    
    def __init__(self, 
                 minimum_weight = 1e-05, 
                 maximum_weight = sys.float_info.max):
        '''Initialize the RiskParityPortfolioOptimizer
        Args:
            minimum_weight(float): The lower bounds on portfolio weights
            maximum_weight(float): The upper bounds on portfolio weights'''
        self.minimum_weight = minimum_weight if minimum_weight >= 1e-05 else 1e-05
        self.maximum_weight = maximum_weight if maximum_weight >= minimum_weight else minimum_weight

    def optimize(self, historical_returns, budget = None, covariance = None):
        '''
        Perform portfolio optimization for a provided matrix of historical returns and an array of expected returns
        args:
            historical_returns: Matrix of annualized historical returns where each column represents a security and each row returns for the given date/time (size: K x N).
            budget: Risk budget vector (size: K x 1).
            covariance: Multi-dimensional array of double with the portfolio covariance of annualized returns (size: K x K).
        Returns:
            Array of double with the portfolio weights (size: K x 1)
        '''
        if covariance is None:
            covariance = np.cov(historical_returns.T)

        size = historical_returns.columns.size   # K x 1
        
        # Optimization Problem
        # minimize_{x >= 0} f(x) = 1/2 * x^T.S.x - b^T.log(x)
        # b = 1 / num_of_assets (equal budget of risk)
        # df(x)/dx = S.x - b / x
        # H(x) = S + Diag(b / x^2)
        # lw <= x <= up
        x0 = np.array(size * [1. / size])
        budget = budget if budget is not None else x0
        objective = lambda weights: 0.5 * weights.T @ covariance @ weights - budget.T @ np.log(weights)
        gradient = lambda weights: covariance @ weights - budget / weights
        hessian = lambda weights: covariance + np.diag((budget / weights**2).flatten())
        solver = minimize(objective, jac=gradient, hess=hessian, x0=x0, method="Newton-CG")

        if not solver["success"]: return x0
        # Normalize weights: w = x / x^T.1
        return np.clip(solver["x"]/np.sum(solver["x"]), self.minimum_weight, self.maximum_weight)
