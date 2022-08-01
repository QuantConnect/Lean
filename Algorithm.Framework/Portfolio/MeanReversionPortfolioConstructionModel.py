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

### <summary>
### Implementation of On-Line Moving Average Reversion (OLMAR)
### </summary>
### <remarks>Li, B., Hoi, S. C. (2012). On-line portfolio selection with moving average reversion. arXiv preprint arXiv:1206.4626.
### Available at https://arxiv.org/ftp/arxiv/papers/1206/1206.4626.pdf</remarks>
### <remarks>Using windowSize = 1 => Passive Aggressive Mean Reversion (PAMR) Portfolio</remarks>
class MeanReversionPortfolioConstructionModel(PortfolioConstructionModel):
    
    def __init__(self,
                 eps = 1,
                 window_size = 20,
                 resolution = Resolution.Daily):
        """Initialize the model
        Args:
            eps: Reversion threshold
            window_size: Window size of mean price calculation
            resolution: The resolution of the history price and rebalancing
        """
        super().__init__()
        self.eps = eps
        self.window_size = window_size
        self.resolution = resolution

        self.m = 0
        # Initialize a dictionary to store stock data
        self.symbol_data = {}

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

        m = len(activeInsights)
        if self.m != m:
            self.m = m
            # Initialize portfolio weightings vector
            self.b_t = np.ones(m) * (1/m)
            
        ### Get price relatives vs expected price (SMA)
        x_tilde = self.GetPriceRelative(activeInsights)

        ### Get step size of next portfolio
        # \bar{x}_{t+1} = 1^T * \tilde{x}_{t+1} / m
        # \lambda_{t+1} = max( 0, ( b_t * \tilde{x}_{t+1} - \epsilon ) / ||\tilde{x}_{t+1}  - \bar{x}_{t+1} * 1|| ^ 2 )
        x_bar = x_tilde.mean()
        assets_mean_dev = x_tilde - x_bar
        second_norm = (np.linalg.norm(assets_mean_dev)) ** 2
        
        if second_norm == 0.0:
            step_size = 0
        else:
            step_size = (np.dot(self.b_t, x_tilde) - self.eps) / second_norm
            step_size = max(0, step_size)

        ### Get next portfolio weightings
        # b_{t+1} = b_t - step_size * ( \tilde{x}_{t+1}  - \bar{x}_{t+1} * 1 )
        b = self.b_t - step_size * assets_mean_dev
        # Normalize
        b_norm = self.SimplexProjection(b)
        # Save normalized result for the next portfolio step
        self.b_t = b_norm

        for i, insight in enumerate(activeInsights):
            targets[insight] = b_norm[i]

        return targets
    
    def GetPriceRelative(self, activeInsights):
        """Get price relatives with reference level of SMA
        Args:
            activeInsights: list of active insights
        Returns:
            array of price relatives vector
        """
        # Initialize a price vector of the next prices relatives' projection
        x_tilde = np.zeros(self.m)

        ### Get next price relative predictions
        # Using the previous price to simulate assumption of instant reversion
        for i, insight in enumerate(activeInsights):
            symbol_data = self.symbol_data[insight.Symbol]
            x_tilde[i] = 1 + insight.Magnitude \
                if insight.Magnitude is not None \
                else symbol_data.Identity.Current.Value / symbol_data.Sma.Current.Value
        
        return x_tilde

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
                self.symbol_data[symbol] = self.SymbolData(algorithm, symbol, self.window_size, self.resolution)

    def SimplexProjection(self, v, b=1):
        """Normalize the updated portfolio into weight vector:
        v_{t+1} = arg min || v - v_{t+1} || ^ 2
        
        Implementation from:
        Duchi, J., Shalev-Shwartz, S., Singer, Y., & Chandra, T. (2008, July). 
            Efficient projections onto the l 1-ball for learning in high dimensions.
            In Proceedings of the 25th international conference on Machine learning 
            (pp. 272-279).
        """
        v = np.asarray(v)

        # Sort v into u in descending order
        u = np.sort(v)[::-1]
        sv = np.cumsum(u)

        rho = np.where(u > (sv - b) / np.arange(1, len(v) + 1))[0][-1]
        theta = (sv[rho] - b) / (rho + 1)
        w = (v - theta)
        w[w < 0] = 0
        return w

    class SymbolData:
        def __init__(self, algo, symbol, window_size, resolution):
            # Indicator of price
            self.Identity = algo.Identity(symbol, resolution)
            # Moving average indicator for mean reversion level
            self.Sma = algo.SMA(symbol, window_size, resolution)
            
            # Warmup indicator
            algo.WarmUpIndicator(symbol, self.Identity, resolution)
            algo.WarmUpIndicator(symbol, self.Sma, resolution)

        def Reset(self):
            self.Identity.Reset()
            self.Sma.Reset()
        
        @property
        def IsReady(self):
            return self.Identity.IsReady and self.Sma.IsReady