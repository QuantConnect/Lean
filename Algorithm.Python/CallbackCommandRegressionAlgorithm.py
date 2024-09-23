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

class InvalidCommand():
    variable = 10

class VoidCommand():
    quantity = 0
    target = []
    parameters = {}
    targettime = None

    def run(self, algo: QCAlgorithm) -> bool | None:
        if not self.targettime or self.targettime != algo.time:
            return
        tag = self.parameters["tag"]
        algo.order(self.target[0], self.get_quantity(), tag=tag)

    def get_quantity(self):
        return self.quantity

class BoolCommand(Command):
    result = False

    def run(self, algo: QCAlgorithm) -> bool | None:
        trade_ibm = self.my_custom_method()
        if trade_ibm:
            algo.buy("IBM", 1)
        return trade_ibm

    def my_custom_method(self):
        return self.result

### <summary>
### Regression algorithm asserting the behavior of different callback commands call
### </summary>
class CallbackCommandRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self.add_equity("SPY")
        self.add_equity("IBM")
        self.add_equity("BAC")

        self.add_command(VoidCommand)
        self.add_command(BoolCommand)

        threw_exception = False
        try:
            self.add_command(InvalidCommand)
        except:
            threw_exception = True
        if not threw_exception:
            raise ValueError('InvalidCommand did not throw!')

    def on_command(self, data):
        self.buy(data.symbol, data.parameters["quantity"])
        return True # False, None
