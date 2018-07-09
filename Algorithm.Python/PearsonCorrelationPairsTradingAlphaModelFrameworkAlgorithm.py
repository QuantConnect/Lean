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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Selection import *
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Alphas.BasePairsTradingAlphaModel import BasePairsTradingAlphaModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from Risk.NullRiskManagementModel import NullRiskManagementModel
from datetime import timedelta
from scipy.stats import pearsonr
import numpy as np

### <summary>
### Framework algorithm that uses the PearsonCorrelationPairsTradingAlphaModel.
### This model extendes BasePairsTradingAlphaModel and uses Pearson correlation
### to rank the pairs trading candidates and use the best candidate to trade.
### </summary>
class PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm(QCAlgorithmFramework):
    '''Framework algorithm that uses the PearsonCorrelationPairsTradingAlphaModel.
    This model extendes BasePairsTradingAlphaModel and uses Pearson correlation
    to rank the pairs trading candidates and use the best candidate to trade.'''

    def Initialize(self):

        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)

        self.SetUniverseSelection(ManualUniverseSelectionModel(
            Symbol.Create('AIG', SecurityType.Equity, Market.USA),
            Symbol.Create('BAC', SecurityType.Equity, Market.USA),
            Symbol.Create('IBM', SecurityType.Equity, Market.USA),
            Symbol.Create('SPY', SecurityType.Equity, Market.USA)))

        self.SetAlpha(self.PearsonCorrelationPairsTradingAlphaModel(360, timedelta(minutes = 15)))
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
        self.SetRiskManagement(NullRiskManagementModel())


    class PearsonCorrelationPairsTradingAlphaModel(BasePairsTradingAlphaModel):
        ''' This alpha model is designed to rank every pair combination by its pearson correlation 
        and trade the pair with the hightest correlation
        This model generates alternating long ratio/short ratio insights emitted as a group'''

        def __init__(self, lookback, period, threshold = 1):
            '''Initializes a new instance of the PearsonCorrelationPairsTradingAlphaModel class
            Args:
                lookback: lookback period to evaluate the historical correlation
                period: Period over which this insight is expected to come to fruition
                threshold: The percent [0, 100] deviation of the ratio from the mean before emitting an insight'''
            super().__init__(period, threshold)
            self.lookback = lookback
            self.best_pair = ()

        def OnSecuritiesChanged(self, algorithm, changes):

            for security in changes.AddedSecurities:
                self.Securities.append(security)

            for security in changes.RemovedSecurities:
                if security in self.Securities:
                    self.Securities.remove(security)

            symbols = [ x.Symbol for x in self.Securities ]

            history = algorithm.History(symbols, self.lookback, Resolution.Daily).close.unstack(level=0)
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
            return self.best_pair == (asset1, asset2)