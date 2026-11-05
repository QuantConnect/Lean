/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using Python.Runtime;
using QuantConnect.Interfaces;

namespace QuantConnect.Storage
{
    /// <summary>
    /// The <see cref="ObjectStore"/> of Python algorithms and notebooks: adds the save and read methods that take
    /// and return Python objects, such as dictionaries and pandas DataFrames
    /// </summary>
    public class PythonObjectStore : ObjectStore
    {
        // Python helpers for the PyObject APIs, lazily created under the GIL
        private static PyObject _jsonSerializeMethod;
        private static PyObject _jsonDeserializeMethod;
        private static PyObject _dataFrameSerializeMethod;
        private static PyObject _dataFrameDeserializeMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonObjectStore"/> class
        /// </summary>
        /// <param name="store">The <see cref="IObjectStore"/> instance to wrap</param>
        /// <param name="warn">Receives the warnings about sanitized keys, the engine log by default</param>
        public PythonObjectStore(IObjectStore store, Action<string> warn = null)
            : base(store, warn)
        {
        }

        /// <summary>
        /// Returns the JSON deserialized object data for the specified path as Python objects
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The deserialized Python object</returns>
        public PyObject ReadJson(string path)
        {
            var json = Read(path);
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var pyJson = json.ToPython();
                return _jsonDeserializeMethod.Invoke(pyJson);
            }
        }

        /// <summary>
        /// Returns the object data for the specified path as a pandas DataFrame, read as JSON if the path has
        /// a '.json' extension and as CSV otherwise, so a <see cref="SaveDataframe"/> round trip restores the frame.
        /// A CSV index in ISO-8601 format is restored as a time index. A saved Series comes back as a
        /// one-column DataFrame, call squeeze() on it to get the Series
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The pandas DataFrame</returns>
        public PyObject ReadDataframe(string path)
        {
            var serialized = Read(path);
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var pySerialized = serialized.ToPython();
                using var pyPath = path.ToPython();
                return _dataFrameDeserializeMethod.Invoke(pySerialized, pyPath);
            }
        }

        /// <summary>
        /// Saves the given Python dictionary, list, tuple or .NET object in JSON format for the specified path,
        /// tolerating nested types the standard json module rejects: datetime/date/time, pandas Timestamp and
        /// numpy datetime64 are stored in ISO-8601 format, timedelta and timedelta64 as ISO-8601 durations,
        /// Decimal and numpy numbers as numbers, NaN, infinity and NaT as null, .NET objects (Symbol included)
        /// as the C# <see cref="ObjectStore.SaveJson{T}"/> overload writes them (a NaN inside one is Newtonsoft's "NaN"
        /// string) and non-string dictionary keys as strings. pandas DataFrames and Series are rejected, use
        /// <see cref="SaveDataframe"/>, and so is any other type that would not round trip. A top-level Python
        /// str, int, float or bool is not accepted, use <see cref="ObjectStore.SaveText"/> for text
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="obj">The Python object to be saved</param>
        /// <returns>True if the object was saved successfully</returns>
        /// <exception cref="PythonException">A TypeError naming the value that cannot be serialized</exception>
        public bool SaveJson(string path, PyObject obj)
        {
            string json;
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var result = _jsonSerializeMethod.Invoke(obj);
                json = result.As<string>();
            }
            return Save(path, json);
        }

        /// <summary>
        /// Saves the given pandas DataFrame (or Series) for the specified path, in JSON format if the
        /// path has a '.json' extension and as CSV otherwise
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="dataFrame">The pandas DataFrame or Series to be saved</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveDataframe(string path, PyObject dataFrame)
        {
            string serialized;
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var pyPath = (path ?? string.Empty).ToPython();
                using var result = _dataFrameSerializeMethod.Invoke(dataFrame, pyPath);
                serialized = result.As<string>();
            }
            return Save(path, serialized);
        }

        /// <summary>
        /// Lazily creates the Python helper methods backing the PyObject APIs. Must be called under the GIL
        /// </summary>
        private static void EnsurePythonHelpers()
        {
            if (_jsonSerializeMethod == null)
            {
                var module = PyModule.FromString("object_store_helpers", @"from json import dumps, loads
from datetime import datetime, date, time, timedelta
from decimal import Decimal
from math import isfinite
import numpy
import pandas
import System
import Newtonsoft.Json as newtonsoft

def _key(key):
    # json only accepts str/int/float/bool/None keys; a .NET key is its string, like a .NET dictionary key
    if isinstance(key, float) and not isfinite(key):
        raise TypeError(f'save_json() cannot use {key} as a dictionary key')
    if isinstance(key, (str, int, float)) or key is None:
        return key
    if isinstance(key, System.Object):
        return str(key)
    normalized = _normalize(key)
    if isinstance(normalized, (str, int, float)) and not isinstance(normalized, bool):
        return normalized
    raise TypeError(f'save_json() cannot use a {type(key).__name__} as a dictionary key')

def _normalize(value):
    if isinstance(value, dict):
        return {_key(k): _normalize(v) for k, v in value.items()}
    if isinstance(value, (list, tuple, set, frozenset)):
        return [_normalize(v) for v in value]
    if isinstance(value, (str, int)) or value is None:
        return value
    if isinstance(value, float):
        # NaN and infinity are not valid JSON
        return value if isfinite(value) else None
    if isinstance(value, (datetime, date, time)):
        return None if value is pandas.NaT else value.isoformat()
    if isinstance(value, timedelta):
        return pandas.Timedelta(value).isoformat()
    if isinstance(value, Decimal):
        return _normalize(float(value))
    if isinstance(value, numpy.ndarray):
        # tolist() turns nanosecond datetime64/timedelta64 into integers: convert those element-wise
        return [_normalize(v) for v in value] if value.dtype.kind in 'mM' else _normalize(value.tolist())
    if isinstance(value, numpy.datetime64):
        return None if numpy.isnat(value) else pandas.Timestamp(value).isoformat()
    if isinstance(value, numpy.timedelta64):
        return None if numpy.isnat(value) else pandas.Timedelta(value).isoformat()
    if isinstance(value, numpy.generic):
        return _normalize(value.item())
    if isinstance(value, (pandas.DataFrame, pandas.Series)):
        raise TypeError(f'save_json() does not accept a pandas {type(value).__name__}, use save_dataframe() instead')
    if isinstance(value, System.Object):
        # .NET objects serialize like the C# SaveJson<T> does
        return loads(newtonsoft.JsonConvert.SerializeObject(value))
    raise TypeError(f'save_json() cannot serialize a {type(value).__name__}: convert it to a dict, list or str first')

def serialize(value):
    return dumps(_normalize(value), allow_nan=False)

def deserialize(json_string):
    return loads(json_string)

def serialize_dataframe(value, key):
    if not hasattr(value, 'to_csv'):
        raise TypeError(f'save_dataframe() expects a pandas DataFrame or Series but received {type(value).__name__}')
    if key.lower().endswith('.json'):
        return value.to_json()
    return value.to_csv()

def deserialize_dataframe(serialized, key):
    import pandas
    from io import StringIO
    if key.lower().endswith('.json'):
        return pandas.read_json(StringIO(serialized))
    # to_csv() writes the index as the first column, datetimes in ISO-8601: restore a time index when it parses as one
    frame = pandas.read_csv(StringIO(serialized), index_col=0)
    if pandas.api.types.is_string_dtype(frame.index):
        try:
            frame.index = pandas.to_datetime(frame.index, format='ISO8601')
        except (ValueError, TypeError):
            pass
    return frame
");
                _jsonDeserializeMethod = module.GetAttr("deserialize");
                _dataFrameSerializeMethod = module.GetAttr("serialize_dataframe");
                _dataFrameDeserializeMethod = module.GetAttr("deserialize_dataframe");
                // last so partial initialization is never observed
                _jsonSerializeMethod = module.GetAttr("serialize");
            }
        }
    }
}
