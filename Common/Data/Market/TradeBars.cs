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
    /// Collection of TradeBars to create a data type for generic data handler:
    /// </summary>
    public class TradeBars : BaseData, IDictionary<string, TradeBar>
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        /// <summary>
        /// Id of this TradeBars Object
        /// </summary>
        public int Id = 0;

        //Internally store the tradebars in a basic dictionary:
        private readonly Dictionary<string, TradeBar> _tradeBars = new Dictionary<string, TradeBar>();

        /******************************************************** 
        * CLASS CONSTRUCTOR:
        *********************************************************/
        /// <summary>
        /// TradeBars default initializer sets the time and values to zero.
        /// </summary>
        public TradeBars() 
        {
            Time = new DateTime();
            Value = 0;
            Symbol = "";
            DataType = MarketDataType.TradeBar;
        }
        
        /// <summary>
        /// Default constructor for tradebars collection at this time frontier: all tradebars in this collection occurred at this time.
        /// </summary>
        /// <param name="frontier">Time frontier of the algorithm and bars in this collection</param>
        public TradeBars(DateTime frontier) 
        {
            Time = frontier;
            Value = 0;
            Symbol = "";
            DataType = MarketDataType.TradeBar;
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// TradeBar Reader: Fetch the data from the QC storage and feed it line by line into the engine.
        /// </summary>
        /// <param name="datafeed">Where are we getting this datafeed from - backtesing or live.</param>
        /// <param name="config">Symbols, Resolution, DataType are all sourced from the Subscription Config object</param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of the reader request, only used when the source file changes daily.</param>
        /// <remarks>This is unused for the Lean Engine but required to match the interface pattern</remarks>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns>Enumerable iterator for returning each line of the required data.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new Exception("TradeBars class not implemented. Use TradeBar reader instead.");
        }


        /// <summary>
        /// Get source file URL/endpoint for this TradeBar subscription request
        /// </summary>
        /// <param name="datafeed">Source of the datafeed / type of strings we'll be receiving</param>
        /// <param name="config">Configuration for the subscription</param>
        /// <param name="date">Date of the source file requested.</param>
        /// <remarks>This is unused for the Lean Engine but required to match the interface pattern</remarks>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns>String URL Source File</returns>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Clone the underlying data type for fillforward methods.
        /// </summary>
        /// <remarks>This is unused for the Lean Engine but required to match the interface pattern</remarks>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns>BaseData Clone of TradeBars Array</returns>
        public override BaseData Clone()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Add a TradeBar to this TradeBars collection.
        /// </summary>
        /// <param name="key">String symbol for tradebar</param>
        /// <param name="value">TradeBar value</param>
        /// <remarks>IDictionary implementation.</remarks>
        public void Add(string key, TradeBar value) {
            _tradeBars.Add(key, value);
        }

        /// <summary>
        /// TradeBar GetEnumerator implementation for IDictionary. Allows for emumeration of the TradeBars class.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>IEnumerator Key Value Pair</returns>
        IEnumerator<KeyValuePair<string, TradeBar>> IEnumerable<KeyValuePair<string, TradeBar>>.GetEnumerator()
        {
            return _tradeBars.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IDictionary)this).GetEnumerator();
        }

        /// <summary>
        /// Public flag indicating if the TradeBars are read only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>False - boolean flag if read only</returns>
        public bool IsReadOnly 
        {
            get 
            {
                return false;
            }
        }

        /// <summary>
        /// Count the number of tradebar objects in the dictionary.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public int Count 
        {
            get 
            {
                return _tradeBars.Count;
            }
        }

        /// <summary>
        /// Remove tradeBar matching this symbol.
        /// </summary>
        /// <param name="key">Key ticker</param>
        /// <remarks>IDictionary implementation</remarks>
        public bool Remove(string key) 
        {
            return _tradeBars.Remove(key);
        }

        /// <summary>
        /// Remove a keyvalue pair matching the symbol-tradebar objects
        /// </summary>
        /// <param name="kvp">KeyValue pair to remove</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Boolean true on success</returns>
        public bool Remove(KeyValuePair<string, TradeBar> kvp) 
        {
            return _tradeBars.Remove(kvp.Key);
        }

        /// <summary>
        /// Check if the TradeBars collection contains a symbol
        /// </summary>
        /// <param name="kvp">Key-value pair to search for</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True if found</returns>
        public bool Contains(KeyValuePair<string, TradeBar> kvp) {
            return _tradeBars.ContainsKey(kvp.Key);
        }

        /// <summary>
        /// Check if the TradeBars collection contains this symbol
        /// </summary>
        /// <param name="symbol">Security symbol</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True if found.</returns>
        public bool ContainsKey(string symbol) {
            return _tradeBars.ContainsKey(symbol);
        }

        /// <summary>
        /// Clear the TradeBars collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public void Clear() {
            _tradeBars.Clear();
        }

        /// <summary>
        /// Add a Symbol-TradeBar keyvalue pair to the dictionary collection.
        /// </summary>
        /// <param name="kvp">KeyValue pair we'd like to add to the the dictionary</param>
        /// <remarks>IDictionary implementation</remarks>
        public void Add(KeyValuePair<string, TradeBar> kvp) {
            _tradeBars.Add(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Collection of TradeBars (values) in this dictionary
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<TradeBar> Values {
            get {
                return _tradeBars.Values;
            }
        }

        /// <summary>
        /// Collection of symbols (keys) in this dictionary
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<string> Keys {
            get {
                return _tradeBars.Keys;
            }
        }

        /// <summary>
        /// TradeBar indexer for finding the tradebar we want using its symbol.
        /// </summary>
        /// <param name="key">Symbol indexer access to retrieve the TradeBar we'd like.</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>TradeBar object</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public TradeBar this[string key]
        {
            get
            {
                TradeBar bar;
                if (TryGetValue(key, out bar))
                {
                    return bar;
                }
                throw new KeyNotFoundException("'" + key + "' wasn't found in the TradeBars object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"" + key + "\")");
            }
            set
            {
                if (!_tradeBars.ContainsKey(key))
                {
                    throw new KeyNotFoundException("'" + key + "' wasn't found in the TradeBars object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"" + key + "\")");
                }
                _tradeBars[key] = value;
            }
        }

        /// <summary>
        /// Try and get the tradebar with this matching symbol. Returns false if the bar was not found.
        /// </summary>
        /// <param name="key">Symbol of TradeBar</param>
        /// <param name="bar">TradeBar object output</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True if finds this key</returns>
        public bool TryGetValue(string key, out TradeBar bar)
        {
            return _tradeBars.TryGetValue(key, out bar);
        }

        /// <summary>
        /// Copy a tradebars array.
        /// </summary>
        /// <param name="array">Destination Array</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="arrayIndex">Starting index</param>
        public void CopyTo(KeyValuePair<string, TradeBar>[] array, int arrayIndex)
        {
            Copy(this, array, arrayIndex);
        }

        private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            if ((array.Length - arrayIndex) < source.Count)
                throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");

            foreach (var item in source)
                array[arrayIndex++] = item;
        }
    }


} // End QC Namespace
