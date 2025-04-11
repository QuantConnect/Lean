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

    def run(self, algo: IAlgorithm) -> bool:
        if not self.targettime or self.targettime != algo.time:
            return False
        tag = self.parameters["tag"]
        algo.order(self.target[0], self.get_quantity(), tag=tag)
        return True

    def get_quantity(self):
        return self.quantity

class BoolCommand(Command):
    something_else = {}
    array_test = []
    result = False

    def run(self, algo: QCAlgorithm) -> bool:
        trade_ibm = self.my_custom_method()
        if trade_ibm:
            algo.debug(f"BoolCommand.run: {str(self)}")
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

        bool_command = BoolCommand()
        bool_command.result = True
        bool_command.something_else = { "Property": 10 }
        bool_command.array_test = [ "SPY", "BTCUSD" ]
        link = self.link(bool_command)
        if "&command%5barray_test%5d%5b0%5d=SPY&command%5barray_test%5d%5b1%5d=BTCUSD&command%5bresult%5d=True&command%5bsomething_else%5d%5bProperty%5d=10&command%5b%24type%5d=BoolCommand" not in link:
            raise ValueError(f'Invalid link was generated! {link}')

        potential_command = VoidCommand()
        potential_command.target = [ "BAC" ]
        potential_command.quantity = 10
        potential_command.parameters = { "tag": "Signal X" }

        command_link = self.link(potential_command)
        if "&command%5btarget%5d%5b0%5d=BAC&command%5bquantity%5d=10&command%5bparameters%5d%5btag%5d=Signal+X&command%5b%24type%5d=VoidCommand" not in command_link:
            raise ValueError(f'Invalid link was generated! {command_link}')
        self.notify.email("email@address", "Trade Command Event", f"Signal X trade\nFollow link to trigger: {command_link}")

        untyped_command_link = self.link({ "symbol": "SPY", "parameters": { "quantity": 10 } })
        if "&command%5bsymbol%5d=SPY&command%5bparameters%5d%5bquantity%5d=10" not in untyped_command_link:
            raise ValueError(f'Invalid link was generated! {untyped_command_link}')
        self.notify.email("email@address", "Untyped Command Event", f"Signal Y trade\nFollow link to trigger: {untyped_command_link}")

        # We need to create a project on QuantConnect to test the broadcast_command method
        # and use the project_id in the broadcast_command call
        self.project_id = 21805137

        # All live deployments receive the broadcasts below
        broadcast_result = self.broadcast_command(potential_command)
        broadcast_result2 = self.broadcast_command({ "symbol": "SPY", "parameters": { "quantity": 10 } })

    def on_command(self, data: object) -> bool:
        self.debug(f"on_command: {str(data)}")
        self.buy(data.symbol, data.parameters["quantity"])
        return True # False, None
