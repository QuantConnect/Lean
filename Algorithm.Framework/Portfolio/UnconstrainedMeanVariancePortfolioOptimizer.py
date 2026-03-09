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

from numpy import dot
from numpy.linalg import inv

### <summary>
### Provides an implementation of a portfolio optimizer with unconstrained mean variance.'''
### </summary>
class UnconstrainedMeanVariancePortfolioOptimizer:
    '''Provides an implementation of a portfolio optimizer with unconstrained mean variance.'''
    def optimize(self, historical_returns, expected_returns = None, covariance = None):
        '''
        Perform portfolio optimization for a provided matrix of historical returns and an array of expected returns
        args:
            historical_returns: Matrix of historical returns where each column represents a security and each row returns for the given date/time (size: K x N).
            expected_returns: Array of double with the portfolio annualized expected returns (size: K x 1).
            covariance: Multi-dimensional array of double with the portfolio covariance of annualized returns (size: K x K).</param>
        Returns:
            Array of double with the portfolio weights (size: K x 1)
        '''
        if expected_returns is None:
            expected_returns = historical_returns.mean()
        if covariance is None:
            covariance = historical_returns.cov()

        return expected_returns.dot(inv(covariance))
