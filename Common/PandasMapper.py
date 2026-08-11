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
        if type(other) is Symbol:
            return False
        # For non-strings str.__eq__ returns NotImplemented, which Python resolves to False
        return super().__eq__(other)

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

def _flatten_keys(key):
    '''Extracts the scalar string/Symbol keys from an indexing key, which might be
    a plain scalar or a list/tuple/set of keys (e.g. df[["symbol", "close"]])
    '''
    if isinstance(key, (list, tuple, set)):
        keys = []
        for item in key:
            keys.extend(_flatten_keys(item))
        return keys
    if isinstance(key, str) or type(key) is Symbol:
        return [key]
    return []

def _format_keys(keys, limit=20):
    '''Formats a list of keys for an error message, truncating long lists'''
    formatted = [f"'{key}'" for key in list(keys)[:limit]]
    suffix = ', ...' if len(keys) > limit else ''
    return f"[{', '.join(formatted)}{suffix}]"

def _describe_missing_keys(target, keys):
    '''Builds a self-describing suffix for the KeyError raised when both the mapped and
    original keys are missing: what the requested keys were, which columns/index levels the
    object actually has, and hints for the common mistakes (requesting an index level as a
    column, requesting a symbol that lives in the index, or a symbol with no data at all).
    '''
    # .loc/.iloc/.at indexers keep the DataFrame/Series they index in 'obj';
    # DataFrame.__getitem__/Index.get_loc receive the indexed object itself
    obj = getattr(target, 'obj', target)

    if isinstance(obj, (pd.DataFrame, pd.Series)):
        index = obj.index
    elif isinstance(obj, pd.Index):
        index = obj
    else:
        return ''

    level_names = [name if name is not None else f'level {i}' for i, name in enumerate(index.names)]
    if isinstance(index, pd.MultiIndex):
        index_description = f"index levels {_format_keys(level_names)}"
    elif index.names[0] is not None:
        index_description = f"a '{level_names[0]}' index"
    else:
        index_description = f"a {type(index).__name__}"

    details = []
    if isinstance(obj, pd.DataFrame):
        details.append(f"The DataFrame has columns {_format_keys([str(column) for column in obj.columns])} "
            f"and {index_description}")
    elif isinstance(obj, pd.Series):
        details.append(f"The Series has {index_description}")
    elif isinstance(index, pd.MultiIndex):
        details.append(f"The index has {index_description}")
    else:
        details.append(f"The index is {index_description}")

    hints = []
    for key in keys:
        # requesting an index level as if it were a column, e.g. df[["symbol", "close"]]
        if isinstance(key, str) and key in level_names:
            hints.append(f"'{key}' is an index level, not a column: read it with df.index.get_level_values('{key}') "
                "or move the index levels into columns with df.reset_index()")
            continue
        # requesting a symbol on the wrong axis: it lives in the index, e.g. df["SPY"] on a (symbol, time) indexed frame
        found_in_level = False
        for level in range(index.nlevels):
            try:
                # index membership is symbol-mapped (see wrap_bool_function below)
                found_in_level = key in index.get_level_values(level)
            except TypeError:
                found_in_level = False
            if found_in_level:
                hints.append(f"'{key}' is a value of the '{level_names[level]}' index level: "
                    f"select it with df.loc['{key}'] or df.xs('{key}', level='{level_names[level]}'), "
                    "or use df.get(key) which returns None when the key is missing")
                break
        if found_in_level:
            continue
        # a known symbol that is simply absent, e.g. df.loc[symbol] when the symbol has no data for the requested period
        mapped_key = mapper(key) if isinstance(key, str) else key
        if type(mapped_key) is Symbol:
            hints.append(f"'{key}' is a known Symbol but is not present in this object, it might have no data "
                "for the requested period: use df.get(key) which returns None when the key is missing")

    return '. ' + '. '.join(details + hints)

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
            # The requested keys, including the ones nested in list keys like df[["symbol", "close"]]
            requestedKeys = [key for arg in args[1:] for key in _flatten_keys(arg)]
            requestedKeys += [key for arg in kwargs.values() for key in _flatten_keys(arg)]
            mKey = [str(arg) for arg in newargs if isinstance(arg, str) or isinstance(arg, Symbol)]
            oKey = [str(key) for key in requestedKeys] or [str(arg) for arg in args if isinstance(arg, str) or isinstance(arg, Symbol)]
            message = f"No key found for either mapped or original key. Mapped Key: {mKey}; Original Key: {oKey}"
            # Describe the object being indexed and hint at the usual causes so the error is actionable
            # without a debugging iteration. Never let the description itself mask the KeyError.
            try:
                message += _describe_missing_keys(args[0], requestedKeys) if len(args) > 0 else ''
            except Exception:
                pass
            raise KeyError(message)

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

# Sentinel to detect when the original DataFrame.get found nothing, so a user-provided default is never confused with a miss
_GET_MISS = object()
_original_dataframe_get = pd.core.frame.DataFrame.get

def _dataframe_get(self, key, default=None):
    '''Extends DataFrame.get to also look for symbols in the index: history data frames are
    indexed by symbol (and time), so history.get(symbol) returns the symbol's sub-frame,
    or the default (None) when the symbol has no data instead of raising a KeyError.
    Column lookups keep the original pandas semantics.
    '''
    result = _original_dataframe_get(self, key, default=_GET_MISS)
    if result is not _GET_MISS:
        return result

    # Only symbols/tickers get the index lookup: other keys keep pandas' column-only semantics
    if isinstance(key, str) or type(key) is Symbol:
        try:
            if isinstance(self.index, pd.MultiIndex):
                for level in range(self.index.nlevels):
                    # index membership is symbol-mapped (see wrap_bool_function above)
                    if key in self.index.get_level_values(level):
                        for candidate in (key, mapper(key)):
                            try:
                                return self.xs(candidate, level=level)
                            except (KeyError, TypeError):
                                continue
            elif key in self.index:
                # loc is symbol-mapped already (see wrap_keyerror_function above)
                return self.loc[key]
        except (KeyError, TypeError, ValueError, IndexError):
            pass

    return default

_dataframe_get.__name__ = 'get'
pd.core.frame.DataFrame.get = _dataframe_get

# For compatibility with PandasData.cs usage of this module (Previously wrapped classes)
FrozenList = pdFrozenList
Index = pd.Index
MultiIndex = pd.MultiIndex
Series = pd.Series
DataFrame = pd.DataFrame
