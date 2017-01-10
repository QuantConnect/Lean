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
AddReference("System.Core")
AddReference("System.Collections")
AddReference("QuantConnect.Common")

from System import DateTime, Func, Decimal, String
from System.Collections.Generic import IEnumerable
from QuantConnect import Symbol
from QuantConnect.Data.UniverseSelection import CoarseFundamental
from QuantConnect.Data.Fundamental import FineFundamental


''' AddUniverse wrapper'''
def AddUniverse(self, *args):
    
    selector = None
    addUniverse = super(self.__class__, self).AddUniverse
    
    if isinstance(args[0], str):
        selector = Func[DateTime, IEnumerable[String]](args[1])
        addUniverse(args[0], selector)
        return

    for arg in args:
        if callable(arg):
            if selector is None:
                selector = Func[IEnumerable[CoarseFundamental], IEnumerable[Symbol]](arg)
            else:
                fine = Func[IEnumerable[FineFundamental], IEnumerable[Symbol]](arg)
                addUniverse(selector, fine)
                return

    addUniverse(selector)


''' SetBenchmark wrapper'''
def SetBenchmark(self, object):
    if callable(object):
        object = Func[DateTime, Decimal](object)
    super(self.__class__, self).SetBenchmark(object)