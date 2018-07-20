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

import numpy as np
import pandas as pd
from scipy.optimize import minimize

### <summary>
### Provides an implementation of a portfolio optimizer that maximizes the portfolio Sharpe Ratio.
### The interval of weights in optimization method can be changed based on the long-short algorithm.
### The default model uses flat risk free rate and weight for an individual security range from -1 to 1.'''
### </summary>
class MaximumSharpeRatioPortfolioOptimizer:
    '''Provides an implementation of a portfolio optimizer that maximizes the portfolio Sharpe Ratio.
   The interval of weights in optimization method can be changed based on the long-short algorithm.
   The default model uses flat risk free rate and weight for an individual security range from -1 to 1.'''
    def __init__(self, 
                 minimum_weight = -1, 
                 maximum_weight = 1,
                 risk_free_rate = 0):
        '''Initialize the MaximumSharpeRatioPortfolioOptimizer
        Args:
            minimum_weight(float): The lower bounds on portfolio weights
            maximum_weight(float): The upper bounds on portfolio weights
            risk_free_rate(float): The risk free rate'''
        self.minimum_weight = minimum_weight
        self.maximum_weight = maximum_weight
        self.risk_free_rate = risk_free_rate
        self.expected_returns = []

    def Optimize(self, historicalReturns, expectedReturns = None):
        '''
        Perform portfolio optimization for a provided matrix of historical returns and an array of expected returns
        args:
            historicalReturns: Matrix of annualized historical returns where each column represents a security and each row returns for the given date/time (size: K x N).
            expectedReturns: Array of double with the portfolio annualized expected returns (size: K x 1).
        Returns:
            Array of double with the portfolio weights (size: K x 1)
        '''
        if expectedReturns is None:
            expectedReturns = historicalReturns.mean()

        cov = historicalReturns.cov()
        size = historicalReturns.columns.size   # K x 1

        constraints = {'type': 'eq', 'fun': lambda weights: self.get_budget_constraint(weights)}

        opt = minimize(lambda weights: -self.sharpe_ratio(weights, expectedReturns, cov),   # Objective function
                       self.get_initial_guess(size),                              # Initial guess
                       bounds = self.get_boundary_conditions(size),               # Bounds for variables
                       constraints = constraints,                                 # Constraints definition
                       method='SLSQP')        # Optimization method:  Sequential Least SQuares Programming
        return opt['x']

    def sharpe_ratio(self, weights, expected_returns, covariance):
        '''Computes the portfolio sharpe ratio
        Args:
            weighs: Portfolio weights
            expected_returns: Portfolio expected return
            covariance: Covariance matrix of historical returns'''
        annual_volatility = np.sqrt(np.dot(weights.T, np.dot(covariance, weights)))
        if annual_volatility == 0:
            raise ValueError(f'MaximumSharpeRatioPortfolioOptimizer.sharpe_ratio: Volatility cannot be zero. Weights: {weights}')

        annual_return = np.dot(np.matrix(expected_returns), np.matrix(weights).T).item()
        return (annual_return - self.risk_free_rate)/annual_volatility

    def get_initial_guess(self, size):
        '''Computes an equally weighted portfolio'''
        return np.array(size * [1. / size])

    def get_boundary_conditions(self, size):
        '''Creates the boundary condition for the portfolio weights'''
        return tuple((self.minimum_weight, self.maximum_weight) for x in range(size))

    def get_budget_constraint(self, weights):
        '''Defines a budget constraint: the sum of the weights equals unity'''
        return np.sum(weights) - 1