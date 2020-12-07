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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Api
{
    /// <summary>
    /// Custom JsonConverter for AlphaRuntimeStatistics data for algorithm results
    /// </summary>
    public class AlphaRuntimeStatisticsJsonConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("The LiveAlgorithmResultsJsonConverter does not implement a WriteJson method.");
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(AlphaRuntimeStatistics).IsAssignableFrom(objectType);
        }


        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var alphaRuntimeStatistics = CreateStatisticsFromJObject(jObject);

            return alphaRuntimeStatistics;
        }

        /// <summary>
        /// Custom parsing of Runtime Statistics
        /// This was implemented for bug surrounding Sortino ratio recieved in exponential format
        /// </summary>
        /// <param name="jObject">Json representing AlphaRuntimeStatistics</param>
        /// <returns></returns>
        public static AlphaRuntimeStatistics CreateStatisticsFromJObject(JObject jObject)
        {
            var statisticsResults = new AlphaRuntimeStatistics
            {
                FitnessScore = jObject["FitnessScore"].Value<decimal>(),
                MeanPopulationScore = jObject["MeanPopulationScore"].ToObject<InsightScore>(),
                PortfolioTurnover = jObject["PortfolioTurnover"].Value<decimal>(),
                ReturnOverMaxDrawdown = jObject["ReturnOverMaxDrawdown"].Value<decimal>(),
                RollingAveragedPopulationScore = jObject["RollingAveragedPopulationScore"].ToObject<InsightScore>(),
                SortinoRatio = decimal.Parse(jObject["SortinoRatio"].Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture),
            };

            return statisticsResults;
        }
    }
}
