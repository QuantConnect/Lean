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
from Portfolio.MaximumSharpeRatioPortfolioOptimizer import MaximumSharpeRatioPortfolioOptimizer
from itertools import groupby
from numpy import dot, transpose
from numpy.linalg import inv

### <summary>
### Provides an implementation of Black-Litterman portfolio optimization. The model adjusts equilibrium market
### returns by incorporating views from multiple alpha models and therefore to get the optimal risky portfolio
### reflecting those views. If insights of all alpha models have None magnitude or there are linearly dependent
### vectors in link matrix of views, the expected return would be the implied excess equilibrium return.
### The interval of weights in optimization method can be changed based on the long-short algorithm.
### The default model uses the 0.0025 as weight-on-views scalar parameter tau and
### MaximumSharpeRatioPortfolioOptimizer that accepts a 63-row matrix of 1-day returns.
### </summary>
class BlackLittermanOptimizationPortfolioConstructionModel(PortfolioConstructionModel):
    def __init__(self,
                 rebalance = Resolution.DAILY,
                 portfolio_bias = PortfolioBias.LONG_SHORT,
                 lookback = 1,
                 period = 63,
                 resolution = Resolution.DAILY,
                 risk_free_rate = 0,
                 delta = 2.5,
                 tau = 0.05,
                 optimizer = None):
        """Initialize the model
        Args:
            rebalance: Rebalancing parameter. If it is a timedelta, date rules or Resolution, it will be converted into a function.
                              If None will be ignored.
                              The function returns the next expected rebalance time for a given algorithm UTC DateTime.
                              The function returns null if unknown, in which case the function will be called again in the
                              next loop. Returning current time will trigger rebalance.
            portfolio_bias: Specifies the bias of the portfolio (Short, Long/Short, Long)
            lookback(int): Historical return lookback period
            period(int): The time interval of history price to calculate the weight
            resolution: The resolution of the history price
            risk_free_rate(float): The risk free rate
            delta(float): The risk aversion coeffficient of the market portfolio
            tau(float): The model parameter indicating the uncertainty of the CAPM prior"""
        super().__init__()
        self.lookback = lookback
        self.period = period
        self.resolution = resolution
        self.risk_free_rate = risk_free_rate
        self.delta = delta
        self.tau = tau
        self.portfolio_bias = portfolio_bias

        lower = 0 if portfolio_bias == PortfolioBias.LONG else -1
        upper = 0 if portfolio_bias == PortfolioBias.SHORT else 1
        self.optimizer = MaximumSharpeRatioPortfolioOptimizer(lower, upper, risk_free_rate) if optimizer is None else optimizer

        self.sign = lambda x: -1 if x < 0 else (1 if x > 0 else 0)
        self.symbol_data_by_symbol = {}

        # If the argument is an instance of Resolution or Timedelta
        # Redefine rebalancing_func
        rebalancing_func = rebalance
        if isinstance(rebalance, int):
            rebalance = Extensions.to_time_span(rebalance)
        if isinstance(rebalance, timedelta):
            rebalancing_func = lambda dt: dt + rebalance
        if rebalancing_func:
            self.set_rebalancing_func(rebalancing_func)

    def should_create_target_for_insight(self, insight):
        return PortfolioConstructionModel.filter_invalid_insight_magnitude(self.algorithm, [ insight ])

    def determine_target_percent(self, last_active_insights):
        targets = {}

        # Get view vectors
        p, q = self.get_views(last_active_insights)
        if p is not None:
            returns = dict()
            # Updates the BlackLittermanSymbolData with insights
            # Create a dictionary keyed by the symbols in the insights with an pandas.Series as value to create a data frame
            for insight in last_active_insights:
                symbol = insight.symbol
                symbol_data = self.symbol_data_by_symbol.get(symbol, self.BlackLittermanSymbolData(symbol, self.lookback, self.period))
                if insight.magnitude is None:
                    self.algorithm.set_run_time_error(ArgumentNullException('BlackLittermanOptimizationPortfolioConstructionModel does not accept \'None\' as Insight.magnitude. Please make sure your Alpha Model is generating Insights with the Magnitude property set.'))
                    return targets
                symbol_data.add(insight.generated_time_utc, insight.magnitude)
                returns[symbol] = symbol_data.return_

            returns = pd.DataFrame(returns)

            # Calculate prior estimate of the mean and covariance
            pi, sigma = self.get_equilibrium_return(returns)

            # Calculate posterior estimate of the mean and covariance
            pi, sigma = self.apply_blacklitterman_master_formula(pi, sigma, p, q)

            # Create portfolio targets from the specified insights
            weights = self.optimizer.optimize(returns, pi, sigma)
            weights = pd.Series(weights, index = sigma.columns)

            for symbol, weight in weights.items():
                for insight in last_active_insights:
                    if str(insight.symbol) == str(symbol):
                        # don't trust the optimizer
                        if self.portfolio_bias != PortfolioBias.LONG_SHORT and self.sign(weight) != self.portfolio_bias:
                            weight = 0
                        targets[insight] = weight
                        break

        return targets

    def get_target_insights(self):
        # Get insight that haven't expired of each symbol that is still in the universe
        active_insights = filter(self.should_create_target_for_insight,
            self.algorithm.insights.get_active_insights(self.algorithm.utc_time))

        # Get the last generated active insight for each symbol
        last_active_insights = []
        for source_model, f in groupby(sorted(active_insights, key = lambda ff: ff.source_model), lambda fff: fff.source_model):
            for symbol, g in groupby(sorted(list(f), key = lambda gg: gg.symbol), lambda ggg: ggg.symbol):
                last_active_insights.append(sorted(g, key = lambda x: x.generated_time_utc)[-1])
        return last_active_insights

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # Get removed symbol and invalidate them in the insight collection
        super().on_securities_changed(algorithm, changes)

        for security in changes.removed_securities:
            symbol = security.symbol
            symbol_data = self.symbol_data_by_symbol.pop(symbol, None)
            if symbol_data is not None:
                symbol_data.reset()

        # initialize data for added securities
        added_symbols = { x.symbol: x.exchange.time_zone for x in changes.added_securities }
        history = algorithm.history(list(added_symbols.keys()), self.lookback * self.period, self.resolution)

        if history.empty:
            return

        history = history.close.unstack(0)
        symbols = history.columns

        for symbol, timezone in added_symbols.items():
            if str(symbol) not in symbols:
                continue

            symbol_data = self.symbol_data_by_symbol.get(symbol, self.BlackLittermanSymbolData(symbol, self.lookback, self.period))
            for time, close in history[symbol].items():
                utc_time = Extensions.convert_to_utc(time, timezone)
                symbol_data.update(utc_time, close)

            self.symbol_data_by_symbol[symbol] = symbol_data

    def apply_blacklitterman_master_formula(self, Pi, Sigma, P, Q):
        '''Apply Black-Litterman master formula
        http://www.blacklitterman.org/cookbook.html
        Args:
            Pi: Prior/Posterior mean array
            Sigma: Prior/Posterior covariance matrix
            P: A matrix that identifies the assets involved in the views (size: K x N)
            Q: A view vector (size: K x 1)'''
        ts = self.tau * Sigma

        # Create the diagonal Sigma matrix of error terms from the expressed views
        omega = np.dot(np.dot(P, ts), P.T) * np.eye(Q.shape[0])
        if np.linalg.det(omega) == 0:
            return Pi, Sigma

        A = np.dot(np.dot(ts, P.T), inv(np.dot(np.dot(P, ts), P.T) + omega))

        Pi = np.squeeze(np.asarray((
            np.expand_dims(Pi, axis=0).T +
            np.dot(A, (Q - np.expand_dims(np.dot(P, Pi.T), axis=1))))
            ))

        M = ts - np.dot(np.dot(A, P), ts)
        Sigma = (Sigma + M) * self.delta

        return Pi, Sigma

    def get_equilibrium_return(self, returns):
        '''Calculate equilibrium returns and covariance
        Args:
            returns: Matrix of returns where each column represents a security and each row returns for the given date/time (size: K x N)
        Returns:
            equilibrium_return: Array of double of equilibrium returns
            cov: Multi-dimensional array of double with the portfolio covariance of returns (size: K x K)'''

        size = len(returns.columns)
        # equal weighting scheme
        W = np.array([1/size]*size)
        # the covariance matrix of excess returns (N x N matrix)
        cov = returns.cov()*252
        # annualized return
        annual_return = np.sum(((1 + returns.mean())**252 -1) * W)
        # annualized variance of return
        annual_variance = dot(W.T, dot(cov, W))
        # the risk aversion coefficient
        risk_aversion = (annual_return - self.risk_free_rate ) / annual_variance
        # the implied excess equilibrium return Vector (N x 1 column vector)
        equilibrium_return = dot(dot(risk_aversion, cov), W)

        return equilibrium_return, cov

    def get_views(self, insights):
        '''Generate views from multiple alpha models
        Args
            insights: Array of insight that represent the investors' views
        Returns
            P: A matrix that identifies the assets involved in the views (size: K x N)
            Q: A view vector (size: K x 1)'''
        try:
            P = {}
            Q = {}
            symbols = set(insight.symbol for insight in insights)

            for model, group in groupby(insights, lambda x: x.source_model):
                group = list(group)

                up_insights_sum = 0.0
                dn_insights_sum = 0.0
                for insight in group:
                    if insight.direction == InsightDirection.UP:
                        up_insights_sum = up_insights_sum + np.abs(insight.magnitude)
                    if insight.direction == InsightDirection.DOWN:
                        dn_insights_sum = dn_insights_sum + np.abs(insight.magnitude)

                q = up_insights_sum if up_insights_sum > dn_insights_sum else dn_insights_sum
                if q == 0:
                    continue

                Q[model] = q

                # generate the link matrix of views: P
                P[model] = dict()
                for insight in group:
                    value = insight.direction * np.abs(insight.magnitude)
                    P[model][insight.symbol] = value / q
                # Add zero for other symbols that are listed but active insight
                for symbol in symbols:
                    if symbol not in P[model]:
                        P[model][symbol] = 0

            Q = np.array([[x] for x in Q.values()])
            if len(Q) > 0:
                P = np.array([list(x.values()) for x in P.values()])
                return P, Q
        except:
            pass

        return None, None


    class BlackLittermanSymbolData:
        '''Contains data specific to a symbol required by this model'''
        def __init__(self, symbol, lookback, period):
            self._symbol = symbol
            self.roc = RateOfChange(f'{symbol}.roc({lookback})', lookback)
            self.roc.updated += self.on_rate_of_change_updated
            self.window = RollingWindow(period)

        def reset(self):
            self.roc.updated -= self.on_rate_of_change_updated
            self.roc.reset()
            self.window.reset()

        def update(self, utc_time, close):
            self.roc.update(utc_time, close)

        def on_rate_of_change_updated(self, roc, value):
            if roc.is_ready:
                self.window.add(value)

        def add(self, time, value):
            if self.window.samples > 0 and self.window[0].end_time == time:
                return

            item = IndicatorDataPoint(self._symbol, time, value)
            self.window.add(item)

        @property
        def return_(self):
            return pd.Series(
                data = [x.value for x in self.window],
                index = [x.end_time for x in self.window])

        @property
        def is_ready(self):
            return self.window.is_ready

        def __str__(self, **kwargs):
            return f'{self.roc.name}: {(1 + self.window[0])**252 - 1:.2%}'
