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
from Alphas.BasePairsTradingAlphaModel import BasePairsTradingAlphaModel
from scipy.stats import pearsonr

class PearsonCorrelationPairsTradingAlphaModel(BasePairsTradingAlphaModel):
    ''' This alpha model is designed to rank every pair combination by its pearson correlation
    and trade the pair with the hightest correlation
    This model generates alternating long ratio/short ratio insights emitted as a group'''

    def __init__(self, lookback = 15,
            resolution = Resolution.MINUTE,
            threshold = 1,
            minimum_correlation = .5):
        '''Initializes a new instance of the PearsonCorrelationPairsTradingAlphaModel class
        Args:
            lookback: lookback period of the analysis
            resolution: analysis resolution
            threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight
            minimum_correlation: The minimum correlation to consider a tradable pair'''
        super().__init__(lookback, resolution, threshold)
        self.lookback = lookback
        self.resolution = resolution
        self.minimum_correlation = minimum_correlation
        self.best_pair = ()

    def on_securities_changed(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        for security in changes.added_securities:
            self.securities.add(security)

        for security in changes.removed_securities:
            if security in self.securities:
                self.securities.remove(security)

        symbols = sorted([ x.symbol for x in self.securities ])

        history = algorithm.history(symbols, self.lookback, self.resolution)

        if not history.empty:
            history = history.close.unstack(level=0)

            df = self.get_price_dataframe(history)
            stop = len(df.columns)

            corr = dict()

            for i in range(0, stop):
                for j in range(i+1, stop):
                    if (j, i) not in corr:
                        corr[(i, j)] = pearsonr(df.iloc[:,i], df.iloc[:,j])[0]

            corr = sorted(corr.items(), key = lambda kv: kv[1])
            if corr[-1][1] >= self.minimum_correlation:
                self.best_pair = (symbols[corr[-1][0][0]], symbols[corr[-1][0][1]])

        super().on_securities_changed(algorithm, changes)

    def has_passed_test(self, algorithm, asset1, asset2):
        '''Check whether the assets pass a pairs trading test
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            asset1: The first asset's symbol in the pair
            asset2: The second asset's symbol in the pair
        Returns:
            True if the statistical test for the pair is successful'''
        return self.best_pair is not None and self.best_pair[0] == asset1 and self.best_pair[1] == asset2

    def get_price_dataframe(self, df):
        timezones = { x.symbol.value: x.exchange.time_zone for x in self.securities }

        # Use log prices
        df = np.log(df)

        is_single_timeZone = len(set(timezones.values())) == 1

        if not is_single_timeZone:
            series_dict = dict()

            for column in df:
                # Change the dataframe index from data time to UTC time
                to_utc = lambda x: Extensions.convert_to_utc(x, timezones[column])
                if self.resolution == Resolution.DAILY:
                    to_utc = lambda x: Extensions.convert_to_utc(x, timezones[column]).date()

                data = df[[column]]
                data.index = data.index.map(to_utc)
                series_dict[column] = data[column]

            df = pd.DataFrame(series_dict).dropna()

        return (df - df.shift(1)).dropna()
