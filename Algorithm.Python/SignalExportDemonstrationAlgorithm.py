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
### This algorithm sends current portfolio targets to different 3rd party API's.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class SignalExportDemonstrationAlgorithm(QCAlgorithm):
    ''' This algorithm sends current portfolio targets to different 3rd party API's. '''

    def Initialize(self):
        ''' Initialize the date and resolution to then add two securities '''
        self.UniverseSettings.Resolution = Resolution.Minute

        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 7)
        self.SetCash(50000)

        self.Collective2Apikey = "" # Replace this value with your Collective2 API key
        self.Collective2SystemId = 1 # Replace this value with your Collective2 system ID
        self.spy = self.AddEquity("SPY").Symbol
        self.es = self.AddFuture("ES").Symbol

    ''' When the data is ready, set the targets and the signal export providers '''
    def OnData(self, slice):
        self.SignalExportManager.SetSignalExportProviders(Collective2SignalExport(self.Collective2Apikey, self.Collective2SystemId, self.Portfolio))
        self.SignalExportManager.SetTargetPortfolio(PortfolioTarget(self.spy, 0.2), PortfolioTarget(self.es, 0.8))


