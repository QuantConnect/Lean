using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{
    public abstract class BaseMessage
    {

        protected string[] allKeys { get; set; }
        protected string[] allValues { get; set; }

        public BaseMessage(string[] values)
        {
            allValues = values;
        }

        public string GetString(string key)
        {
            return allValues[Array.IndexOf(allKeys, key)];
        }

        public decimal GetDecimal(string key)
        {
            return decimal.Parse(allValues[Array.IndexOf(allKeys, key)]);
        }

        public decimal GetDecimalFromScientific(string key)
        {
            if (allValues[Array.IndexOf(allKeys, key)] == null)
            {
                return 0m;
            }
            string value = allValues[Array.IndexOf(allKeys, key)].Trim('-');
            return Decimal.Parse(value, System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowDecimalPoint);
        }

        public int GetInt(string key)
        {
            return int.Parse(allValues[Array.IndexOf(allKeys, key)]);
        }

        public int TryGetInt(string key)
        {
            int parsed;
            if (int.TryParse(allValues[Array.IndexOf(allKeys, key)], out parsed))
            {
                return parsed;
            }
            return 0;
        }

        public DateTime GetDateTime(string key)
        {
            return Time.UnixTimeStampToDateTime(double.Parse(allValues[Array.IndexOf(allKeys, key)]));
        }


    }
}
