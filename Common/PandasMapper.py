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

'''
Lean Pandas Remapper
Wraps key indexing functions of Pandas to remap keys to SIDs when accessing dataframes.
Allowing support for indexing of Lean created Indexes with tickers like "SPY", Symbol objs, and SIDs

'''

import pandas as pd
from pandas.core.indexes.frozen import FrozenList as pdFrozenList

from clr import AddReference
AddReference("QuantConnect.Common")
from QuantConnect import *

class PandasColumn(str):
    '''
    PandasColumn is a wrapper class for a pandas column that allows for the column to be used as a key
    and properly compared to strings, regardless of whether it's a C# or Python string
    (since the hash of a C# string and the same Python string are different).
    '''

    def __new__(cls, key):
        return super().__new__(cls, key)

    def __eq__(self, other):
        # We need this since Lean created data frames might contain Symbol objects in the indexes
        return super().__eq__(other) and type(other) is not Symbol

    def __hash__(self):
        return super().__hash__()

def mapper(key):
    '''Maps a Symbol object or a Symbol Ticker (string) to the string representation of
    Symbol SecurityIdentifier.If cannot map, returns the object
    '''
    keyType = type(key)
    if keyType is tuple:
        return tuple(mapper(x) for x in key)
    if keyType is str:
        kvp = SymbolCache.try_get_symbol(key, None)
        if kvp[0]:
            return kvp[1]
        return key
    if keyType is list:
        return [mapper(x) for x in key]
    if keyType is dict:
        return {k: mapper(v) for k, v in key.items()}
    return key

def wrap_keyerror_function(f):
    '''Wraps function f with wrapped_function, used for functions that throw KeyError when not found.
    wrapped_function converts the args / kwargs to use alternative index keys and then calls the function.
    If this fails we fall back to the original key and try it as well, if they both fail we throw our error.
    '''
    def wrapped_function(*args, **kwargs):
        # Map args & kwargs and execute function
        try:
            newargs = args
            newkwargs = kwargs

            if len(args) > 1:
                newargs = mapper(args)
            if len(kwargs) > 0:
                newkwargs = mapper(kwargs)

            return f(*newargs, **newkwargs)
        except KeyError as e:
            pass

        # Execute original
        # Allows for df, Series, etc indexing for keys like 'SPY' if they exist
        try:
            return f(*args, **kwargs)
        except KeyError as e:
            mKey = [str(arg) for arg in newargs if isinstance(arg, str) or isinstance(arg, Symbol)]
            oKey = [str(arg) for arg in args if isinstance(arg, str) or isinstance(arg, Symbol)]
            raise KeyError(f"No key found for either mapped or original key. Mapped Key: {mKey}; Original Key: {oKey}")

    wrapped_function.__name__ = f.__name__
    return wrapped_function

def wrap_bool_function(f):
    '''Wraps function f with wrapped_function, used for functions that reply true/false if key is found.
    wrapped_function attempts with the original args, if its false, it converts the args / kwargs to use
    alternative index keys and then attempts with the mapped args.
    '''
    def wrapped_function(*args, **kwargs):

        # Try the original args; if true just return true
        originalResult = f(*args, **kwargs)
        if originalResult:
            return originalResult

        # Try our mapped args; return this result regardless
        newargs = args
        newkwargs = kwargs

        if len(args) > 1:
            newargs = mapper(args)
        if len(kwargs) > 0:
            newkwargs = mapper(kwargs)

        return f(*newargs, **newkwargs)

    wrapped_function.__name__ = f.__name__
    return wrapped_function


# Wrap all core indexing functions that are shared, yet still throw key errors if index not found
pd.core.indexing._LocationIndexer.__getitem__ = wrap_keyerror_function(pd.core.indexing._LocationIndexer.__getitem__)
pd.core.indexing._ScalarAccessIndexer.__getitem__ = wrap_keyerror_function(pd.core.indexing._ScalarAccessIndexer.__getitem__)
pd.core.indexes.base.Index.get_loc = wrap_keyerror_function(pd.core.indexes.base.Index.get_loc)

# Wrap our DF _getitem__ as well, even though most pathways go through the above functions
# There are cases like indexing with an array that need to be mapped earlier to stop KeyError from arising
pd.core.frame.DataFrame.__getitem__ = wrap_keyerror_function(pd.core.frame.DataFrame.__getitem__)

# For older version of pandas we may need to wrap extra functions
if (int(pd.__version__.split('.')[0]) < 1):
    pd.core.indexes.base.Index.get_value = wrap_keyerror_function(pd.core.indexes.base.Index.get_value)

# Special cases where we need to wrap a function that won't throw a keyerror when not found but instead returns true or false
# Wrap __contains__ to support Python syntax like 'SPY' in DataFrame
pd.core.indexes.base.Index.__contains__ = wrap_bool_function(pd.core.indexes.base.Index.__contains__)

# For compatibility with PandasData.cs usage of this module (Previously wrapped classes)
FrozenList = pdFrozenList
Index = pd.Index
MultiIndex = pd.MultiIndex
Series = pd.Series
DataFrame = pd.DataFrame
