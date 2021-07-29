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

def mapper(key):
    '''Maps a Symbol object or a Symbol Ticker (string) to the string representation of
    Symbol SecurityIdentifier.If cannot map, returns the object
    '''
    keyType = type(key)
    if keyType is Symbol:
        return str(key.ID)
    if keyType is str:
        reserved = ['high', 'low', 'open', 'close']
        if key in reserved:
            return key
        kvp = SymbolCache.TryGetSymbol(key, None)
        if kvp[0]:
            return str(kvp[1].ID)
    if keyType is list:
        return [mapper(x) for x in key]
    if keyType is tuple:
        return tuple([mapper(x) for x in key])
    if keyType is dict:
        return { k: mapper(v) for k, v in key.items()}
    return key

def wrap_function(f):
    '''Wraps function f with g.
    Function g converts the args / kwargs to use alternative index keys
      and then calls original function with the mapped args
    '''
    def wrapped_function(*args, **kwargs):

        # Map args & kwargs, if wrapped function fails because key, execute with original
        # Allows for df, Series, etc indexing for keys like 'SPY' if they exist
        try:
            newargs = args
            newkwargs = kwargs

            if len(args) > 1:
                newargs = mapper(args)
            if len(kwargs) > 0:
                newkwargs = mapper(kwargs)

            result = f(*newargs, **newkwargs)
            return result
        except KeyError:
            pass

        result = f(*args, **kwargs)
        return result

    wrapped_function.__name__ = f.__name__
    return wrapped_function

# Wrap all core __getItem__ and loc functions that are shared, yet still throw key errors if index not found
pd.core.indexing._LocationIndexer.__getitem__ = wrap_function(pd.core.indexing._LocationIndexer.__getitem__)
pd.core.indexing._ScalarAccessIndexer.__getitem__ = wrap_function(pd.core.indexing._ScalarAccessIndexer.__getitem__)
pd.core.indexes.base.Index.get_loc = wrap_function(pd.core.indexes.base.Index.get_loc)

# Wrap __contains__ to support Python syntax like 'SPY' in DataFrame 
pd.core.indexes.base.Index.__contains__ = wrap_function(pd.core.indexes.base.Index.__contains__)

# For older version of pandas we may need to wrap extra functions
if (int(pd.__version__.split('.')[0]) < 1):
    pd.core.indexes.base.Index.get_value = wrap_function(pd.core.indexes.base.Index.get_value)

# For compatibility with PandasData.cs usage of this module (Previously wrapped classes)
FrozenList = pdFrozenList
Index = pd.Index
MultiIndex = pd.MultiIndex
Series = pd.Series
DataFrame = pd.DataFrame
