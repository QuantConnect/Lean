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
from scipy.optimize import minimize

### <summary>
### Risk Parity Portfolio Construction Model
### </summary>
### <remarks>Spinu, F. (2013). An algorithm for computing risk parity weights. Available at SSRN 2297383.
### Available at https://papers.ssrn.com/sol3/Papers.cfm?abstract_id=2297383</remarks>
class RiskParityPortfolioConstructionModel(PortfolioConstructionModel):
    '''Optimization Problem
    minimize_{w >= 0} 1/2 * x^T S x - b^T log(x)
    b = 1/num_of_assets (equal budget of risk)
    
    dy/dw = Sx - b/x'''
    
    def __init__(self,
                 rebalance = Resolution.Daily,
                 portfolioBias = PortfolioBias.LongShort,
                 lookback = 252,
                 resolution = Resolution.Daily):
        """Initialize the model
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.
            portfolioBias: Specifies the bias of the portfolio (Short, Long/Short, Long)
            lookback: Lookback period for volatility estimation
            resolution: The resolution of the history price and rebalancing
        """
        np.random.seed(0)
        
        super().__init__()
        if portfolioBias == PortfolioBias.Short:
            raise ArgumentException("Long position must be allowed in RiskParityPortfolioConstructionModel.")
            
        self.lookback = lookback
        self.resolution = resolution

        # Initialize a dictionary to store stock data
        self.symbol_data = {}

        # If the argument is an instance of Resolution or Timedelta
        # Redefine rebalancingFunc
        rebalancingFunc = rebalance
        if isinstance(rebalance, int):
            rebalance = Extensions.ToTimeSpan(rebalance)
        if isinstance(rebalance, timedelta):
            rebalancingFunc = lambda dt: dt + rebalance
        if rebalancingFunc:
            self.SetRebalancingFunc(rebalancingFunc)

    def DetermineTargetPercent(self, activeInsights):
        """Will determine the target percent for each insight
        Args:
            activeInsights: list of active insights
        Returns:
            dictionary of insight and respective target weight
        """
        targets = {}

        # If we have no insights or non-ready just return an empty target list
        if len(activeInsights) == 0 or not all([self.symbol_data[x.Symbol].IsReady for x in activeInsights]):
            return targets
        
        # Get the covariance matrix of all activeInsights' symbols
        rets = np.array([list(self.symbol_data[insight.Symbol].Returns) for insight in activeInsights])
        cov = np.cov(rets)
        
        # Set up optimization parameters
        # Constraints: each weight is between 0 and 1 inclusively
        num_of_assets = len(activeInsights)
        init_weight = np.array([1/num_of_assets] * num_of_assets)
        objective = lambda w: 1/2 * w.T @ cov @ w - init_weight.T @ np.log(w)
        gradient = lambda w: cov @ w - init_weight / w
        bounds = tuple((0, np.inf) for _ in range(num_of_assets))
        
        # Optimize for weights
        opt = minimize(objective, x0=init_weight, jac=gradient, bounds=bounds, method="L-BFGS-B")
        # Normalize
        weights = opt.x / np.sum(opt.x)

        # Update portfolio state
        for i, insight in enumerate(activeInsights):
            targets[insight] = weights[i]

        return targets

    def OnSecuritiesChanged(self, algorithm, changes):
        """Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm
        """
        # clean up data for removed securities
        super().OnSecuritiesChanged(algorithm, changes)
        for removed in changes.RemovedSecurities:
            symbol_data = self.symbol_data.pop(removed.Symbol, None)
            symbol_data.Reset()

        # initialize data for added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities ]

        for symbol in symbols:
            if symbol not in self.symbol_data:
                self.symbol_data[symbol] = self.RiskParitySymbolData(algorithm, symbol, self.lookback, self.resolution)

    class RiskParitySymbolData:
        def __init__(self, algo, symbol, lookback, resolution):
            # Indicator of pct return
            self.roc = algo.ROC(symbol, 1, resolution)
            # RollingWindow to save the pct return
            self.Returns = RollingWindow[float](lookback)
            # Update the RollingWindow when new pct change piped
            self.roc.Updated += self.OnROCUpdated
            
            # Warmup indicator
            history = algo.History[TradeBar](symbol, lookback + 1, resolution)
            for bar in history:
                self.roc.Update(bar.EndTime, bar.Close)

        def OnROCUpdated(self, sender, updated):
            self.Returns.Add(updated.Value)

        def Reset(self):
            self.roc.Updated -= self.OnROCUpdated
            self.roc.Reset()
            self.Returns.Reset()
        
        @property
        def IsReady(self):
            return self.roc.IsReady and self.Returns.IsReady