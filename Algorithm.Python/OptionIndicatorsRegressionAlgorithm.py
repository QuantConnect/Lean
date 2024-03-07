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

class OptionIndicatorsRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 6, 5)
        self.SetEndDate(2014, 6, 7)
        self.SetCash(1000000)

        self.aapl = self.AddEquity("AAPL", Resolution.Daily).Symbol
        self.option = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Put, 505, datetime(2014, 6, 27))
        self.AddOptionContract(self.option)

        self.impliedVolatility = self.IV(self.option, optionModel = OptionPricingModelType.BlackScholes, period = 2)
        self.delta = self.D(self.option, optionModel = OptionPricingModelType.BinomialCoxRossRubinstein, ivModel = OptionPricingModelType.BlackScholes)
        self.gamma = self.G(self.option, optionModel = OptionPricingModelType.ForwardTree, ivModel = OptionPricingModelType.BlackScholes)

    def OnData(self, slice):
        if slice.Bars.ContainsKey(self.aapl) and slice.QuoteBars.ContainsKey(self.option):
            underlyingDataPoint = IndicatorDataPoint(self.aapl, slice.Time, slice.Bars[self.aapl].Close)
            optionDataPoint = IndicatorDataPoint(self.option, slice.Time, slice.QuoteBars[self.option].Close)

            self.impliedVolatility.Update(underlyingDataPoint)
            self.impliedVolatility.Update(optionDataPoint)

            self.delta.Update(underlyingDataPoint)
            self.delta.Update(optionDataPoint)

            self.gamma.Update(underlyingDataPoint)
            self.gamma.Update(optionDataPoint)

    def OnEndOfAlgorithm(self):
        if self.impliedVolatility.Current.Value == 0 or self.delta.Current.Value == 0 or self.gamma.Current.Value == 0:
            raise Exception("Expected IV/greeks calculated")

        self.Debug(f"""Implied Volatility: {self.impliedVolatility.Current.Value},
Delta: {self.delta.Current.Value},
Gamma: {self.gamma.Current.Value}""")
