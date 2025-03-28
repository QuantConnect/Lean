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

class OptionIndicatorsMirrorContractsRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2014, 6, 5)
        self.set_end_date(2014, 6, 9)
        self.set_cash(100000)

        equity = self.add_equity("AAPL", Resolution.DAILY).symbol
        option = Symbol.create_option("AAPL", Market.USA, OptionStyle.AMERICAN, OptionRight.PUT, 650, datetime(2014, 6, 21))
        self.add_option_contract(option, Resolution.DAILY)
        # add the call counter side of the mirrored pair
        mirror_option = Symbol.create_option("AAPL", Market.USA, OptionStyle.AMERICAN, OptionRight.CALL, 650, datetime(2014, 6, 21))
        self.add_option_contract(mirror_option, Resolution.DAILY)

        self._delta = self.d(option, mirror_option, option_model = OptionPricingModelType.BINOMIAL_COX_ROSS_RUBINSTEIN, iv_model = OptionPricingModelType.BLACK_SCHOLES)
        self._gamma = self.g(option, mirror_option, option_model = OptionPricingModelType.FORWARD_TREE, iv_model = OptionPricingModelType.BLACK_SCHOLES)
        self._vega = self.v(option, mirror_option, option_model = OptionPricingModelType.FORWARD_TREE, iv_model = OptionPricingModelType.BLACK_SCHOLES)
        self._theta = self.t(option, mirror_option, option_model = OptionPricingModelType.FORWARD_TREE, iv_model = OptionPricingModelType.BLACK_SCHOLES)
        self._rho = self.r(option, mirror_option, option_model = OptionPricingModelType.FORWARD_TREE, iv_model = OptionPricingModelType.BLACK_SCHOLES)

        # A custom IV indicator with custom calculation of IV
        risk_free_rate_model = InterestRateProvider()
        dividend_yield_model = DividendYieldProvider(equity)
        self._implied_volatility = CustomImpliedVolatility(option, mirror_option, risk_free_rate_model, dividend_yield_model)
        self.register_indicator(option, self._implied_volatility, QuoteBarConsolidator(timedelta(1)))
        self.register_indicator(mirror_option, self._implied_volatility, QuoteBarConsolidator(timedelta(1)))
        self.register_indicator(equity, self._implied_volatility, TradeBarConsolidator(timedelta(1)))

        # custom IV smoothing function: assume the lower IV is more "fair"
        smoothing_func = lambda iv, mirror_iv: min(iv, mirror_iv)
        # set the smoothing function
        self._delta.implied_volatility.set_smoothing_function(smoothing_func)
        self._gamma.implied_volatility.set_smoothing_function(smoothing_func)
        self._vega.implied_volatility.set_smoothing_function(smoothing_func)
        self._theta.implied_volatility.set_smoothing_function(smoothing_func)
        self._rho.implied_volatility.set_smoothing_function(smoothing_func)

    def on_end_of_algorithm(self) -> None:
        if not self._implied_volatility.is_ready or not self._delta.is_ready or not self._gamma.is_ready \
        or not self._vega.is_ready or not self._theta.is_ready or not self._rho.is_ready:
            raise AssertionError("Expected IV/greeks calculated")

        self.debug(f"""Implied Volatility: {self._implied_volatility.current.value},
Delta: {self._delta.current.value},
Gamma: {self._gamma.current.value},
Vega: {self._vega.current.value},
Theta: {self._theta.current.value},
Rho: {self._rho.current.value}""")

class CustomImpliedVolatility(ImpliedVolatility):
    def __init__(self, option, mirror_option, risk_free_rate_model, dividend_yield_model):
        super().__init__(option, risk_free_rate_model, dividend_yield_model, mirror_option)
        self.set_smoothing_function(lambda iv, mirror_iv: iv)

    def calculate_iv(self, time_till_expiry: float) -> float:
        try:
            return brentq(self.f, 1e-7, 2.0, args=(time_till_expiry), xtol=1e-4, maxiter=100)
        except:
            print("ImpliedVolatility.calculate_i_v(): Fail to converge, returning 0.")
            return 0.0

    # we demonstate put-call parity calculation here, but note that it is not suitable for American options
    def f(self, vol: float, time_till_expiry: float) -> float:
        call_black_price = OptionGreekIndicatorsHelper.black_theoretical_price(
            vol, self.underlying_price.current.value, self.strike, time_till_expiry, self.risk_free_rate.current.value, self.dividend_yield.current.value, OptionRight.CALL)
        put_black_price = OptionGreekIndicatorsHelper.black_theoretical_price(
            vol, self.underlying_price.current.value, self.strike, time_till_expiry, self.risk_free_rate.current.value, self.dividend_yield.current.value, OptionRight.PUT)
        return self.price.current.value + self.opposite_price.current.value - call_black_price - put_black_price
