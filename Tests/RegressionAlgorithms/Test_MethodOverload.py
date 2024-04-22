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

class Test_MethodOverload(QCAlgorithm):
    def initialize(self):
        self.add_equity("SPY", Resolution.SECOND)
        self.sma = self.sma('SPY', 20)
        self.std = self.std('SPY', 20)
        self.a = A()

    def on_data(self, data):
        pass

    def call_plot_std_test(self):
        self.plot('STD', self.std)

    def call_plot_sma_test(self):
        self.plot('SMA', self.sma)

    def call_plot_number_test(self):
        self.plot('NUMBER', 0.1)

    def call_plot_throw_test(self):
        self.plot("ERROR", self.name)

    def call_plot_throw_managed_test(self):
        self.plot("ERROR", self.portfolio)

    def call_plot_throw_pyobject_test(self):
        self.plot("ERROR", self.a)

    def no_method_match(self):
        self.log(1)


class A(object):
   def __init__(self):
       pass
