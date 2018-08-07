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
AddReference("QuantConnect.Common")
from System import *
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
import decimal 

class Test_PythonExceptionInterpreter(QCAlgorithm):
    def Initialize(self):
        pass

    def key_error(self):
        x = dict()['SPY']

    def no_method_match(self):
        self.SetCash('SPY')

    def unsupported_operand(self):
        x = decimal.Decimal(1) * 1.1

    def zero_division_error(self):
        x = 1 / 0