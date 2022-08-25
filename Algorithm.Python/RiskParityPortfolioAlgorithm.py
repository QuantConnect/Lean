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
from Portfolio.RiskParityPortfolioConstructionModel import *

class RiakParityPortfolioAlgorithm(QCAlgorithm):
    '''Example algorithm of using RiskParityPortfolioConstructionModel'''

    def Initialize(self):
        self.SetStartDate(2021, 2, 21)  # Set Start Date
        self.SetEndDate(2021, 3, 30)
        self.SetCash(100000)  # Set Strategy Cash
        self.SetSecurityInitializer(lambda security: security.SetMarketPrice(self.GetLastKnownPrice(security)))
        
        self.AddEquity("SPY", Resolution.Daily)
        self.AddEquity("AAPL", Resolution.Daily)

        self.AddAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1)))
        self.SetPortfolioConstruction(RiskParityPortfolioConstructionModel())