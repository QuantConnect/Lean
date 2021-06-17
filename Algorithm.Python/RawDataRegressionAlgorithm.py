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
from QuantConnect.Data.Auxiliary import *
from QuantConnect.Lean.Engine.DataFeeds import DefaultDataProvider

_ticker = "GOOGL";
_expectedRawPrices = [ 1157.93, 1158.72,
1131.97, 1114.28, 1120.15, 1114.51, 1134.89, 567.55, 571.50, 545.25, 540.63 ]

# <summary>
# In this algorithm we demonstrate how to use the raw data for our securities
# and verify that the behavior is correct.
# </summary>
# <meta name="tag" content="using data" />
# <meta name="tag" content="regression test" />
class RawDataRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 3, 25)
        self.SetEndDate(2014, 4, 7)
        self.SetCash(100000)

        # Set our DataNormalizationMode to raw
        self.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw
        self._googl = self.AddEquity(_ticker, Resolution.Daily).Symbol

        # Get our factor file for this regression
        dataProvider = DefaultDataProvider()
        mapFileProvider = LocalDiskMapFileProvider()
        mapFileProvider.Initialize(dataProvider)
        factorFileProvider = LocalDiskFactorFileProvider()
        factorFileProvider.Initialize(mapFileProvider, dataProvider)

        # Get our factor file for this regression
        self._factorFile = factorFileProvider.Get(self._googl)


    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings(self._googl, 1)

        if data.Bars.ContainsKey(self._googl):
            googlData = data.Bars[self._googl]

            # Assert our volume matches what we expected
            expectedRawPrice = _expectedRawPrices.pop(0)
            if expectedRawPrice != googlData.Close:
                # Our values don't match lets try and give a reason why
                dayFactor = self._factorFile.GetPriceScaleFactor(googlData.Time)
                probableRawPrice = googlData.Close / dayFactor  # Undo adjustment

                raise Exception("Close price was incorrect; it appears to be the adjusted value"
                    if expectedRawPrice == probableRawPrice else
                   "Close price was incorrect; Data may have changed.")
