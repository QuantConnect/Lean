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
### This regression algorithm has examples of how to add an equity indicating the <see cref="DataNormalizationMode"/>
### directly with the <see cref="QCAlgorithm.AddEquity"/> method instead of using the <see cref="Equity.SetDataNormalizationMode"/> method.
### </summary>
class SetEquityDataNormalizationModeOnAddEquity(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 7)

        spyNormalizationMode = DataNormalizationMode.Raw
        ibmNormalizationMode = DataNormalizationMode.Adjusted
        aigNormalizationMode = DataNormalizationMode.TotalReturn

        self._priceRanges = {}

        spyEquity = self.AddEquity("SPY", Resolution.Minute, dataNormalizationMode=spyNormalizationMode)
        self.CheckEquityDataNormalizationMode(spyEquity, spyNormalizationMode)
        self._priceRanges[spyEquity] = (167.28, 168.37)

        ibmEquity = self.AddEquity("IBM", Resolution.Minute, dataNormalizationMode=ibmNormalizationMode)
        self.CheckEquityDataNormalizationMode(ibmEquity, ibmNormalizationMode)
        self._priceRanges[ibmEquity] = (135.864131052, 136.819606508)

        aigEquity = self.AddEquity("AIG", Resolution.Minute, dataNormalizationMode=aigNormalizationMode)
        self.CheckEquityDataNormalizationMode(aigEquity, aigNormalizationMode)
        self._priceRanges[aigEquity] = (48.73, 49.10)

    def OnData(self, slice):
        for equity, (minExpectedPrice, maxExpectedPrice) in self._priceRanges.items():
            if equity.HasData and (equity.Price < minExpectedPrice or equity.Price > maxExpectedPrice):
                raise Exception(f"{equity.Symbol}: Price {equity.Price} is out of expected range [{minExpectedPrice}, {maxExpectedPrice}]")

    def CheckEquityDataNormalizationMode(self, equity, expectedNormalizationMode):
        subscriptions = [x for x in self.SubscriptionManager.Subscriptions if x.Symbol == equity.Symbol]
        if any([x.DataNormalizationMode != expectedNormalizationMode for x in subscriptions]):
            raise Exception(f"Expected {equity.Symbol} to have data normalization mode {expectedNormalizationMode} but was {subscriptions[0].DataNormalizationMode}")


