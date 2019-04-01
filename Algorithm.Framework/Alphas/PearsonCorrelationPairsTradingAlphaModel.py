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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework.Alphas import *
from Alphas.BasePairsTradingAlphaModel import BasePairsTradingAlphaModel
from datetime import timedelta
from scipy.stats import pearsonr
import numpy as np
import pandas as pd

class PearsonCorrelationPairsTradingAlphaModel(BasePairsTradingAlphaModel):
    ''' This alpha model is designed to rank every pair combination by its pearson correlation 
    and trade the pair with the hightest correlation
    This model generates alternating long ratio/short ratio insights emitted as a group'''

    def __init__(self, lookback = 15,
            resolution = Resolution.Minute,
            threshold = 1,
            minimumCorrelation = .5):
        '''Initializes a new instance of the PearsonCorrelationPairsTradingAlphaModel class
        Args:
            lookback: lookback period of the analysis
            resolution: analysis resolution
            threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight
            minimumCorrelation: The minimum correlation to consider a tradable pair'''
        super().__init__(lookback, resolution, threshold)
        self.lookback = lookback
        self.resolution = resolution
        self.minimumCorrelation = minimumCorrelation
        self.best_pair = ()

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed.
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        for security in changes.AddedSecurities:
            self.Securities.append(security)

        for security in changes.RemovedSecurities:
            if security in self.Securities:
                self.Securities.remove(security)

        symbols = [ x.Symbol for x in self.Securities ]
        
        history = algorithm.History(symbols, self.lookback, self.resolution).close.unstack(level=0)

        if not history.empty:

            df = self.get_price_dataframe(history)
            stop = len(df.columns)

            corr = dict()

            for i in range(0, stop):
                for j in range(i+1, stop):
                    if (j, i) not in corr:
                        corr[(i, j)] = pearsonr(df.iloc[:,i], df.iloc[:,j])[0]

            corr = sorted(corr.items(), key = lambda kv: kv[1])
            if corr[-1][1] >= self.minimumCorrelation: 
                self.best_pair = (symbols[corr[-1][0][0]], symbols[corr[-1][0][1]])

        super().OnSecuritiesChanged(algorithm, changes)

    def HasPassedTest(self, algorithm, asset1, asset2):
        '''Check whether the assets pass a pairs trading test
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            asset1: The first asset's symbol in the pair
            asset2: The second asset's symbol in the pair
        Returns:
            True if the statistical test for the pair is successful'''
        return self.best_pair is not None and self.best_pair == (asset1, asset2)

    def get_price_dataframe(self, df):
        timezones = { x.Symbol.Value: x.Exchange.TimeZone for x in self.Securities }

        # Use log prices
        df = np.log(df)

        is_single_timeZone = len(set(timezones.values())) == 1

        if not is_single_timeZone:
            series_dict = dict()

            for column in df:
                # Change the dataframe index from data time to UTC time
                to_utc = lambda x: Extensions.ConvertToUtc(x, timezones[column])
                if self.resolution == Resolution.Daily:
                    to_utc = lambda x: Extensions.ConvertToUtc(x, timezones[column]).date()

                data = df[[column]]
                data.index = data.index.map(to_utc)
                series_dict[column] = data[column]

            df = pd.DataFrame(series_dict).dropna()

        return (df - df.shift(1)).dropna()