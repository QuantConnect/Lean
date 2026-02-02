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

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Represents a response containing price response data.
/// </summary>
[JsonConverter(typeof(ThetaDataPriceConverter))]
public readonly struct PriceResponse
{
    /// <summary>
    /// Gets the time at which open interest was reported, represented in milliseconds since 00:00:00.000 (midnight) Eastern Time (ET).
    /// </summary>
    public uint TimeMilliseconds { get; }

    /// <summary>
    /// The reported price of the index.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// The date formatted as YYYYMMDD. (e.g. "20240328" -> 2024/03/28)
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// Gets the DateTime representation of the last quote time. DateTime is New York Time (EST) Time Zone!
    /// </summary>
    /// <remarks>
    /// This property calculates the <see cref="Date"/> by adding the <seealso cref="TimeMilliseconds"/> to the Date property.
    /// </remarks>
    public DateTime DateTimeMilliseconds { get => Date.AddMilliseconds(TimeMilliseconds); }

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceResponse"/> struct.
    /// </summary>
    /// <param name="timeMilliseconds">The milliseconds since 00:00:00.000 (midnight) Eastern Time (ET).</param>
    /// <param name="price">The reported price of the index.</param>
    /// <param name="date">The date formatted as YYYYMMDD.</param>
    public PriceResponse(uint timeMilliseconds, decimal price, DateTime date)
    {
        TimeMilliseconds = timeMilliseconds;
        Price = price;
        Date = date;
    }
}
