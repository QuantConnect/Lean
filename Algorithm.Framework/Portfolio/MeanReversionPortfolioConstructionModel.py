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
                 rebalance = Resolution.Daily,
                 portfolioBias = PortfolioBias.LongShort,
                 reversion_threshold = 1,
                 window_size = 20,
                 resolution = Resolution.Daily):
        """Initialize the model
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.
            portfolioBias: Specifies the bias of the portfolio (Short, Long/Short, Long)
            reversion_threshold: Reversion threshold
            window_size: Window size of mean price calculation
            resolution: The resolution of the history price and rebalancing
        """
        super().__init__()
        if portfolioBias == PortfolioBias.Short:
            raise ArgumentException("Long position must be allowed in MeanReversionPortfolioConstructionModel.")
            
        self.reversion_threshold = reversion_threshold
        self.window_size = window_size
        self.resolution = resolution

        self.num_of_assets = 0
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

        num_of_assets = len(activeInsights)
        if self.num_of_assets != num_of_assets:
            self.num_of_assets = num_of_assets
            # Initialize portfolio weightings vector
            self.weight_vector = np.ones(num_of_assets) * (1/num_of_assets)
            
        ### Get price relatives vs expected price (SMA)
        price_relatives = self.GetPriceRelatives(activeInsights)     # \tilde{x}_{t+1}

        ### Get step size of next portfolio
        # \bar{x}_{t+1} = 1^T * \tilde{x}_{t+1} / m
        # \lambda_{t+1} = max( 0, ( b_t * \tilde{x}_{t+1} - \epsilon ) / ||\tilde{x}_{t+1}  - \bar{x}_{t+1} * 1|| ^ 2 )
        next_prediction = price_relatives.mean()        # \bar{x}_{t+1}
        assets_mean_dev = price_relatives - next_prediction
        second_norm = (np.linalg.norm(assets_mean_dev)) ** 2
        
        if second_norm == 0.0:
            step_size = 0
        else:
            step_size = (np.dot(self.weight_vector, price_relatives) - self.reversion_threshold) / second_norm
            step_size = max(0, step_size)       # \lambda_{t+1}

        ### Get next portfolio weightings
        # b_{t+1} = b_t - step_size * ( \tilde{x}_{t+1}  - \bar{x}_{t+1} * 1 )
        next_portfolio = self.weight_vector - step_size * assets_mean_dev
        # Normalize
        normalized_portfolio_weight_vector = self.SimplexProjection(next_portfolio)
        # Save normalized result for the next portfolio step
        self.weight_vector = normalized_portfolio_weight_vector

        # Update portfolio state
        for i, insight in enumerate(activeInsights):
            targets[insight] = normalized_portfolio_weight_vector[i]

        return targets
    
    def GetPriceRelatives(self, activeInsights):
        """Get price relatives with reference level of SMA
        Args:
            activeInsights: list of active insights
        Returns:
            array of price relatives vector
        """        
        # Initialize a price vector of the next prices relatives' projection
        next_price_relatives = np.zeros(len(activeInsights))

        ### Get next price relative predictions
        # Using the previous price to simulate assumption of instant reversion
        for i, insight in enumerate(activeInsights):
            symbol_data = self.symbol_data[insight.Symbol]
            next_price_relatives[i] = 1 + insight.Magnitude * insight.Direction \
                if insight.Magnitude is not None \
                else symbol_data.Identity.Current.Value / symbol_data.Sma.Current.Value
        
        return next_price_relatives

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
                self.symbol_data[symbol] = self.MeanReversionSymbolData(algorithm, symbol, self.window_size, self.resolution)

    def SimplexProjection(self, vector, total=1):
        """Normalize the updated portfolio into weight vector:
        v_{t+1} = arg min || v - v_{t+1} || ^ 2
        Implementation from:
        Duchi, J., Shalev-Shwartz, S., Singer, Y., & Chandra, T. (2008, July). 
            Efficient projections onto the l 1-ball for learning in high dimensions.
            In Proceedings of the 25th international conference on Machine learning 
            (pp. 272-279).
        Args:
            vector: unnormalized weight vector
            total: total weight of output, default to be 1, making it a probabilistic simplex
        """
        if total <= 0:
            raise ArgumentException("Total must be > 0 for Euclidean Projection onto the Simplex.")
            
        vector = np.asarray(vector)

        # Sort v into u in descending order
        mu = np.sort(vector)[::-1]
        sv = np.cumsum(mu)

        rho = np.where(mu > (sv - total) / np.arange(1, len(vector) + 1))[0][-1]
        theta = (sv[rho] - total) / (rho + 1)
        w = (vector - theta)
        w[w < 0] = 0
        return w

    class MeanReversionSymbolData:
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