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

import decimal
from clr import AddReference
AddReference("QuantConnect.Common")

from QuantConnect.Python import PythonData

def OnPythonData(self, data):       
    self.OnData(PythonSlice(data))

class PythonSlice(dict):
    '''PythonSlice class: '''
    def __init__(self, slice):
        for data in slice:
            self[data.Key] = Data(data.Value)

class Data(object):
    '''Python Data class: Converts custom data (PythonData) into a python object'''
    def __init__(self, data):
        members = [attr for attr in dir(data) if not callable(attr) and not attr.startswith("__")]
        for member in members:
            setattr(self, member, getattr(data, member))
        
        if not isinstance(data, PythonData): return

        for member in data.DynamicMembers:
            val = data[member]
            setattr(self, member, decimal.Decimal(val) if isinstance(val, float) else val)