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
from scipy.optimize import brentq

class OptionIndicatorsRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2014, 6, 5)
        self.SetEndDate(2014, 6, 7)
        self.SetCash(100000)

        equity = self.AddEquity("AAPL", Resolution.Daily).Symbol
        option = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Put, 650, datetime(2014, 6, 21))
        self.AddOptionContract(option, Resolution.Daily)
        # add the call counter side of the mirrored pair
        mirror_option = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 650, datetime(2014, 6, 21))
        self.AddOptionContract(mirror_option, Resolution.Daily)

        self.delta = self.D(option, mirror_option, optionModel = OptionPricingModelType.BinomialCoxRossRubinstein, ivModel = OptionPricingModelType.BlackScholes)
        self.gamma = self.G(option, mirror_option, optionModel = OptionPricingModelType.ForwardTree, ivModel = OptionPricingModelType.BlackScholes)
        self.vega = self.V(option, mirror_option, optionModel = OptionPricingModelType.ForwardTree, ivModel = OptionPricingModelType.BlackScholes)
        self.theta = self.T(option, mirror_option, optionModel = OptionPricingModelType.ForwardTree, ivModel = OptionPricingModelType.BlackScholes)
        self.rho = self.R(option, mirror_option, optionModel = OptionPricingModelType.ForwardTree, ivModel = OptionPricingModelType.BlackScholes)

        # A custom IV indicator with custom calculation of IV
        riskFreeRateModel = InterestRateProvider()
        dividendYieldModel = DividendYieldProvider(equity)
        self.impliedVolatility = CustomImpliedVolatility(option, mirror_option, riskFreeRateModel, dividendYieldModel)
        self.RegisterIndicator(option, self.impliedVolatility, QuoteBarConsolidator(timedelta(1)))
        self.RegisterIndicator(mirror_option, self.impliedVolatility, QuoteBarConsolidator(timedelta(1)))
        self.RegisterIndicator(equity, self.impliedVolatility, TradeBarConsolidator(timedelta(1)))

        # custom IV smoothing function: assume the lower IV is more "fair"
        smoothing_func = lambda iv, mirror_iv: min(iv, mirror_iv)
        # set the smoothing function
        self.delta.ImpliedVolatility.SetSmoothingFunction(smoothing_func)
        self.gamma.ImpliedVolatility.SetSmoothingFunction(smoothing_func)
        self.vega.ImpliedVolatility.SetSmoothingFunction(smoothing_func)
        self.theta.ImpliedVolatility.SetSmoothingFunction(smoothing_func)
        self.rho.ImpliedVolatility.SetSmoothingFunction(smoothing_func)

    def OnEndOfAlgorithm(self):
        if self.impliedVolatility.Current.Value == 0 or self.delta.Current.Value == 0 or self.gamma.Current.Value == 0 \
        or self.vega.Current.Value == 0 or self.theta.Current.Value == 0 or self.rho.Current.Value == 0:
            raise Exception("Expected IV/greeks calculated")

        self.Debug(f"""Implied Volatility: {self.impliedVolatility.Current.Value},
Delta: {self.delta.Current.Value},
Gamma: {self.gamma.Current.Value},
Vega: {self.vega.Current.Value},
Theta: {self.theta.Current.Value},
Rho: {self.rho.Current.Value}""")

class CustomImpliedVolatility(ImpliedVolatility):
    def __init__(self, option, mirror_option, risk_free_rate_model, dividend_yield_model):
        super().__init__(option, risk_free_rate_model, dividend_yield_model, mirror_option, period=2)
        self.SetSmoothingFunction(lambda iv, mirror_iv: iv)

    def CalculateIV(self, timeTillExpiry: float) -> float:
        try:
            return brentq(self.f, 1e-7, 2.0, args=(timeTillExpiry), xtol=1e-4, maxiter=100)
        except:
            print("ImpliedVolatility.CalculateIV(): Fail to converge, returning 0.")
            return 0.0

    # we demonstate put-call parity calculation here, but note that it is not suitable for American options
    def f(self, vol: float, time_till_expiry: float) -> float:
        call_black_price = OptionGreekIndicatorsHelper.BlackTheoreticalPrice(
            vol, UnderlyingPrice.Current.Value, Strike, timeTillExpiry, RiskFreeRate.Current.Value, DividendYield.Current.Value, OptionRight.Call)
        put_black_price = OptionGreekIndicatorsHelper.BlackTheoreticalPrice(
            vol, UnderlyingPrice.Current.Value, Strike, timeTillExpiry, RiskFreeRate.Current.Value, DividendYield.Current.Value, OptionRight.Put)
        return Price.Current.Value + OppositePrice.Current.Value - call_black_price - put_black_price
