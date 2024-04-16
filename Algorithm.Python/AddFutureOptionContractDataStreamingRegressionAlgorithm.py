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
### This regression algorithm tests that we receive the expected data when
### we add future option contracts individually using <see cref="AddFutureOptionContract"/>
### </summary>
class AddFutureOptionContractDataStreamingRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.on_data_reached = False
        self.invested = False
        self.symbols_received = []
        self.expected_symbols_received = []
        self.data_received = {}

        self.set_start_date(2020, 1, 4)
        self.set_end_date(2020, 1, 8)

        self.es20h20 = self.add_future_contract(
            Symbol.create_future(Futures.Indices.SP500EMini, Market.CME, datetime(2020, 3, 20)),
            Resolution.MINUTE).symbol

        self.es19m20 = self.add_future_contract(
            Symbol.create_future(Futures.Indices.SP500EMini, Market.CME, datetime(2020, 6, 19)),
            Resolution.MINUTE).symbol

        # Get option contract lists for 2020/01/05 (timedelta(days=1)) because Lean has local data for that date
        optionChains = self.option_chain_provider.get_option_contract_list(self.es20h20, self.time + timedelta(days=1))
        optionChains += self.option_chain_provider.get_option_contract_list(self.es19m20, self.time + timedelta(days=1))

        for optionContract in optionChains:
            self.expected_symbols_received.append(self.add_future_option_contract(optionContract, Resolution.MINUTE).symbol)

    def on_data(self, data: Slice):
        if not data.has_data:
            return

        self.on_data_reached = True
        hasOptionQuoteBars = False

        for qb in data.quote_bars.values():
            if qb.symbol.security_type != SecurityType.FUTURE_OPTION:
                continue

            hasOptionQuoteBars = True

            self.symbols_received.append(qb.symbol)
            if qb.symbol not in self.data_received:
                self.data_received[qb.symbol] = []

            self.data_received[qb.symbol].append(qb)

        if self.invested or not hasOptionQuoteBars:
            return

        if data.contains_key(self.es20h20) and data.contains_key(self.es19m20):
            self.set_holdings(self.es20h20, 0.2)
            self.set_holdings(self.es19m20, 0.2)

            self.invested = True

    def on_end_of_algorithm(self):
        self.symbols_received = list(set(self.symbols_received))
        self.expected_symbols_received = list(set(self.expected_symbols_received))

        if not self.on_data_reached:
            raise AssertionError("OnData() was never called.")
        if len(self.symbols_received) != len(self.expected_symbols_received):
            raise AssertionError(f"Expected {len(self.expected_symbols_received)} option contracts Symbols, found {len(self.symbols_received)}")

        missingSymbols = [expectedSymbol for expectedSymbol in self.expected_symbols_received if expectedSymbol not in self.symbols_received]
        if any(missingSymbols):
            raise AssertionError(f'Symbols: "{", ".join(missingSymbols)}" were not found in OnData')

        for expectedSymbol in self.expected_symbols_received:
            data = self.data_received[expectedSymbol]
            for dataPoint in data:
                dataPoint.end_time = datetime(1970, 1, 1)

            nonDupeDataCount = len(set(data))
            if nonDupeDataCount < 1000:
                raise AssertionError(f"Received too few data points. Expected >=1000, found {nonDupeDataCount} for {expectedSymbol}")
