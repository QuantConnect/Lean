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
import math

class Decimal(float):
    '''This is required for backwards compatibility with users performing operations over expected decimal types.
    NOTE: previously we converted C# decimal into python Decimal, but now its converted to python float due to performance.'''
    def __init__(self, obj):
        self = obj

    def is_finite(self):
        '''Return True if the argument is a finite number, and False if the argument
        is infinite or a NaN.'''
        return not self.is_infinite() and not self.is_nan()

    def is_infinite(self):
        '''Return True if the argument is either positive or negative infinity and
        False otherwise.'''
        return math.isinf(self)

    def is_nan(self):
        '''Return True if the argument is a (quiet or signaling) NaN and False
        otherwise.'''
        return math.isnan(self)