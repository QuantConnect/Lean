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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Logging;


namespace QuantConnect.Securities 
{
    /// <summary>
    /// Enumerable security management class for grouping security objects into an array and providing any common properties.
    /// </summary>
    /// <remarks>Implements IDictionary for the index searching of securities by symbol</remarks>
    public class SecurityManager : IDictionary<string, Security> 
    {
        //Internal dictionary implementation:
        private IDictionary<string, Security> _securityManager;
        private int _minuteLimit = 500;
        private int _minuteMemory = 2;
        private int _secondLimit = 100;
        private int _secondMemory = 10;
        private int _tickLimit = 30;
        private int _tickMemory = 34;
        private decimal _maxRamEstimate = 1024;

        /// <summary>
        /// Initialise the algorithm security manager with two empty dictionaries
        /// </summary>
        public SecurityManager()
        {
            _securityManager = new Dictionary<string, Security>(); 
        }

        /// <summary>
        /// Add a new security with this symbol to the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">symbol for security we're trading</param>
        /// <param name="security">security object</param>
        /// <seealso cref="Add(string,Resolution,bool)"/>
        public void Add(string symbol, Security security)
        {
            CheckResolutionCounts(security.Resolution);
            _securityManager.Add(symbol, security);
        }

        /// <summary>
        /// Add a new security with this symbol to the collection.
        /// </summary>
        /// <param name="security">security object</param>
        public void Add(Security security)
        {
            Add(security.Symbol, security);
        }

        /// <summary>
        /// Add a symbol-security by its key value pair.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="pair"></param>
        public void Add(KeyValuePair<string, Security> pair)
        {
            CheckResolutionCounts(pair.Value.Resolution);
            _securityManager.Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Clear the securities array to delete all the portfolio and asset information.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public void Clear()
        {
            _securityManager.Clear();
        }

        /// <summary>
        /// Check if this collection contains this key value pair.
        /// </summary>
        /// <param name="pair">Search key-value pair</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Bool true if contains this key-value pair</returns>
        public bool Contains(KeyValuePair<string, Security> pair)
        {
            return _securityManager.Contains(pair);
        }

        /// <summary>
        /// Check if this collection contains this symbol.
        /// </summary>
        /// <param name="symbol">Symbol we're checking for.</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Bool true if contains this symbol pair</returns>
        public bool ContainsKey(string symbol)
        {
            return _securityManager.ContainsKey(symbol);
        }

        /// <summary>
        /// Copy from the internal array to an external array.
        /// </summary>
        /// <param name="array">Array we're outputting to</param>
        /// <param name="number">Starting index of array</param>
        /// <remarks>IDictionary implementation</remarks>
        public void CopyTo(KeyValuePair<string, Security>[] array, int number)
        {
            _securityManager.CopyTo(array, number);
        }

        /// <summary>
        /// Count of the number of securities in the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public int Count
        {
            get { return _securityManager.Count; }
        }

        /// <summary>
        /// Flag indicating if the internal arrray is read only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public bool IsReadOnly
        {
            get { return _securityManager.IsReadOnly;  }
        }

        /// <summary>
        /// Remove a key value of of symbol-securities from the collections.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="pair">Key Value pair of symbol-security to remove</param>
        /// <returns>Boolean true on success</returns>
        public bool Remove(KeyValuePair<string, Security> pair)
        {
            return _securityManager.Remove(pair);
        }

        /// <summary>
        /// Remove this symbol security: Dictionary interface implementation.
        /// </summary>
        /// <param name="symbol">string symbol we're searching for</param>
        /// <returns>true success</returns>
        public bool Remove(string symbol)
        {
            return _securityManager.Remove(symbol);
        }

        /// <summary>
        /// List of the symbol-keys in the collection of securities.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<string> Keys
        {
            get { return _securityManager.Keys; }
        }

        /// <summary>
        /// Try and get this security object with matching symbol and return true on success.
        /// </summary>
        /// <param name="symbol">String search symbol</param>
        /// <param name="security">Output Security object</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>True on successfully locating the security object</returns>
        public bool TryGetValue(string symbol, out Security security)
        {
            return _securityManager.TryGetValue(symbol, out security);
        }

        /// <summary>
        /// Get a list of the security objects for this collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public ICollection<Security> Values
        {
            get { return _securityManager.Values; }
        }

        /// <summary>
        /// Get the enumerator for this security collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerable key value pair</returns>
        IEnumerator<KeyValuePair<string, Security>> IEnumerable<KeyValuePair<string, Security>>.GetEnumerator() 
        {
            return _securityManager.GetEnumerator();
        }

        /// <summary>
        /// Get the enumerator for this securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() 
        {
            return _securityManager.GetEnumerator();
        }

        /// <summary>
        /// Indexer method for the security manager to access the securities objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">Symbol string indexer</param>
        /// <returns>Security</returns>
        public Security this[string symbol]
        {
            get
            {
                symbol = symbol.ToUpper();
                if (!_securityManager.ContainsKey(symbol))
                {
                    throw new Exception("This asset symbol (" + symbol + ") was not found in your security list. Please add this security or check it exists before using it with 'Securities.ContainsKey(\"" + symbol + "\")'");
                } 
                return _securityManager[symbol];
            }
            set 
            {
                _securityManager[symbol] = value;
            }
        }

        /// <summary>
        /// Get the number of securities that have this resolution.
        /// </summary>
        /// <param name="resolution">Search resolution value.</param>
        /// <returns>Count of the securities</returns>
        public int  GetResolutionCount(Resolution resolution) 
        {
            var count = 0;
            try
            {
                count = (from security in _securityManager.Values
                         where security.Resolution == resolution
                         // don't count feeds we auto add
                         where !security.SubscriptionDataConfig.IsInternalFeed
                         select security.Resolution).Count();
            } 
            catch (Exception err) 
            {
                Log.Error("Algorithm.Market.GetResolutionCount(): " + err.Message);
            }
            return count;
        }

        /// <summary>
        /// Limits on the number of minute, second and tick assets due to memory constraints.
        /// </summary>
        /// <param name="minute">Minute asset allowance</param>
        /// <param name="second">Second asset allowance</param>
        /// <param name="tick">Tick asset allowance</param>
        public void SetLimits(int minute, int second, int tick)
        {
            _minuteLimit = minute;  //Limit the number and combination of symbols
            _secondLimit = second;
            _tickLimit = tick;
            _maxRamEstimate = Math.Max(Math.Max(_minuteLimit * _minuteMemory, _secondLimit * _secondMemory), _tickLimit * _tickMemory);
        }

        /// <summary>
        /// Update the security properties/online functions with new data/price packets.
        /// </summary>
        /// <param name="time">Time Frontier</param>
        /// <param name="data">Data packets to update</param>
        public void Update(DateTime time, Dictionary<int, List<BaseData>> data) 
        {
            try 
            {
                //If its market data, look for the matching security symbol and update it:
                foreach (var security in _securityManager.Values)
                {
                    BaseData dataPoint = null;
                    List<BaseData> dataPoints;
                    if (data.TryGetValue(security.SubscriptionDataConfig.SubscriptionIndex, out dataPoints) && dataPoints.Count != 0)
                    {
                        // ignore aux data when doing data update, we trust the latest data more
                        for (int i = dataPoints.Count - 1; i > -1; i--)
                        {
                            if (dataPoints[i].DataType == MarketDataType.Auxiliary)
                            {
                                continue;
                            }
                            dataPoint = dataPoints[i];
                            break;
                        }
                    }
                    security.SetMarketPrice(time, dataPoint);
                }
            }
            catch (Exception err) 
            {
                Log.Error("Algorithm.Market.Update(): " + err.Message);
            }
        }

        /// <summary>
        /// Verifies that we can add more securities
        /// </summary>
        /// <param name="resolution">The new resolution to be added</param>
        private void CheckResolutionCounts(Resolution resolution)
        {
            //Maximum Data Usage: mainly RAM constraints but this has never been fully tested.
            if (GetResolutionCount(Resolution.Tick) >= _tickLimit && resolution == Resolution.Tick)
            {
                throw new Exception("We currently only support " + _tickLimit + " tick assets at a time due to physical memory limitations.");
            }
            if (GetResolutionCount(Resolution.Second) >= _secondLimit && resolution == Resolution.Second)
            {
                throw new Exception("We currently only support  " + _secondLimit + "  second resolution securities at a time due to physical memory limitations.");
            }
            if (GetResolutionCount(Resolution.Minute) >= _minuteLimit && resolution == Resolution.Minute)
            {
                throw new Exception("We currently only support  " + _minuteLimit + "  minute assets at a time due to physical memory limitations.");
            }

            //Current ram usage: this especially applies during live trading where micro servers have limited resources:
            var currentEstimatedRam = GetRamEstimate(GetResolutionCount(Resolution.Minute), GetResolutionCount(Resolution.Second),
                GetResolutionCount(Resolution.Tick));

            if (currentEstimatedRam > _maxRamEstimate)
            {
                throw new Exception("We estimate you will run out of memory (" + currentEstimatedRam + "mb of " + _maxRamEstimate
                    + "mb physically available). Please reduce the number of symbols you're analysing or if in live trading upgrade your server to allow more memory.");
            }
        }

        /// <summary>
        /// Estimated ram usage with this symbol combination:
        /// </summary>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="tick"></param>
        /// <returns>Decimal estimate of the number of MB ram the requested assets would consume</returns>
        private decimal GetRamEstimate(int minute, int second, int tick)
        {
            return _minuteMemory * minute + _secondMemory * second + _tickMemory * tick;
        }

    } // End Algorithm Security Manager Class

} // End QC Namespace
