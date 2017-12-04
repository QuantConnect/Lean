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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Base class for Bitfinex wss messages
    /// </summary>
    public abstract class BaseMessage
    {
        /// <summary>
        /// Stored Keys for message
        /// </summary>
        protected string[] AllKeys { get; set; }

        /// <summary>
        /// Stores values of message
        /// </summary>
        protected string[] AllValues { get; set; }

        /// <summary>
        /// Creates base message instance
        /// </summary>
        /// <param name="values"></param>
        public BaseMessage(string[] values)
        {
            AllValues = values;
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(int key)
        {
            return AllValues[key];
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public decimal GetDecimal(int key)
        {
            return decimal.Parse(AllValues[key]);
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <remarks>This should not be necessary if Json serialization settings are used</remarks>
        /// <param name="key"></param>
        /// <returns></returns>
        public decimal TryGetDecimalFromScientific(int key)
        {
            if (AllValues[key] == null)
            {
                return 0m;
            }

            decimal parsed;
            try
            {
                parsed = decimal.Parse(AllValues[key], System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowDecimalPoint);
            }
            catch (Exception)
            {
                return 0m;
            }
            return parsed;
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetInt(int key)
        {
            return int.Parse(AllValues[key]);
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetLong(int key)
        {
            return long.Parse(AllValues[key]);
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int TryGetInt(int key)
        {
            int parsed;
            if (int.TryParse(AllValues[key], out parsed))
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
        public long TryGetLong(int key)
        {
            long parsed;
            if (long.TryParse(AllValues[key], out parsed))
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
        public decimal TryGetDecimal(int key)
        {
            decimal parsed;
            if (decimal.TryParse(AllValues[key], out parsed))
            {
                return parsed;
            }

            var scientific = TryGetDecimalFromScientific(key);
            return scientific;
        }

        /// <summary>
        /// Returns typed value from untyped json array
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DateTime GetDateTime(int key)
        {
            return Time.UnixTimeStampToDateTime(double.Parse(AllValues[key]));
        }
    }
}