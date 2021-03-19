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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Zerodha utility class
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Convert string to Date object
        /// </summary>
        /// <param name="dateString">Date string.</param>
        /// <returns>Date object/</returns>
        public static DateTime? StringToDate(string dateString)
        {
            if (dateString == null)
            {
                return null;
            }

            try
            {
                return DateTime.ParseExact(dateString, dateString.Length == 10 ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Serialize C# object to JSON string.
        /// </summary>
        /// <param name="obj">C# object to serialize.</param>
        /// <returns>JSON string/</returns>
        public static string JsonSerialize(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            MatchCollection mc = Regex.Matches(json, @"\\/Date\((\d*?)\)\\/");
            foreach (Match m in mc)
            {
                var unix = Convert.ToInt64(m.Groups[1].Value,CultureInfo.InvariantCulture) / 1000;
                json = json.Replace(m.Groups[0].Value, UnixToDateTime(unix).ToStringInvariant());
            }
            return json;
        }

        /// <summary>
        /// Deserialize Json string to nested string dictionary.
        /// </summary>
        /// <param name="Json">Json string to deserialize.</param>
        /// <returns>Json in the form of nested string dictionary.</returns>
        public static JObject JsonDeserialize(string Json)
        {
            JObject jObject = JObject.Parse(Json);
            return jObject;
        }

        /// <summary>
        /// Recursively traverses an object and converts double fields to decimal.
        /// This is used in Json deserialization. JavaScriptSerializer converts floats
        /// in exponential notation to double and everthing else to double. This function
        /// makes everything decimal. Currently supports only Dictionary and Array as input.
        /// </summary>
        /// <param name="obj">Input object.</param>
        /// <returns>Object with decimals instead of doubles</returns>
        public static dynamic DoubleToDecimal(dynamic obj)
        {
            if (obj is double)
            {
                obj = Convert.ToDecimal(obj);
            }
            else if (obj is IDictionary)
            {
                var keys = new List<string>(obj.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    obj[keys[i]] = DoubleToDecimal(obj[keys[i]]);
                }
            }
            else if (obj is ICollection)
            {
                obj = new ArrayList(obj);
                for (int i = 0; i < obj.Count; i++)
                {
                    obj[i] = DoubleToDecimal(obj[i]);
                }
            }
            return obj;
        }

        /// <summary>
        /// Wraps a string inside a stream
        /// </summary>
        /// <param name="value">string data</param>
        /// <returns>Stream that reads input string</returns>
        public static MemoryStream StreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        /// <summary>
        /// Helper function to add parameter to the request only if it is not null or empty
        /// </summary>
        /// <param name="Params">Dictionary to add the key-value pair</param>
        /// <param name="Key">Key of the parameter</param>
        /// <param name="Value">Value of the parameter</param>
        public static void AddIfNotNull(Dictionary<string, dynamic> Params, string Key, string Value)
        {
            if (!String.IsNullOrEmpty(Value))
                Params.Add(Key, Value);
        }


        /// <summary>
        /// Creates key=value with url encoded value
        /// </summary>
        /// <param name="Key">Key</param>
        /// <param name="Value">Value</param>
        /// <returns>Combined string</returns>
        public static string BuildParam(string Key, dynamic Value)
        {
            if (Value is string)
            {
                return HttpUtility.UrlEncode(Key) + "=" + HttpUtility.UrlEncode((string)Value);
            }
            else
            {
                string[] values = (string[])Value;
                return String.Join("&", values.Select(x => HttpUtility.UrlEncode(Key) + "=" + HttpUtility.UrlEncode(x)));
            }
        }

        /// <summary>
        /// Convert Unix TimeStamp to DateTime
        /// </summary>
        /// <param name="unixTimeStamp">Timestamp to convert</param>
        /// <returns><see cref="DateTime"/> object representing the timestamp</returns>
        public static DateTime UnixToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 5, 30, 0, 0, DateTimeKind.Unspecified);
            dateTime = dateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }

        /// <summary>
        /// Convert ArrayList to list of <see cref="decimal"/>
        /// </summary>
        /// <param name="arrayList"><see cref="ArrayList"/> to convert</param>
        /// <returns>List of <see cref="decimal"/>s</returns>
        public static List<decimal> ToDecimalList(ArrayList arrayList)
        {
            var res = new List<decimal>();
            foreach(var i in arrayList)
            {
                res.Add(Convert.ToDecimal(i,CultureInfo.InvariantCulture));
            }
            return res;
        }
    }
}
