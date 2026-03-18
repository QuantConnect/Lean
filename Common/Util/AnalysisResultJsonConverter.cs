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
 *
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// JSON converter for <see cref="AnalysisResult"/>.
    /// Deserializes into a <see cref="AnalysisResult"/> and detects the concrete
    /// <see cref="IResultsAnalysisContext"/> type from the shape of the JSON:
    /// <list type="bullet">
    ///   <item>JSON array -> <see cref="ResultsAnalysisAggregateContext"/></item>
    ///   <item>JSON object with an <c>Occurrences</c> property -> <see cref="ResultsAnalysisRepeatedContext"/></item>
    ///   <item>Any other JSON object -> <see cref="ResultsAnalysisContext"/></item>
    /// </list>
    /// </summary>
    public class AnalysisResultJsonConverter : JsonConverter
    {
        /// <summary>
        /// Serialization is handled by the default JSON.NET serializer.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Not implemented — serialization is delegated to the default serializer.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserializes a JSON object into a <see cref="AnalysisResult"/>, resolving the
        /// concrete <see cref="IResultsAnalysisContext"/> type from the structure of the
        /// <c>Context</c> JSON token.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var name = jObject["Name"]?.Value<string>();
            var issue = jObject["Issue"]?.Value<string>();
            var weight = jObject["Weight"]?.Value<int>() ?? 0;
            var solutions = jObject["Solutions"]?.ToObject<List<string>>() ?? [];
            var context = DeserializeContext(jObject["Context"]);

            return new AnalysisResult(name, issue, weight, context, solutions);
        }

        /// <summary>
        /// Determines whether this converter can handle the given type.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return typeof(AnalysisResult).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Recursively deserializes a context token into the correct
        /// <see cref="IResultsAnalysisContext"/> concrete type.
        /// </summary>
        private static IResultsAnalysisContext DeserializeContext(JToken contextToken)
        {
            if (contextToken == null || contextToken.Type == JTokenType.Null)
            {
                return null;
            }

            if (contextToken.Type == JTokenType.Array)
            {
                var innerContexts = new List<IResultsAnalysisContext>();
                foreach (var item in (JArray)contextToken)
                {
                    innerContexts.Add(DeserializeContext(item));
                }
                return new ResultsAnalysisAggregateContext(innerContexts);
            }

            if (contextToken.Type == JTokenType.Object)
            {
                var jObj = (JObject)contextToken;
                var sample = jObj["Sample"]?.ToObject<object>();

                if (jObj.ContainsKey("Occurrences"))
                {
                    return new ResultsAnalysisRepeatedContext([])
                    {
                        Sample = sample,
                        Occurrences = jObj["Occurrences"]?.Value<int>() ?? 0
                    };
                }

                return new ResultsAnalysisContext(sample);
            }

            return null;
        }
    }
}
