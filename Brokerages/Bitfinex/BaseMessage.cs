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
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{


    /// <summary>
    /// Base class for Bitfinex wss messages
    /// </summary>
    public abstract class BaseMessage
    {

        /// <summary>
        /// Stored Keys for message
        /// </summary>
        protected string[] allKeys { get; set; }

        /// <summary>
        /// Stores values of message
        /// </summary>
        protected string[] allValues { get; set; }

        /// <summary>
        /// Creates base message instance
        /// </summary>
        /// <param name="values"></param>
        public BaseMessage(string[] values)
        {
            allValues = values;
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            return allValues[Array.IndexOf(allKeys, key)];
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public decimal GetDecimal(string key)
        {
            return decimal.Parse(allValues[Array.IndexOf(allKeys, key)]);
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public decimal GetDecimalFromScientific(string key)
        {
            if (allValues[Array.IndexOf(allKeys, key)] == null)
            {
                return 0m;
            }
            string value = allValues[Array.IndexOf(allKeys, key)].Trim('-');
            return Decimal.Parse(value, System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowDecimalPoint);
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetInt(string key)
        {
            return int.Parse(allValues[Array.IndexOf(allKeys, key)]);
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int TryGetInt(string key)
        {
            int parsed;
            if (int.TryParse(allValues[Array.IndexOf(allKeys, key)], out parsed))
            {
                return parsed;
            }
            return 0;
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public decimal TryGetDecimal(string key)
        {
            decimal parsed;
            if (decimal.TryParse(allValues[Array.IndexOf(allKeys, key)], out parsed))
            {
                return parsed;
            }
            return 0m;
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DateTime GetDateTime(string key)
        {
            return Time.UnixTimeStampToDateTime(double.Parse(allValues[Array.IndexOf(allKeys, key)]));
        }


    }
}
