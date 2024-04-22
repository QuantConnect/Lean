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
from Portfolio.RiskParityPortfolioOptimizer import RiskParityPortfolioOptimizer

### <summary>
### Risk Parity Portfolio Construction Model
### </summary>
### <remarks>Spinu, F. (2013). An algorithm for computing risk parity weights. Available at SSRN 2297383.
### Available at https://papers.ssrn.com/sol3/Papers.cfm?abstract_id=2297383</remarks>
class RiskParityPortfolioConstructionModel(PortfolioConstructionModel):
    def __init__(self,
                 rebalance = Resolution.DAILY,
                 portfolio_bias = PortfolioBias.LONG_SHORT,
                 lookback = 1,
                 period = 252,
                 resolution = Resolution.DAILY,
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
            optimizer(class): Method used to compute the portfolio weights"""
        super().__init__()
        if portfolio_bias == PortfolioBias.SHORT:
            raise ArgumentException("Long position must be allowed in RiskParityPortfolioConstructionModel.")

        self.lookback = lookback
        self.period = period
        self.resolution = resolution
        self.sign = lambda x: -1 if x < 0 else (1 if x > 0 else 0)

        self.optimizer = RiskParityPortfolioOptimizer() if optimizer is None else optimizer

        self._symbol_data_by_symbol = {}

        # If the argument is an instance of Resolution or Timedelta
        # Redefine rebalancing_func
        rebalancing_func = rebalance
        if isinstance(rebalance, int):
            rebalance = Extensions.to_time_span(rebalance)
        if isinstance(rebalance, timedelta):
            rebalancing_func = lambda dt: dt + rebalance
        if rebalancing_func:
            self.set_rebalancing_func(rebalancing_func)

    def determine_target_percent(self, active_insights):
        """Will determine the target percent for each insight
        Args:
            active_insights: list of active insights
        Returns:
            dictionary of insight and respective target weight
        """
        targets = {}

        # If we have no insights just return an empty target list
        if len(active_insights) == 0:
            return targets

        symbols = [insight.symbol for insight in active_insights]

        # Create a dictionary keyed by the symbols in the insights with an pandas.series as value to create a data frame
        returns = { str(symbol) : data.return_ for symbol, data in self._symbol_data_by_symbol.items() if symbol in symbols }
        returns = pd.DataFrame(returns)

        # The portfolio optimizer finds the optional weights for the given data
        weights = self.optimizer.optimize(returns)
        weights = pd.Series(weights, index = returns.columns)

        # Create portfolio targets from the specified insights
        for insight in active_insights:
            targets[insight] = weights[str(insight.symbol)]

        return targets

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        super().on_securities_changed(algorithm, changes)
        for removed in changes.removed_securities:
            symbol_data = self._symbol_data_by_symbol.pop(removed.symbol, None)
            symbol_data.reset()
            algorithm.unregister_indicator(symbol_data.roc)

        # initialize data for added securities
        symbols = [ x.symbol for x in changes.added_securities ]
        history = algorithm.history(symbols, self.lookback * self.period, self.resolution)
        if history.empty: return

        tickers = history.index.levels[0]
        for ticker in tickers:
            symbol = SymbolCache.get_symbol(ticker)

            if symbol not in self._symbol_data_by_symbol:
                symbol_data = self.RiskParitySymbolData(symbol, self.lookback, self.period)
                symbol_data.warm_up_indicators(history.loc[ticker])
                self._symbol_data_by_symbol[symbol] = symbol_data
                algorithm.register_indicator(symbol, symbol_data.roc, self.resolution)

    class RiskParitySymbolData:
        '''Contains data specific to a symbol required by this model'''
        def __init__(self, symbol, lookback, period):
            self._symbol = symbol
            self.roc = RateOfChange(f'{symbol}.roc({lookback})', lookback)
            self.roc.updated += self.on_rate_of_change_updated
            self.window = RollingWindow[IndicatorDataPoint](period)

        def reset(self):
            self.roc.updated -= self.on_rate_of_change_updated
            self.roc.reset()
            self.window.reset()

        def warm_up_indicators(self, history):
            for tuple in history.itertuples():
                self.roc.update(tuple.Index, tuple.close)

        def on_rate_of_change_updated(self, roc, value):
            if roc.is_ready:
                self.window.add(value)

        def add(self, time, value):
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
            return '{}: {:.2%}'.format(self.roc.name, self.window[0])
