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
### This example demonstrates how to override the option pricing model with the
### <see cref="QLOptionPriceModel"/> for a given option security.
### </summary>
class QLOptionPricingModelRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        equity = self.add_equity("GOOG")
        self._option = self.add_option(equity.symbol)
        self._option.set_filter(lambda u: u.strikes(-2, +2).expiration(0, 180))

        # Set the option price model to the default QL model
        self._option.set_price_model(QLOptionPriceModelProvider.INSTANCE.get_option_price_model(self._option.symbol))

        if not isinstance(self._option.price_model, QLOptionPriceModel):
            raise Exception("Option pricing model was not set to QLOptionPriceModel, which should be the default")

        self._checked = False

    def on_data(self, slice):
        if self._checked:
            return;

        chain = slice.option_chains.get(self._option.symbol)
        if chain is not None:
            if not isinstance(self._option.price_model, QLOptionPriceModel):
                raise Exception("Option pricing model was not set to QLOptionPriceModel");

            for contract in chain:
                theoretical_price = contract.theoretical_price
                iv = contract.implied_volatility
                greeks = contract.greeks
                self.log(f"{contract.symbol}:: Theoretical Price: {theoretical_price}, IV: {iv}, " +
                       f"Delta: {greeks.delta}, Gamma: {greeks.gamma}, Vega: {greeks.vega}, " +
                       f"Theta: {greeks.theta}, Rho: {greeks.rho}, Lambda: {greeks.lambda_}")
                self._checked = True

    def on_end_of_algorithm(self):
        if not self._checked:
            raise Exception("Option chain was never received.")
