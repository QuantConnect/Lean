using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Report
{
    /// <summary>
    /// Removes null values in the <see cref="Result"/> object's x,y values so that
    /// deserialization can occur without exceptions.
    /// </summary>
    /// <typeparam name="T">Result type to deserialize into</typeparam>
    public class NullResultValueTypeJsonConverter<T> : JsonConverter
        where T : Result
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);

            foreach (JProperty property in token["Charts"].Children())
            {
                foreach (JProperty seriesProperty in property.Value["Series"])
                {
                    var newValues = new List<JToken>();
                    foreach (var entry in seriesProperty.Value["Values"])
                    {
                        if (entry["x"] == null || entry["x"].Value<long?>() == null ||
                            entry["y"] == null || entry["y"].Value<decimal?>() == null)
                        {
                            continue;
                        }

                        newValues.Add(entry);
                    }

                    token["Charts"][property.Name]["Series"][seriesProperty.Name]["Values"] = JArray.FromObject(newValues);
                }
            }

            // Deserialize with OrderJsonConverter, otherwise it will fail. We convert the token back
            // to its JSON representation and use the `JsonConvert.DeserializeObject<T>(...)` method instead
            // of using `token.ToObject<T>()` since it can be provided a JsonConverter in its arguments.
            return JsonConvert.DeserializeObject<T>(token.ToString(), new Orders.OrderJsonConverter());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
