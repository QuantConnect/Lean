/*
 * CASCADELABS.IO
 * Cascade Labs LLC
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
