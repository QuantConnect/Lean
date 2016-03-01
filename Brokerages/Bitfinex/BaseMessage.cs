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
