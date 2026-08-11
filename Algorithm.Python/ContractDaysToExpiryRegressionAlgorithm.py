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
### Regression algorithm asserting the behavior of 'days_to_expiry' and 'dte' on option/future contracts,
### the supported alternative to manual expiry math mixing datetime and date values.
### It also asserts that the reference argument accepts both datetime and date instances.
### </summary>
class ContractDaysToExpiryRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        option = self.add_option("GOOG")
        option.set_filter(lambda u: u.strikes(-2, +2).expiration(0, 180))
        self._option_symbol = option.symbol
        self._contracts_validated = 0

    def on_data(self, slice):
        chain = slice.option_chains.get(self._option_symbol)
        if not chain:
            return

        for contract in chain:
            # The manual shape, with the operand types correctly aligned
            expected = (contract.expiry.date() - self.time.date()).days
            if contract.days_to_expiry() != expected:
                raise AssertionError(f"Expected days_to_expiry() to be {expected} but was {contract.days_to_expiry()}")
            if contract.dte != expected:
                raise AssertionError(f"Expected dte to be {expected} but was {contract.dte}")
            # The reference argument accepts both datetime and date instances
            if contract.days_to_expiry(self.time) != expected:
                raise AssertionError(f"Expected days_to_expiry(datetime) to be {expected} but was {contract.days_to_expiry(self.time)}")
            if contract.days_to_expiry(self.time.date()) != expected:
                raise AssertionError(f"Expected days_to_expiry(date) to be {expected} but was {contract.days_to_expiry(self.time.date())}")
            if contract.days_to_expiry(reference=self.time.date() - timedelta(days=10)) != expected + 10:
                raise AssertionError(f"Expected days_to_expiry(reference=date) to be {expected + 10}")
            self._contracts_validated += 1

    def on_end_of_algorithm(self):
        if self._contracts_validated == 0:
            raise AssertionError("No contracts were validated")
