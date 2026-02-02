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

using Newtonsoft.Json;
using QuantConnect.Lean.DataSource.CascadeThetaData.Converters;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.WebSocket;

public readonly struct WebSocketTrade
{
    [JsonProperty("ms_of_day")]
    public int TimeMilliseconds { get; }

    [JsonProperty("sequence")]
    public int Sequence { get; }

    [JsonProperty("size")]
    public int Size { get; }

    [JsonProperty("condition")]
    public int Condition { get; }

    [JsonProperty("price")]
    public decimal Price { get; }

    [JsonProperty("exchange")]
    public byte Exchange { get; }

    [JsonProperty("date")]
    [JsonConverter(typeof(DateTimeIntJsonConverter))]
    public DateTime Date { get; }

    /// <summary>
    /// Gets the DateTime representation of the last trade time. DateTime is New York Time (EST) Time Zone!
    /// </summary>
    /// <remarks>
    /// This property calculates the <see cref="Date"/> by adding the <seealso cref="TimeMilliseconds"/> to the Date property.
    /// </remarks>
    public DateTime DateTimeMilliseconds { get => Date.AddMilliseconds(TimeMilliseconds); }

    [JsonConstructor]
    public WebSocketTrade(int timeMilliseconds, int sequence, int size, int condition, decimal price, byte exchange, DateTime date)
    {
        TimeMilliseconds = timeMilliseconds;
        Sequence = sequence;
        Size = size;
        Condition = condition;
        Price = price;
        Exchange = exchange;
        Date = date;
    }
}
