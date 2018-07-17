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

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *
from Alphas.HistoricalReturnsAlphaModel import *
from Portfolio.MeanVarianceOptimizationPortfolioConstructionModel import *
from QuantConnect.Util import PythonUtil

### <summary>
### Mean Variance Optimization algorithm
### Uses the HistoricalReturnsAlphaModel and the MeanVarianceOptimizationPortfolioConstructionModel
### to create an algorithm that rebalances the portfolio according to modern portfolio theory
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
class MeanVarianceOptimizationAlgorithm(QCAlgorithmFramework):
    '''Mean Variance Optimization algorithm.'''

    def Initialize(self):

        # Set requested data resolution
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.symbols = [ Symbol.Create(x, SecurityType.Equity, Market.USA) for x in [ 'AIG', 'BAC', 'IBM', 'SPY' ] ]

        self.minimum_weight = -1
        self.maximum_weight = 1

        # set algorithm framework models
        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.coarseSelector))
        self.SetAlpha(HistoricalReturnsAlphaModel(resolution = Resolution.Daily))
        self.SetPortfolioConstruction(MeanVarianceOptimizationPortfolioConstructionModel(optimization_method = self.maximum_sharpe_ratio))
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())

    def coarseSelector(self, coarse):
        # Drops SPY after the 8th
        last = 3 if self.Time.day > 8 else len(self.symbols)

        return self.symbols[0:last]

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug(orderEvent.ToString())

    def maximum_sharpe_ratio(self, returns):
        '''Maximum Sharpe Ratio optimization method'''

        # Objective function
        fun = lambda weights: -self.sharpe_ratio(returns, weights)

        # Constraint #1: The weights can be negative, which means investors can short a security.
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
        self.Log('{}:\n\r{}'.format(self.Time, weights))

        return opt, weights

    def sharpe_ratio(self, returns, weights):
        annual_return = np.dot(np.matrix(returns.mean()), np.matrix(weights).T).item()
        annual_volatility = np.sqrt(np.dot(weights.T, np.dot(returns.cov(), weights)))
        return annual_return/annual_volatility
