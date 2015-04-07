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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Logging;


namespace QuantConnect.Securities 
{
    /********************************************************
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Enumerable security management class for grouping security objects into an array and providing any common properties.
    /// </summary>
    /// <remarks>Implements IDictionary for the index searching of securities by symbol</remarks>
    public class SecurityManager : IDictionary<string, Security> 
    {
        /********************************************************
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        //Internal dictionary implementation:
        private IDictionary<string, Security> _securityManager;
        private IDictionary<string, SecurityHolding> _securityHoldings;
        private int _minuteLimit = 500;
        private int _minuteMemory = 2;
        private int _secondLimit = 100;
        private int _secondMemory = 10;
        private int _tickLimit = 30;
        private int _tickMemory = 34;
        private decimal _maxRamEstimate = 1024;

        /********************************************************
        * CLASS PUBLIC VARIABLES
        *********************************************************/

        /********************************************************
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Initialise the algorithm security manager with two empty dictionaries
        /// </summary>
        public SecurityManager()
        {
            _securityManager = new Dictionary<string, Security>(); 
            _securityHoldings = new Dictionary<string, SecurityHolding>();
        }

        /********************************************************
        * CLASS PROPERTIES
        *********************************************************/
        

        /********************************************************
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Add a new security with this symbol to the collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">symbol for security we're trading</param>
        /// <param name="security">security object</param>
        /// <seealso cref="Add(string,Resolution,bool)"/>
        public void Add(string symbol, Security security) 
        {
            _securityManager.Add(symbol, security);
        }


        /// <summary>
        /// Add a new security to the collection by symbol defaulting to SecurityType.Equity
        /// </summary>
        /// <param name="symbol">Symbol for the equity we're adding</param>
        /// <param name="resolution">Resolution of the securty we're adding</param>
        /// <param name="fillDataForward">Boolean flag indicating the security is fillforward</param>
        public void Add(string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true) 
        {
            Add(symbol, SecurityType.Equity, resolution, fillDataForward);
        }


        /// <summary>
        /// Add a new security by all of its properties.
        /// </summary>
        /// <param name="symbol">Symbol of the security</param>
        /// <param name="type">Type of security: Equity, Forex or Future</param>
        /// <param name="resolution">Resolution of data required: currently only tick, second and minute for QuantConnect sources.</param>
        /// <param name="fillDataForward">Return previous bar's data when there is no trading in this bar</param>
        /// <param name="leverage">Leverage for this security, default = 1</param>
        /// <param name="extendedMarketHours">Request all the data available, including the extended market hours from 4am - 8pm.</param>
        /// <param name="isDynamicallyLoadedData">Use dynamic data</param>
        public void Add(string symbol, SecurityType type = SecurityType.Equity, Resolution resolution = Resolution.Minute, bool fillDataForward = true, decimal leverage = 1, bool extendedMarketHours = false, bool isDynamicallyLoadedData = false) 
        {
            //Upper case sybol:
            symbol = symbol.ToUpper();

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
            var currentEstimatedRam = GetRamEstimate(GetResolutionCount(Resolution.Minute), GetResolutionCount(Resolution.Second), GetResolutionCount(Resolution.Tick));
            
            if (currentEstimatedRam > _maxRamEstimate)
            {
                throw new Exception("We estimate you will run out of memory (" + currentEstimatedRam + "mb of " + _maxRamEstimate + "mb physically available). Please reduce the number of symbols you're analysing or if in live trading upgrade your server to allow more memory.");
            }

            //If we don't already have this asset, add it to the securities list.
            if (!_securityManager.ContainsKey(symbol)) 
            {
                switch (type)
                {
                    case SecurityType.Equity:
                        Add(symbol, new Equity.Equity(symbol, resolution, fillDataForward, leverage, extendedMarketHours, isDynamicallyLoadedData));
                        break;
                    case SecurityType.Forex:
                        Add(symbol, new Forex.Forex(symbol, resolution, fillDataForward, leverage, extendedMarketHours, isDynamicallyLoadedData));
                        break;
                    case SecurityType.Base:
                        Add(symbol, new Security(symbol, SecurityType.Base, resolution, fillDataForward, leverage, extendedMarketHours, isDynamicallyLoadedData));
                        break;
                    default:
                        throw new Exception("We currently only support Equity and Forex Securities Types. Its still possible to trade futures but you must use generic data. Please see the QC University example 'Quandl Futures'.");
                }
            } 
            else 
            {
                //Otherwise, we already have it, just change its resolution:
                Log.Trace("Algorithm.Securities.Add(): Changing security information will overwrite portfolio");
                switch (type)
                {
                    case SecurityType.Equity:
                        _securityManager[symbol] = new Equity.Equity(symbol, resolution, fillDataForward, leverage, extendedMarketHours);
                        break;
                    case SecurityType.Forex:
                        _securityManager[symbol] = new Forex.Forex(symbol, resolution, fillDataForward, leverage, extendedMarketHours);
                        break;
                    case SecurityType.Base:
                        _securityManager[symbol] = new Security(symbol, SecurityType.Base, resolution, fillDataForward, leverage, extendedMarketHours, isDynamicallyLoadedData);
                        break;
                }
            }
        }
        
        
        /// <summary>
        /// Add a symbol-security by its key value pair.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="pair"></param>
        public void Add(KeyValuePair<string, Security> pair)
        {
            _securityManager.Add(pair.Key, pair.Value);
            _securityHoldings.Add(pair.Key, pair.Value.Holdings);
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
        /// Get the internal enumerator for the securities collection for use by the Portfolio object.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerator</returns>
        public IDictionary<string, SecurityHolding> GetInternalPortfolioCollection()
        {
            return _securityHoldings;
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
                    throw new Exception("This asset symbol (" + symbol + ") was not found in your security list. Please add this security or check it exists before using it with 'data.ContainsKey(\"" + symbol + "\")'");
                } 
                return _securityManager[symbol];
            }
            set 
            {
                _securityManager[symbol] = value;
                _securityHoldings[symbol] = value.Holdings;
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

        /// <summary>
        /// Update the security properties/online functions with new data/price packets.
        /// </summary>
        /// <param name="time">Time Frontier</param>
        /// <param name="data">Data packets to update</param>
        public void Update(DateTime time, BaseData data) 
        {
            try 
            {
                //If its market data, look for the matching security symbol and update it:
                foreach (var security in _securityManager.Values)
                {
                    if (data.Symbol == security.Symbol)
                    {
                        security.Update(time, data);
                    }
                    else
                    {
                        security.Update(time, null); //No data, update time
                    }
                }
            }
            catch (Exception err) 
            {
                Log.Error("Algorithm.Market.Update(): " + err.Message);
            }
        }

    } // End Algorithm Security Manager Class

} // End QC Namespace
