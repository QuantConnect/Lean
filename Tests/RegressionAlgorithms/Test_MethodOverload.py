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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")
from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Selection import *

class Test_MethodOverload(QCAlgorithm):
    def Initialize(self):
        self.AddEquity("SPY", Resolution.Second)
        self.sma = self.SMA('SPY', 20)
        self.std = self.STD('SPY', 20)
        self.a = A()

    def OnData(self, data):
        pass

    def call_plot_std_test(self):
        self.Plot('STD', self.std)

    def call_plot_sma_test(self):
        self.Plot('SMA', self.sma)

    def call_plot_number_test(self):
        self.Plot('NUMBER', 0.1)

    def call_plot_throw_test(self):
        self.Plot("ERROR", self.Name)

    def call_plot_throw_managed_test(self):
        self.Plot("ERROR", self.Portfolio)

    def call_plot_throw_pyobject_test(self):
        self.Plot("ERROR", self.a)

    def no_method_match(self):
        self.Log(1)


class A(object):
   def __init__(self):
       pass