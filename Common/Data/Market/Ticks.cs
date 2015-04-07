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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Ticks collection which implements an IDictionary-string-list of ticks. This way users can iterate over the string indexed ticks of the requested symbol.
    /// </summary>
    /// <remarks>Ticks are timestamped to the nearest second in QuantConnect</remarks>
    public class Ticks : BaseData, IDictionary<string, List<Tick>>
    {
        /********************************************************
        * CLASS VARIABLES
        *********************************************************/
        //Private storage of ticks collection
        private Dictionary<string, List<Tick>> _ticks = new Dictionary<string, List<Tick>>();

        /********************************************************
        * CONSTRUCTOR METHODS
        *********************************************************/
        /// <summary>
        /// Default constructor for the ticks collection
        /// </summary>
        /// <param name="frontier"></param>
        public Ticks(DateTime frontier)
        {
            Time = frontier;
            Symbol = "";
            Value = 0;
            DataType = MarketDataType.Tick;
        }

        /********************************************************
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Ticks array reader - fetch the data from the QC storage and feed it line by line into the
        /// system.
        /// </summary>
        /// <param name="datafeed">Who is requesting this data, backtest or live streamer</param>
        /// <param name="config">Symbols, Resolution, DataType</param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of the reader day:</param>
        /// <returns>Enumerable iterator for returning each line of the required data.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clonable Interface; create a new instance of the object
        /// - Don't need to implement for Ticks array, each symbol-subscription is treated separately.
        /// </summary>
        /// <returns>BaseData clone of the Ticks Array</returns>
        /// <exception cref="NotImplementedException"></exception>
        public override BaseData Clone()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the source file for this tick subscription
        /// </summary>
        /// <param name="datafeed">Source of the datafeed / type of strings we'll be receiving</param>
        /// <param name="config">Configuration for the subscription</param>
        /// <param name="date">Date of the source file requested.</param>
        /// <returns>String URL Source File</returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add ticks to this Ticks collection
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="key">String ticker</param>
        /// <param name="value">TradeBar value</param>
        public void Add(string key, List<Tick> value)
        {
            _ticks.Add(key, value);
        }

        /// <summary>
        /// Get enumerator for tick collection.
        /// </summary>
        /// <returns>Enumerator for indexing dictionary</returns>
        /// <remarks>IDictionary implementation</remarks>
        IEnumerator<KeyValuePair<string, List<Tick>>> IEnumerable<KeyValuePair<string, List<Tick>>>.GetEnumerator()
        {
            return _ticks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, List<Tick>>)this).GetEnumerator();
        }

        /// <summary>
        /// IDictionary :: IsReadOnly Implementation
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Count the number of symbols in this collection of ticks.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public int Count
        {
            get
            {
                return _ticks.Count;
            }
        }

        /// <summary>
        /// Remove a specific symbol tick from the tick collection.
        /// </summary>
        /// <param name="key">Key ticker</param>
        /// <remarks>IDictionary implementation</remarks>
        public bool Remove(string key)
        {
            return _ticks.Remove(key);
        }

        /// <summary>
        /// Remove a key value pair of symbols-list items from the tick collection.
        /// </summary>
        /// <param name="kvp">KVP Remove</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True</returns>
        public bool Remove(KeyValuePair<string, List<Tick>> kvp)
        {
            return _ticks.Remove(kvp.Key);
        }

        /// <summary>
        /// Check if the tick collection contains this key value pair.
        /// </summary>
        /// <param name="kvp"></param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Boolean True-False on whether the list contains the key value pair</returns>
        public bool Contains(KeyValuePair<string, List<Tick>> kvp)
        {
            return _ticks.ContainsKey(kvp.Key);
        }

        /// <summary>
        /// Check if we have this asset-symbol in the tick collection.
        /// </summary>
        /// <param name="key">Asset we're looking for.</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Boolean true-false on whether the collection contains the asset we're seeking.</returns>
        public bool ContainsKey(string key)
        {
            return _ticks.ContainsKey(key);
        }

        /// <summary>
        /// Clear the Tick collection of items
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public void Clear()
        {
            _ticks.Clear();
        }

        /// <summary>
        /// Add a new symbol list to the dictionary.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="kvp"></param>
        public void Add(KeyValuePair<string, List<Tick>> kvp)
        {
            _ticks.Add(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Array of List-Tick Values from tick collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<List<Tick>> Values
        {
            get
            {
                return _ticks.Values;
            }
        }

        /// <summary>
        /// Collection of symbols-assets contained in this dictionary
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<string> Keys
        {
            get
            {
                return _ticks.Keys;
            }
        }

        /// <summary>
        /// Indexer for the tick collection. Access the underlying list of ticks by its symbol string.
        /// </summary>
        /// <param name="key">string symbol of the asset we're seeking</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>List of ticks corresponding to this asset symbol key.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public List<Tick> this[string key]
        {
            get
            {
                List<Tick> output;

                if (_ticks.TryGetValue(key, out output))
                {
                    return output;
                }

                throw new KeyNotFoundException(string.Format("'{0}' wasn't found in the Ticks object, likely because there was no-data at this moment in time. Please check the data exists before accessing it with data.ContainsKey(\"{0}\")", key));
            }
            set
            {
                _ticks[key] = value;
            }
        }

        /// <summary>
        /// Try get the list of tick with the matching symbol
        /// </summary>
        /// <param name="key">Symbol/Asset we're searching for</param>
        /// <param name="listTicks">Output list of ticks, or null if not found</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Boolean true when successfully locating tick list, or false when not found.</returns>
        public bool TryGetValue(string key, out List<Tick> listTicks)
        {
            return _ticks.TryGetValue(key, out listTicks);
        }

        /// <summary>
        /// Copy the underlying array from position arrayIndex into a second array.
        /// </summary>
        /// <param name="array">Destination Array</param>
        /// <param name="arrayIndex">Starting index</param>
        /// <remarks>IDictionary implementation</remarks>
        public void CopyTo(KeyValuePair<string, List<Tick>>[] array, int arrayIndex)
        {
            Copy(this, array, arrayIndex);
        }

        /// <summary>
        /// Copy Tick Array Generic Implementation
        /// </summary>
        /// <typeparam name="T">Type of copy to.</typeparam>
        /// <param name="source">Source of the copy.</param>
        /// <param name="array">Array destinations</param>
        /// <param name="arrayIndex">Index of current copy.</param>
        private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if ((array.Length - arrayIndex) < source.Count)
            {
                throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");
            }

            foreach (var item in source)
            {
                array[arrayIndex++] = item;
            }
        }
    }
} // End QC Namespace
