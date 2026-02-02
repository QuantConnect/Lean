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

[JsonConverter(typeof(ThetaDataOpenHighLowCloseConverter))]
public readonly struct OpenHighLowCloseResponse
{
    /// <summary>
    /// The milliseconds since 00:00:00.000 (midnight) Eastern Time (ET).
    /// </summary>
    public uint TimeMilliseconds { get; }

    /// <summary>
    /// The opening trade price.
    /// </summary>
    public decimal Open { get; }

    /// <summary>
    /// The highest traded price.
    /// </summary>
    public decimal High { get; }

    /// <summary>
    /// The lowest traded price.
    /// </summary>
    public decimal Low { get; }

    /// <summary>
    /// The closing traded price.
    /// </summary>
    public decimal Close { get; }

    /// <summary>
    /// The amount of contracts traded.
    /// </summary>
    public decimal Volume { get; }

    /// <summary>
    /// The amount of trades.
    /// </summary>
    public uint Count { get; }

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
    /// Initializes a new instance of the <see cref="OpenHighLowCloseResponse"/> struct.
    /// </summary>
    /// <param name="timeMilliseconds">The milliseconds since 00:00:00.000 (midnight) Eastern Time (ET).</param>
    /// <param name="open">The opening trade price.</param>
    /// <param name="high">The highest traded price.</param>
    /// <param name="low">The lowest traded price.</param>
    /// <param name="close">The closing traded price.</param>
    /// <param name="volume">The amount of contracts traded.</param>
    /// <param name="count">The amount of trades.</param>
    /// <param name="date">The date formatted as YYYYMMDD.</param>
    public OpenHighLowCloseResponse(uint timeMilliseconds, decimal open, decimal high, decimal low, decimal close, decimal volume, uint count, DateTime date)
    {
        TimeMilliseconds = timeMilliseconds;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        Count = count;
        Date = date;
    }
}
