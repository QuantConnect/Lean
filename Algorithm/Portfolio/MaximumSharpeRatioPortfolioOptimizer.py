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

    def Optimize(self, historicalReturns, expectedReturns = None, covariance = None):
        '''
        Perform portfolio optimization for a provided matrix of historical returns and an array of expected returns
        args:
            historicalReturns: Matrix of annualized historical returns where each column represents a security and each row returns for the given date/time (size: K x N).
            expectedReturns: Array of double with the portfolio annualized expected returns (size: K x 1).
            covariance: Multi-dimensional array of double with the portfolio covariance of annualized returns (size: K x K).
        Returns:
            Array of double with the portfolio weights (size: K x 1)
        '''
        if covariance is None:
            covariance = historicalReturns.cov()
        if expectedReturns is None:
            expectedReturns = historicalReturns.mean()
        expectedReturns = expectedReturns - self.risk_free_rate

        size = covariance.columns.size   # K x 1
        x0 = np.array(size * [1. / size])
        k = expectedReturns.dot(x0)

        # Sharpe Maximization under Quadratic Constraints
        # https://quant.stackexchange.com/questions/18521/sharpe-maximization-under-quadratic-constraints
        # (µ − r_f)^T w = k
        constraints = [
            {'type': 'eq', 'fun': lambda weights: expectedReturns.dot(weights) - k}]

        # Σw = 1
        constraints.append(
            {'type': 'eq', 'fun': lambda weights: self.get_budget_constraint(weights)})

        opt = minimize(lambda weights: self.portfolio_variance(weights, covariance),   # Objective function
                       x0,                                                        # Initial guess
                       bounds = self.get_boundary_conditions(size),               # Bounds for variables: lw ≤ w ≤ up
                       constraints = constraints,                                 # Constraints definition
                       method='SLSQP')        # Optimization method:  Sequential Least SQuares Programming
        sharpe_ratio = expectedReturns.dot(opt['x']) / opt.fun
        return opt['x']

    def portfolio_variance(self, weights, covariance):
        '''Computes the portfolio variance
        Args:
            weighs: Portfolio weights
            covariance: Covariance matrix of historical returns'''
        variance = np.dot(weights.T, np.dot(covariance, weights))
        if variance == 0:
            raise ValueError(f'MaximumSharpeRatioPortfolioOptimizer.portfolio_variance: Volatility cannot be zero. Weights: {weights}')
        return variance

    def get_boundary_conditions(self, size):
        '''Creates the boundary condition for the portfolio weights'''
        return tuple((self.minimum_weight, self.maximum_weight) for x in range(size))

    def get_budget_constraint(self, weights):
        '''Defines a budget constraint: the sum of the weights equals unity'''
        return np.sum(weights) - 1