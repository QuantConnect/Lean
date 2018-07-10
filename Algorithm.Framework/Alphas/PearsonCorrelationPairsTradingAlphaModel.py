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

class PearsonCorrelationPairsTradingAlphaModel(BasePairsTradingAlphaModel):
    ''' This alpha model is designed to rank every pair combination by its pearson correlation 
    and trade the pair with the hightest correlation
    This model generates alternating long ratio/short ratio insights emitted as a group'''

    def __init__(self, lookback = 15,
            resolution = Resolution.Minute,
            threshold = 1):
        '''Initializes a new instance of the PearsonCorrelationPairsTradingAlphaModel class
        Args:
            lookback: lookback period of the analysis
            resolution: analysis resolution
            threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
        super().__init__(lookback, resolution, threshold)
        self.lookback = lookback
        self.resolution = resolution
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
        df = (np.log(history) - np.log(history.shift(1))).dropna()
        stop = len(df.columns)

        corr = dict()

        for i in range(0, stop):
            for j in range(i+1, stop):
                if (j, i) not in corr:
                    corr[(i, j)] = pearsonr(df.iloc[:,i], df.iloc[:,j])[0]

        corr = sorted(corr.items(), key = lambda kv: kv[1])

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
        return self.best_pair == (asset1, asset2)